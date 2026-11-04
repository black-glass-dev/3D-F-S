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

        /// <summary>Two crossed quads (pivot at the bottom centre) for grass tufts.</summary>
        public static Mesh Tuft
        {
            get
            {
                if (_tuft == null) _tuft = BuildTuft();
                return _tuft;
            }
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
