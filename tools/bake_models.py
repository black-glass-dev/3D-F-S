#!/usr/bin/env python3
"""Turn the user's .glb model drops into Unity-ready assets.

    python3 tools/bake_models.py "Test asset.zip"

What it does
------------
* unpacks the zip and finds every .glb/.gltf/.obj inside
* converts glTF -> OBJ (+ .mtl + textures) with no external tooling
* small, hand-made meshes (<= 25 000 triangles) are shipped as OBJ + a
  downscaled texture -- Unity imports OBJ natively, nothing else needed
* heavy scanned meshes are decimated with quadric edge collapse, cleaned
  (degenerate / duplicate faces removed) and their baked texture colour is
  transferred onto the vertices, then written as a tiny `*.bytes` mesh
  (positions + normals + RGBA) that ModelLibrary.cs reads at runtime.
  That keeps a 500 000-triangle photogrammetry house at ~60 000 triangles
  and ~2 MB, with no texture fetch and one draw call on a phone.

Requires: numpy, pillow, fast-simplification, scipy  (pip install ...)
"""
import json
import os
import struct
import sys
import zipfile

import numpy as np
from PIL import Image

COMPONENT = {5120: ('b', 1), 5121: ('B', 1), 5122: ('h', 2), 5123: ('H', 2),
             5125: ('I', 4), 5126: ('f', 4)}
NCOMP = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}

LIGHT_MESH_TRIS = 25000      # below this: keep UVs + texture, ship as OBJ
TARGET_REDUCTION = 0.75      # heavy meshes: quadric collapse ratio (~62k tris)

# Photogrammetry albedo atlases are irregular: any UV transfer onto a
# decimated mesh shows as per-triangle patchwork, so scans are shipped with
# baked vertex colours (smooth, coherent) instead. Flip this to True for a
# future model whose atlas unwraps cleanly.
USE_TEXTURE_BAKE = False
ATLAS_SIZE = 512             # downscale imported textures for phones
SCAN_TEX_SIZE = 1024         # albedo size for the baked scan atlases


# --------------------------------------------------------------------------
# glTF / GLB reading
# --------------------------------------------------------------------------
def load_glb(path):
    data = open(path, 'rb').read()
    magic, _version, _length = struct.unpack('<III', data[:12])
    if magic != 0x46546C67:
        raise ValueError('not a binary glTF: ' + path)
    off, js, blob = 12, None, b''
    while off < len(data):
        clen, ctype = struct.unpack('<II', data[off:off + 8])
        chunk = data[off + 8:off + 8 + clen]
        if ctype == 0x4E4F534A:
            js = json.loads(chunk.decode('utf-8'))
        elif ctype == 0x004E4942:
            blob = chunk
        off += 8 + clen
    return js, blob


def accessor(js, blob, index):
    acc = js['accessors'][index]
    fmt, size = COMPONENT[acc['componentType']]
    n = NCOMP[acc['type']]
    bv = js['bufferViews'][acc['bufferView']]
    start = bv.get('byteOffset', 0) + acc.get('byteOffset', 0)
    stride = bv.get('byteStride') or (size * n)
    out = np.empty((acc['count'], n), dtype=np.float64)
    raw = blob
    for i in range(acc['count']):
        out[i] = struct.unpack_from('<' + fmt * n, raw, start + i * stride)
    return out


def mat_mul(a, b):
    return [sum(a[r * 4 + k] * b[k * 4 + c] for k in range(4)) for r in range(4) for c in range(4)]


def node_matrix(node):
    if 'matrix' in node:
        m = node['matrix']  # column-major
        return [m[0], m[4], m[8], m[12], m[1], m[5], m[9], m[13],
                m[2], m[6], m[10], m[14], m[3], m[7], m[11], m[15]]
    t = node.get('translation', [0, 0, 0])
    x, y, z, w = node.get('rotation', [0, 0, 0, 1])
    s = node.get('scale', [1, 1, 1])
    rot = [1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w), 0,
           2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w), 0,
           2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y), 0,
           0, 0, 0, 1]
    scl = [s[0], 0, 0, 0, 0, s[1], 0, 0, 0, 0, s[2], 0, 0, 0, 0, 1]
    m = mat_mul(rot, scl)
    m[3], m[7], m[11] = t[0], t[1], t[2]
    return m


def xform(m, p):
    x, y, z = p
    return (m[0] * x + m[1] * y + m[2] * z + m[3],
            m[4] * x + m[5] * y + m[6] * z + m[7],
            m[8] * x + m[9] * y + m[10] * z + m[11])


def read_gltf(glb_path):
    """Returns (positions, uvs, colours, tris, materials) flattened to one mesh."""
    js, blob = load_glb(glb_path)

    images = {}
    for i, img in enumerate(js.get('images', [])):
        if 'bufferView' not in img:
            continue
        bv = js['bufferViews'][img['bufferView']]
        start = bv.get('byteOffset', 0)
        images[i] = blob[start:start + bv['byteLength']]

    def base_texture(mat):
        pbr = mat.get('pbrMetallicRoughness', {})
        if 'baseColorTexture' not in pbr:
            return None
        src = js['textures'][pbr['baseColorTexture']['index']].get('source')
        return Image.open(__import__('io').BytesIO(images[src])).convert('RGB') if src in images else None

    verts, uvs, tris = [], [], []
    v_off = 0
    materials = []

    def walk(idx, parent):
        nonlocal v_off
        node = js['nodes'][idx]
        world = mat_mul(parent, node_matrix(node))
        if 'mesh' in node:
            for prim in js['meshes'][node['mesh']]['primitives']:
                if prim.get('mode', 4) != 4:
                    continue
                pos = accessor(js, blob, prim['attributes']['POSITION'])
                uv = accessor(js, blob, prim['attributes']['TEXCOORD_0']) if 'TEXCOORD_0' in prim['attributes'] else None
                if 'indices' in prim:
                    idxs = accessor(js, blob, prim['indices'])[:, 0].astype(np.int64)
                else:
                    idxs = np.arange(len(pos), dtype=np.int64)

                verts.append(np.array([xform(world, p) for p in pos]))
                uvs.append(uv if uv is not None else np.zeros((len(pos), 2)))
                tris.append(idxs.reshape(-1, 3) + v_off)
                v_off += len(pos)

                mat_index = prim.get('material', 0)
                while len(materials) <= mat_index:
                    materials.append(None)
                if materials[mat_index] is None and mat_index < len(js.get('materials', [])):
                    materials[mat_index] = base_texture(js['materials'][mat_index])
        for child in node.get('children', []):
            walk(child, world)

    identity = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]
    scene = js.get('scenes', [{}])[js.get('scene', 0)]
    for root in scene.get('nodes', []):
        walk(root, identity)

    V = np.concatenate(verts) if verts else np.zeros((0, 3))
    UV = np.concatenate(uvs) if uvs else np.zeros((0, 2))
    F = np.concatenate(tris) if tris else np.zeros((0, 3), dtype=np.int64)
    return V, UV, F, materials


# --------------------------------------------------------------------------
# mesh helpers
# --------------------------------------------------------------------------
def vertex_normals(V, F):
    n = np.cross(V[F[:, 1]] - V[F[:, 0]], V[F[:, 2]] - V[F[:, 0]])
    nv = np.zeros((len(V), 3))
    for k in range(3):
        np.add.at(nv, F[:, k], n)
    return nv / (np.linalg.norm(nv, axis=1, keepdims=True) + 1e-12)


def cleanup(V, F):
    """Drop degenerate + duplicate faces and weld identical vertices."""
    keep = (F[:, 0] != F[:, 1]) & (F[:, 1] != F[:, 2]) & (F[:, 0] != F[:, 2])
    F = F[keep]
    uniq, inv = np.unique(np.round(V, 6), axis=0, return_inverse=True)
    F = inv.reshape(-1)[F]
    keep = (F[:, 0] != F[:, 1]) & (F[:, 1] != F[:, 2]) & (F[:, 0] != F[:, 2])
    F = F[keep]
    _, u = np.unique(np.sort(F, axis=1), axis=0, return_index=True)
    F = F[np.sort(u)]
    used = np.unique(F)
    remap = np.full(len(uniq), -1, dtype=np.int64)
    remap[used] = np.arange(len(used))
    return uniq[used], remap[F]


def smooth_colours(V, F, C, iterations=3, weight=0.55, angle_cos=0.55):
    """Laplacian smoothing of baked colour, but never across a hard crease."""
    from scipy.sparse import coo_matrix
    N = vertex_normals(V, F)
    e = np.vstack([F[:, [0, 1]], F[:, [1, 2]], F[:, [2, 0]]])
    e = np.vstack([e, e[:, ::-1]])
    align = np.einsum('ij,ij->i', N[e[:, 0]], N[e[:, 1]])
    e = e[align > angle_cos]
    if len(e) == 0:
        return C
    A = coo_matrix((np.ones(len(e)), (e[:, 0], e[:, 1])), shape=(len(V), len(V))).tocsr()
    deg = np.maximum(np.asarray(A.sum(1)).ravel(), 1)[:, None]
    c = C.copy()
    for _ in range(iterations):
        c = (1 - weight) * c + weight * (A @ c / deg)
    return c


def write_fmsh(path, V, N, C, F, flip=False):
    """Little-endian binary mesh, 20-byte header, vertex data 4-byte aligned.

    offset 0  magic  "FMSH"
    offset 4  uint16 version (1)
    offset 6  uint16 flags   (0)
    offset 8  uint32 vertexCount
    offset 12 uint32 triCount
    offset 16 uint32 indexSize (2 or 4 bytes)
    offset 20 vertexCount * (3 float32 position, 3 float32 normal, 4 uint8 RGBA)
              triCount * 3 * indexSize indices
    """
    if flip:                        # glTF (right handed) -> Unity (left handed)
        # Mirroring X turns the outward CCW winding into CW, which is Unity's
        # front-face convention -- so the index order must stay untouched.
        V = V * np.array([-1.0, 1.0, 1.0])
        N = N * np.array([-1.0, 1.0, 1.0])
    vcount, tcount = len(V), len(F)
    idx16 = vcount <= 65535
    with open(path, 'wb') as f:
        f.write(b'FMSH')
        # 20-byte header: magic, version, flags, vertex count, tri count, index size
        f.write(struct.pack('<HHIII', 1, 0, vcount, tcount, 1 if idx16 else 0))
        buf = np.empty((vcount, 6), dtype=np.float32)
        buf[:, 0:3] = V
        buf[:, 3:6] = N
        f.write(buf.tobytes())
        c = np.clip(C * 255.0, 0, 255).astype(np.uint8)
        alpha = np.full((vcount, 1), 255, dtype=np.uint8)
        f.write(np.hstack([c, alpha]).tobytes())
        f.write(F.astype(np.uint16 if idx16 else np.uint32).tobytes())
    return os.path.getsize(path)


def write_fmsh_textured(path, V, N, UV, F):
    """Version 2 of the baked mesh: positions, normals and per-vertex UVs.

    Vertices are duplicated per triangle (the UV mapping jumps between atlas
    islands), so the file is non-indexed: indexSize = 0.
    """
    V = V * np.array([-1.0, 1.0, 1.0])   # glTF -> Unity handedness
    N = N * np.array([-1.0, 1.0, 1.0])
    vcount = len(V)
    buf = np.empty((vcount, 8), dtype=np.float32)
    buf[:, 0:3] = V
    buf[:, 3:6] = N
    buf[:, 6:8] = UV
    with open(path, 'wb') as f:
        f.write(b'FMSH')
        f.write(struct.pack('<HHIII', 2, 0, vcount, len(F), 0))
        f.write(buf.tobytes())
    return os.path.getsize(path)


def barycentric(q, a, b, c, n):
    d = np.dot(q - a, n)
    q = q - n * d
    v0, v1, v2 = b - a, c - a, q - a
    d00, d01, d11 = v0 @ v0, v0 @ v1, v1 @ v1
    d20, d21 = v2 @ v0, v2 @ v1
    denom = d00 * d11 - d01 * d01
    if abs(denom) < 1e-12:
        return 1.0, 0.0, 0.0
    v = (d11 * d20 - d01 * d21) / denom
    w = (d00 * d21 - d01 * d20) / denom
    u = 1.0 - v - w
    # clamp outside-the-triangle projections, then renormalise
    u, v, w = max(0.0, u), max(0.0, v), max(0.0, w)
    total = u + v + w
    if total < 1e-6:
        return 1.0, 0.0, 0.0
    return u / total, v / total, w / total


def transfer_uv(V_orig, F_orig, UV_orig, V_new, F_new):
    """Give every decimated triangle the UV mapping of the closest original
    triangle (projected through its plane). Keeps the texture readable instead
    of smearing atlas islands across big triangles."""
    from scipy.spatial import cKDTree

    centroids = V_orig[F_orig].mean(axis=1)
    normals = np.cross(V_orig[F_orig[:, 1]] - V_orig[F_orig[:, 0]],
                       V_orig[F_orig[:, 2]] - V_orig[F_orig[:, 0]])
    normals /= (np.linalg.norm(normals, axis=1, keepdims=True) + 1e-12)

    new_normals = np.cross(V_new[F_new[:, 1]] - V_new[F_new[:, 0]],
                           V_new[F_new[:, 2]] - V_new[F_new[:, 0]])
    new_normals /= (np.linalg.norm(new_normals, axis=1, keepdims=True) + 1e-12)

    tree = cKDTree(centroids)
    k = 6
    _, idx = tree.query(V_new[F_new].mean(axis=1), k=k)

    verts, uvs = [], []
    for i in range(len(F_new)):
        tri = F_new[i]
        tri_pts = V_new[tri]

        # Pick the source triangle that lies in the same plane: centroid
        # distance alone happily maps a gable onto the roof and smears red
        # tiles down the wall.
        best, best_score = int(idx[i, 0]), 1e9
        for cand in idx[i]:
            cand = int(cand)
            align = float(np.dot(new_normals[i], normals[cand]))
            if align < 0.25:
                continue
            plane = normals[cand]
            dist = float(np.abs((tri_pts - V_orig[F_orig[cand, 0]]) @ plane).mean())
            score = dist * 1000.0 + (1.0 - align)
            if score < best_score:
                best_score, best = score, cand

        o = F_orig[best]
        a, b, c = V_orig[o[0]], V_orig[o[1]], V_orig[o[2]]
        n = normals[best]
        for vi in tri:
            u, v, w = barycentric(V_new[vi], a, b, c, n)
            uv = u * UV_orig[o[0]] + v * UV_orig[o[1]] + w * UV_orig[o[2]]
            verts.append(V_new[vi])
            uvs.append((uv[0] % 1.0, uv[1] % 1.0))

    return np.array(verts), F_new, np.array(uvs)


def write_obj(path, V, UV, F, N, mtl_name, material_names, face_material):
    with open(path, 'w') as f:
        f.write('mtllib %s\n' % mtl_name)
        for v in V:
            f.write('v %.5f %.5f %.5f\n' % tuple(v))
        for t in UV:
            f.write('vt %.6f %.6f\n' % (t[0], 1.0 - t[1]))
        for n in N:
            f.write('vn %.4f %.4f %.4f\n' % tuple(n))
        current = None
        for i, tri in enumerate(F):
            name = material_names[face_material[i]] if material_names else 'mat_0'
            if name != current:
                f.write('usemtl %s\n' % name)
                current = name
            f.write('f %d/%d/%d %d/%d/%d %d/%d/%d\n' % (
                tri[0] + 1, tri[0] + 1, tri[0] + 1,
                tri[1] + 1, tri[1] + 1, tri[1] + 1,
                tri[2] + 1, tri[2] + 1, tri[2] + 1))


# --------------------------------------------------------------------------
# main pipeline
# --------------------------------------------------------------------------
def bake(glb_path, out_dir, key, force_obj=False):
    V, UV, F, materials = read_gltf(glb_path)
    height = V[:, 1].max() - V[:, 1].min()
    print('  %-28s tris=%-7d verts=%-7d height=%.2f' % (os.path.basename(glb_path), len(F), len(V), height))

    os.makedirs(out_dir, exist_ok=True)
    tex = next((m for m in materials if m is not None), None)

    if force_obj or len(F) <= LIGHT_MESH_TRIS:
        # ---- light mesh: OBJ + atlas -------------------------------------
        if tex is not None:
            atlas = tex.resize((ATLAS_SIZE, ATLAS_SIZE), Image.LANCZOS)
            atlas.save(os.path.join(out_dir, key + '_atlas.png'), optimize=True)
        with open(os.path.join(out_dir, key + '.mtl'), 'w') as m:
            m.write('newmtl %s_mat\nKa 1 1 1\nKd 1 1 1\nKs 0 0 0\nd 1\nillum 1\n' % key)
            if tex is not None:
                m.write('map_Kd %s_atlas.png\n' % key)
        write_obj(os.path.join(out_dir, key + '.obj'), V, UV, F, vertex_normals(V, F),
                  key + '.mtl', [key + '_mat'], np.zeros(len(F), dtype=int))
        print('    -> %s.obj (%d tris)' % (key, len(F)))
        return 'obj'

    # ---- heavy scan: decimate, bake vertex colour, custom binary --------
    from fast_simplification import simplify
    from scipy.spatial import cKDTree

    tex_big = tex.resize((1024, 1024), Image.LANCZOS) if tex is not None else None
    C = np.ones((len(V), 3))
    if tex_big is not None:
        ta = np.asarray(tex_big, dtype=np.float64) / 255.0
        tx = np.clip((UV[:, 0] % 1.0) * (ta.shape[0] - 1), 0, ta.shape[0] - 1).astype(int)
        ty = np.clip(((1 - UV[:, 1]) % 1.0) * (ta.shape[1] - 1), 0, ta.shape[1] - 1).astype(int)
        C = ta[ty, tx]

    n0 = vertex_normals(V, F)
    v2, f2 = simplify(V.astype(np.float32), F.astype(np.int64), target_reduction=TARGET_REDUCTION)
    v2, f2 = cleanup(v2.astype(np.float64), f2.astype(np.int64))
    print('    decimated to %d tris / %d verts' % (len(f2), len(v2)))

    n2 = vertex_normals(v2, f2)
    tree = cKDTree(V)
    dist, idx = tree.query(v2, k=12)
    align = np.einsum('vkc,vc->vk', n0[idx], n2)
    score = np.where(align > 0.6, dist + (1 - align) * 0.02, 1e9)
    best = np.argmin(score, axis=1)
    sel = idx[np.arange(len(v2)), best]
    bad = ~np.isfinite(score[np.arange(len(v2)), best])
    sel[bad] = idx[bad, 0]

    if USE_TEXTURE_BAKE and tex_big is not None:
        # textured path: per-triangle UV transfer + the albedo as a texture
        atlas = tex.resize((SCAN_TEX_SIZE, SCAN_TEX_SIZE), Image.LANCZOS)
        atlas.save(os.path.join(out_dir, key + '_albedo.png'), optimize=True)

        tv, tf, tuv = transfer_uv(V, F, UV, v2, f2)
        tn = n2[f2].reshape(-1, 3)
        path = os.path.join(out_dir, key + '.bytes')
        size = write_fmsh_textured(path, tv, tn, tuv, tf)
        print('    -> %s.bytes (%.2f MB, textured, %d verts)' % (key, size / 1048576.0, len(tv)))
        return 'bytes'

    colour = smooth_colours(v2, f2, C[sel])
    path = os.path.join(out_dir, key + '.bytes')
    size = write_fmsh(path, v2, n2, colour, f2, flip=True)
    print('    -> %s.bytes (%.2f MB)' % (key, size / 1048576.0))
    return 'bytes'


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return
    src = sys.argv[1]
    here = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    out_dir = os.path.join(here, 'Assets', 'Resources', 'Models')
    work = '/tmp/farmsim_models'
    os.makedirs(work, exist_ok=True)

    if src.lower().endswith('.zip'):
        with zipfile.ZipFile(src) as z:
            z.extractall(work)
    else:
        import shutil
        shutil.copy(src, work)

    # optional explicit names:  file.glb=key
    naming = {}
    for arg in sys.argv[2:]:
        if '=' in arg:
            a, b = arg.split('=', 1)
            naming[a] = b

    glbs = sorted(f for f in os.listdir(work) if f.lower().endswith(('.glb', '.gltf', '.obj')))
    print('found %d model(s) in %s' % (len(glbs), src))
    for f in glbs:
        key = naming.get(f) or os.path.splitext(f)[0].replace(' ', '_').replace('(', '').replace(')', '')
        print('*', f, '->', key)
        if f.lower().endswith('.obj'):
            import shutil
            shutil.copy(os.path.join(work, f), os.path.join(out_dir, key + '.obj'))
            continue
        bake(os.path.join(work, f), out_dir, key)


if __name__ == '__main__':
    main()
