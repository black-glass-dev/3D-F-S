using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FarmSim
{
    /// <summary>
    /// v1.5.0 optimisation: folds every static prop that shares a material into
    /// one mesh. The world is built out of hundreds of small objects (tufts,
    /// pebbles, fence posts, tree canopies, every single box of a building), and
    /// on a phone the per-renderer cost — culling, sorting, and the shadow pass —
    /// matters more than the triangle count.
    ///
    /// Only things that can never move are touched: children of the static
    /// world root, with a single-submesh mesh and no extra behaviour components.
    /// The result is uploaded to the GPU and the CPU copy dropped, so the merged
    /// meshes also free memory instead of costing it.
    /// </summary>
    public static class StaticMeshCombiner
    {
        private const int VertexBudget = 60000;

        public static int Combine(Transform root)
        {
            if (root == null) return 0;

            var groups = new Dictionary<long, List<MeshFilter>>();
            var materials = new Dictionary<long, Material>();
            var filters = root.GetComponentsInChildren<MeshFilter>(true);

            for (int i = 0; i < filters.Length; i++)
            {
                var mf = filters[i];
                if (mf == null) continue;

                var mesh = mf.sharedMesh;
                if (mesh == null || !mesh.isReadable || mesh.subMeshCount != 1) continue;

                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mr.sharedMaterial == null) continue;

                // Anything with behaviour on it keeps its own GameObject.
                if (mf.GetComponents<Component>().Length > 3) continue;

                long key = (long)mr.sharedMaterial.GetInstanceID() * 31L +
                           (long)mr.shadowCastingMode * 7L +
                           (mr.receiveShadows ? 1L : 0L);

                List<MeshFilter> list;
                if (!groups.TryGetValue(key, out list))
                {
                    list = new List<MeshFilter>();
                    groups[key] = list;
                    materials[key] = mr.sharedMaterial;
                }
                list.Add(mf);
            }

            int merged = 0;
            int created = 0;

            foreach (var pair in groups)
            {
                var list = pair.Value;
                if (list.Count < 2) continue;

                var chunk = new List<MeshFilter>();
                int verts = 0;

                for (int i = 0; i < list.Count; i++)
                {
                    var mf = list[i];
                    int count = mf.sharedMesh.vertexCount;
                    if (verts + count > VertexBudget && chunk.Count > 0)
                    {
                        Flush(chunk, materials[pair.Key], root, ref merged);
                        created++;
                        chunk.Clear();
                        verts = 0;
                    }
                    chunk.Add(mf);
                    verts += count;
                }

                if (chunk.Count > 0)
                {
                    Flush(chunk, materials[pair.Key], root, ref merged);
                    created++;
                }
            }

            Debug.Log("[FarmSim] mesh combine: " + merged + " renderers folded into " +
                      created + " batches");
            return merged;
        }

        private static void Flush(List<MeshFilter> chunk, Material material, Transform root, ref int merged)
        {
            if (chunk.Count == 0 || material == null) return;

            var first = chunk[0].GetComponent<MeshRenderer>();
            ShadowCastingMode shadows = first != null ? first.shadowCastingMode : ShadowCastingMode.On;
            bool receive = first == null || first.receiveShadows;

            int total = 0;
            for (int i = 0; i < chunk.Count; i++) total += chunk[i].sharedMesh.vertexCount;

            var combines = new CombineInstance[chunk.Count];
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            for (int i = 0; i < chunk.Count; i++)
            {
                var mf = chunk[i];
                combines[i].mesh = mf.sharedMesh;
                combines[i].subMeshIndex = 0;
                combines[i].transform = toRoot * mf.transform.localToWorldMatrix;
            }

            var mesh = new Mesh { name = "Static_" + material.name };
            mesh.indexFormat = total > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.CombineMeshes(combines, true, true);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);

            var go = new GameObject("Static_" + material.name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows;
            renderer.receiveShadows = receive;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            for (int i = 0; i < chunk.Count; i++)
            {
                var mf = chunk[i];
                if (mf != null) Object.Destroy(mf.gameObject);
                merged++;
            }
        }
    }
}
