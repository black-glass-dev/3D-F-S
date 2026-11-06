using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Tiny procedural mesh library: cones (pines, hills), crossed grass quads
    /// and a rounded-ish box. Keeps the project asset-free while giving the
    /// world shapes that read better than raw Unity primitives.
    /// </summary>
    public static class MeshFactory
    {
        private static Mesh _cone;
        private static Mesh _tuft;
        private static Mesh _thinCone;
        private static Mesh _roof;
        private static Mesh _plotSoil;
        private static Mesh _trunk;
        private static Mesh _bush;
        private static Mesh _disc;
        private static Mesh[] _rocks;

        /// <summary>Cone with base radius 0.5 at y=0 and apex at y=1.</summary>
        public static Mesh Cone
        {
            get
            {
                if (_cone == null) _cone = BuildCone(14, 0.5f, 1f, "Cone");
                return _cone;
            }
        }

        /// <summary>Narrow cone (tree pine layer).</summary>
        public static Mesh ThinCone
        {
            get
            {
                if (_thinCone == null) _thinCone = BuildCone(12, 0.5f, 1f, "ThinCone");
                return _thinCone;
            }
        }

        /// <summary>
        /// v1.7.0: one mesh holds the whole soil tile — slab, three furrows and a
        /// couple of pebble bumps. The old code built six separate primitives per
        /// plot, so a 36-plot field carried 216 renderers before a single seed was
        /// planted. Every plot now shares this mesh: one draw batch, one material
        /// switch between dry and watered soil.
        /// </summary>
        public static Mesh PlotSoil
        {
            get
            {
                if (_plotSoil == null) _plotSoil = BuildPlotSoil();
                return _plotSoil;
            }
        }

        private static Mesh BuildPlotSoil()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            AppendBox(verts, uvs, tris, new Vector3(0f, 0.06f, 0f), new Vector3(1f, 0.12f, 1f));
            for (int i = 0; i < 3; i++)
                AppendBox(verts, uvs, tris,
                    new Vector3(-0.28f + i * 0.28f, 0.135f, 0f), new Vector3(0.10f, 0.035f, 0.86f));
            AppendBox(verts, uvs, tris, new Vector3(0.30f, 0.135f, 0.26f), new Vector3(0.09f, 0.05f, 0.07f));
            AppendBox(verts, uvs, tris, new Vector3(-0.24f, 0.135f, -0.30f), new Vector3(0.07f, 0.045f, 0.08f));

            var mesh = new Mesh { name = "PlotSoil" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AppendBox(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3 x = new Vector3(h.x, 0f, 0f);
            Vector3 y = new Vector3(0f, h.y, 0f);
            Vector3 z = new Vector3(0f, 0f, h.z);

            // +X / -X
            AppendQuad(verts, uvs, tris, center + x - y - z, center + x - y + z, center + x + y + z, center + x + y - z, Vector3.right);
            AppendQuad(verts, uvs, tris, center - x - y + z, center - x - y - z, center - x + y - z, center - x + y + z, Vector3.left);
            // +Y / -Y
            AppendQuad(verts, uvs, tris, center - x + y - z, center + x + y - z, center + x + y + z, center - x + y + z, Vector3.up);
            AppendQuad(verts, uvs, tris, center - x - y + z, center + x - y + z, center + x - y - z, center - x - y - z, Vector3.down);
            // +Z / -Z
            AppendQuad(verts, uvs, tris, center - x - y + z, center + x - y + z, center + x + y + z, center - x + y + z, Vector3.forward);
            AppendQuad(verts, uvs, tris, center + x - y - z, center - x - y - z, center - x + y - z, center + x + y - z, Vector3.back);
        }

        /// <summary>Adds a quad and fixes its winding to face <paramref name="normal"/>.</summary>
        private static void AppendQuad(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int i = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            verts.Add(d);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f)
            {
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            }
            else
            {
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
            }
        }

        /// <summary>Two crossed quads (pivot at the bottom centre) for grass tufts.</summary>
        public static Mesh Tuft
        {
            get
            {
                if (_tuft == null) _tuft = BuildTuft();
                return _tuft;
            }
        }


        /// <summary>Gable roof: 1x1 footprint, ridge along X at height 1.</summary>
        public static Mesh Roof
        {
            get
            {
                if (_roof == null) _roof = BuildRoof();
                return _roof;
            }
        }

        /// <summary>Tapered tree trunk: bottom radius 0.5, top radius 0.18, height 1.</summary>
        public static Mesh Trunk
        {
            get
            {
                if (_trunk == null) _trunk = BuildTrunk();
                return _trunk;
            }
        }

        /// <summary>Soft lump used for bushes (a squashed, noiseless blob).</summary>
        public static Mesh Bush
        {
            get
            {
                if (_bush == null) _bush = BuildBlob(5, 0.16f, 1.0f);
                return _bush;
            }
        }

        /// <summary>Horizontal disc with white vertex colours — used for soft contact shadows.</summary>
        public static Mesh Disc
        {
            get
            {
                if (_disc == null) _disc = BuildDisc(18);
                return _disc;
            }
        }

        /// <summary>One of four lumpy rock shapes.</summary>
        public static Mesh Rock(int variant)
        {
            if (_rocks == null)
            {
                _rocks = new Mesh[4];
                for (int i = 0; i < _rocks.Length; i++)
                    _rocks[i] = BuildBlob(3 + i, 0.34f, 0.72f);
            }
            int index = ((variant % _rocks.Length) + _rocks.Length) % _rocks.Length;
            return _rocks[index];
        }

        private static void AddTri(List<int> tris, List<Vector3> verts, int a, int b, int c, Vector3 inside)
        {
            Vector3 normal = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (Vector3.Dot(normal, verts[a] - inside) < 0f)
            {
                int swap = b;
                b = c;
                c = swap;
            }
            tris.Add(a);
            tris.Add(b);
            tris.Add(c);
        }

        private static void AddQuad(List<int> tris, List<Vector3> verts, int a, int b, int c, int d, Vector3 inside)
        {
            AddTri(tris, verts, a, b, c, inside);
            AddTri(tris, verts, a, c, d, inside);
        }

        private static Mesh BuildRoof()
        {
            var verts = new List<Vector3>
            {
                new Vector3(-0.5f, 0f, 0.5f),   // 0 front-left eave
                new Vector3(0.5f, 0f, 0.5f),    // 1 front-right eave
                new Vector3(0.5f, 1f, 0f),      // 2 ridge right
                new Vector3(-0.5f, 1f, 0f),     // 3 ridge left
                new Vector3(-0.5f, 0f, -0.5f),  // 4 back-left eave
                new Vector3(0.5f, 0f, -0.5f)    // 5 back-right eave
            };
            var inside = new Vector3(0f, 0.35f, 0f);
            var tris = new List<int>();

            AddQuad(tris, verts, 0, 1, 2, 3, inside);   // front slope
            AddQuad(tris, verts, 4, 3, 2, 5, inside);   // back slope
            AddTri(tris, verts, 0, 3, 4, inside);       // left gable
            AddTri(tris, verts, 1, 5, 2, inside);       // right gable
            AddQuad(tris, verts, 0, 4, 5, 1, inside);   // underside

            var uvs = new List<Vector2>();
            for (int i = 0; i < verts.Count; i++)
                uvs.Add(new Vector2(verts[i].x + 0.5f, verts[i].z + 0.5f));

            var mesh = new Mesh { name = "Roof" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildTrunk()
        {
            const int segments = 10;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var inside = new Vector3(0f, 0.5f, 0f);

            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
                verts.Add(new Vector3(Mathf.Cos(a) * 0.20f, 1f, Mathf.Sin(a) * 0.20f));
                uvs.Add(new Vector2(i / (float)segments, 0f));
                uvs.Add(new Vector2(i / (float)segments, 1f));
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int bottomA = i * 2;
                int topA = i * 2 + 1;
                int bottomB = next * 2;
                int topB = next * 2 + 1;
                AddQuad(tris, verts, bottomA, topA, topB, bottomB, inside);
            }

            var mesh = new Mesh { name = "Trunk" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildBlob(int seed, float amount, float squash)
        {
            const int segments = 12;
            const int rings = 7;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var inside = Vector3.zero;
            float offset = seed * 13.7f;

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = ring / (float)rings;
                float phi = v * Mathf.PI;
                float y = Mathf.Cos(phi);
                float radius = Mathf.Sin(phi);

                for (int seg = 0; seg < segments; seg++)
                {
                    float u = seg / (float)segments;
                    float theta = u * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(theta) * radius, y, Mathf.Sin(theta) * radius);

                    float noise = Mathf.PerlinNoise(dir.x * 1.6f + offset, dir.z * 1.6f + offset + dir.y * 1.3f);
                    float scale = 1f + (noise - 0.5f) * amount * 2f;
                    var p = dir * 0.5f * scale;
                    p.y *= squash;

                    verts.Add(p);
                    uvs.Add(new Vector2(u, v));
                }
            }

            for (int ring = 0; ring < rings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int nextSeg = (seg + 1) % segments;
                    int a = ring * segments + seg;
                    int b = ring * segments + nextSeg;
                    int c = (ring + 1) * segments + nextSeg;
                    int d = (ring + 1) * segments + seg;
                    AddQuad(tris, verts, a, b, c, d, inside);
                }
            }

            var mesh = new Mesh { name = "Blob" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildDisc(int segments)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var tris = new List<int>();
            var inside = new Vector3(0f, -1f, 0f);

            verts.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));
            colors.Add(Color.white);

            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
                uvs.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
                colors.Add(Color.white);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddTri(tris, verts, 0, 1 + i, 1 + next, inside);
            }

            var mesh = new Mesh { name = "Disc" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildCone(int segments, float radius, float height, string name)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // apex
            verts.Add(new Vector3(0f, height, 0f));
            uvs.Add(new Vector2(0.5f, 1f));

            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                uvs.Add(new Vector2(i / (float)segments, 0f));
            }

            // base ring centre (for the cap)
            int capCenter = verts.Count;
            verts.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                // side
                tris.Add(0);
                tris.Add(1 + next);
                tris.Add(1 + i);
                // cap
                tris.Add(capCenter);
                tris.Add(1 + i);
                tris.Add(1 + next);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildTuft()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int start = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
            }

            AddQuad(new Vector3(-0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f),
                    new Vector3(0.5f, 1f, 0f), new Vector3(0.5f, 0f, 0f));
            AddQuad(new Vector3(0f, 0f, -0.5f), new Vector3(0f, 1f, -0.5f),
                    new Vector3(0f, 1f, 0.5f), new Vector3(0f, 0f, 0.5f));

            // Normals point straight up so the cards receive the same soft
            // top-light as the ground instead of shading like flat walls.
            var normals = new List<Vector3>(verts.Count);
            for (int i = 0; i < verts.Count; i++) normals.Add(Vector3.up);

            var mesh = new Mesh { name = "Tuft" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
