#!/usr/bin/env python3
"""Minimal GLB (binary glTF 2.0) -> OBJ + MTL + PNG converter.

Keeps every primitive as its own material group, bakes node transforms into
the vertices and writes out the textures that are embedded in the .glb.
Used once to bring the user's models into Unity (which imports OBJ natively).
"""
import json, os, struct, sys

COMPONENT = {5120: ('b', 1), 5121: ('B', 1), 5122: ('h', 2), 5123: ('H', 2),
             5125: ('I', 4), 5126: ('f', 4)}
NCOMP = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}


def load_glb(path):
    data = open(path, 'rb').read()
    magic, version, length = struct.unpack('<III', data[:12])
    assert magic == 0x46546C67, 'not a glb'
    off, js, bin_chunk = 12, None, b''
    while off < len(data):
        clen, ctype = struct.unpack('<II', data[off:off + 8])
        chunk = data[off + 8:off + 8 + clen]
        if ctype == 0x4E4F534A:
            js = json.loads(chunk.decode('utf-8'))
        elif ctype == 0x004E4942:
            bin_chunk = chunk
        off += 8 + clen
    return js, bin_chunk


def accessor(js, blob, index):
    acc = js['accessors'][index]
    fmt, size = COMPONENT[acc['componentType']]
    n = NCOMP[acc['type']]
    count = acc['count']
    bv = js['bufferViews'][acc['bufferView']]
    start = bv.get('byteOffset', 0) + acc.get('byteOffset', 0)
    stride = bv.get('byteStride') or (size * n)
    out = []
    for i in range(count):
        base = start + i * stride
        out.append(struct.unpack_from('<' + fmt * n, blob, base))
    return out


def mat_mul(a, b):
    return [sum(a[r * 4 + k] * b[k * 4 + c] for k in range(4)) for r in range(4) for c in range(4)]


def node_matrix(node):
    if 'matrix' in node:
        m = node['matrix']  # column-major
        return [m[0], m[4], m[8], m[12],
                m[1], m[5], m[9], m[13],
                m[2], m[6], m[10], m[14],
                m[3], m[7], m[11], m[15]]
    t = node.get('translation', [0, 0, 0])
    r = node.get('rotation', [0, 0, 0, 1])
    s = node.get('scale', [1, 1, 1])
    x, y, z, w = r
    rot = [1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w), 0,
           2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w), 0,
           2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y), 0,
           0, 0, 0, 1]
    scl = [s[0], 0, 0, 0, 0, s[1], 0, 0, 0, 0, s[2], 0, 0, 0, 0, 1]
    trs = mat_mul(rot, scl)
    trs[3], trs[7], trs[11] = t[0], t[1], t[2]
    return trs


def transform(m, p):
    x, y, z = p
    return (m[0] * x + m[1] * y + m[2] * z + m[3],
            m[4] * x + m[5] * y + m[6] * z + m[7],
            m[8] * x + m[9] * y + m[10] * z + m[11])


def convert(glb_path, out_dir, name):
    js, blob = load_glb(glb_path)
    os.makedirs(out_dir, exist_ok=True)

    # ---- textures ----
    tex_files = {}
    for i, img in enumerate(js.get('images', [])):
        bv = js['bufferViews'][img['bufferView']]
        raw = blob[bv.get('byteOffset', 0): bv.get('byteOffset', 0) + bv['byteLength']]
        ext = '.png' if img.get('mimeType', 'image/png').endswith('png') else '.jpg'
        fname = 'tex%d%s' % (i, ext)
        with open(os.path.join(out_dir, fname), 'wb') as f:
            f.write(raw)
        tex_files[i] = fname

    def material_texture(mat):
        pbr = mat.get('pbrMetallicRoughness', {})
        if 'baseColorTexture' in pbr:
            tex_index = pbr['baseColorTexture']['index']
            src = js['textures'][tex_index].get('source')
            return tex_files.get(src)
        return None

    # ---- mtl ----
    mtl_name = name + '.mtl'
    with open(os.path.join(out_dir, mtl_name), 'w') as mtl:
        for i, mat in enumerate(js.get('materials', [])):
            mtl.write('newmtl mat_%d\nKa 1 1 1\nKd 1 1 1\nKs 0 0 0\nd 1\nillum 1\n' % i)
            tex = material_texture(mat)
            if tex:
                mtl.write('map_Kd %s\n' % tex)
            mtl.write('\n')

    # ---- obj ----
    verts, uvs, normals, faces = [], [], [], []
    groups = []
    vnorm_off = uv_off = 0

    def walk(node_index, parent_matrix):
        nonlocal vnorm_off, uv_off
        node = js['nodes'][node_index]
        world = mat_mul(parent_matrix, node_matrix(node))

        if 'mesh' in node:
            mesh = js['meshes'][node['mesh']]
            for prim in mesh['primitives']:
                if prim.get('mode', 4) != 4:
                    continue
                pos = accessor(js, blob, prim['attributes']['POSITION'])
                nrm = accessor(js, blob, prim['attributes']['NORMAL']) if 'NORMAL' in prim['attributes'] else None
                uv = accessor(js, blob, prim['attributes']['TEXCOORD_0']) if 'TEXCOORD_0' in prim['attributes'] else None
                idx = [x[0] for x in accessor(js, blob, prim['indices'])] if 'indices' in prim else list(range(len(pos)))

                gname = '%s_%d' % (node.get('name', 'part'), prim.get('material', 0))
                groups.append(gname)
                faces.append('g %s\nusemtl mat_%d\n' % (gname, prim.get('material', 0)))

                for p in pos:
                    verts.append(transform(world, p))
                if nrm:
                    for n in nrm:
                        normals.append(transform(world[:12] + [0, 0, 0, 1], n))
                if uv:
                    for t in uv:
                        uvs.append(t)

                for i in range(0, len(idx), 3):
                    tri = idx[i:i + 3]
                    if len(tri) < 3:
                        break
                    line = []
                    for k in tri:
                        vi = len(verts) - len(pos) + k + 1
                        s = str(vi)
                        if uv:
                            s += '/%d' % (uv_off + k + 1)
                        line.append(s)
                    faces.append('f %s\n' % ' '.join(line))

                if uv:
                    uv_off += len(uv)

        for child in node.get('children', []):
            walk(child, world)

    identity = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]
    scene = js.get('scenes', [{}])[js.get('scene', 0)]
    for root in scene.get('nodes', []):
        walk(root, identity)

    obj_path = os.path.join(out_dir, name + '.obj')
    with open(obj_path, 'w') as obj:
        obj.write('# converted from %s\nmtllib %s\n' % (os.path.basename(glb_path), mtl_name))
        for v in verts:
            obj.write('v %.6f %.6f %.6f\n' % v)
        for t in uvs:
            obj.write('vt %.6f %.6f\n' % (t[0], 1.0 - t[1]))
        for n in normals:
            obj.write('vn %.6f %.6f %.6f\n' % n)
        for f in faces:
            obj.write(f)

    xs = [v[0] for v in verts]; ys = [v[1] for v in verts]; zs = [v[2] for v in verts]
    print('%-24s verts=%6d tris=%6d  size=%.2f x %.2f x %.2f' % (
        name, len(verts), len(faces) - len(groups), max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)))
    return obj_path


if __name__ == '__main__':
    convert(sys.argv[1], sys.argv[2], sys.argv[3])
