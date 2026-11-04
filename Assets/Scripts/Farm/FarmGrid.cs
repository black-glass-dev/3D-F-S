using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>Builds the 6 x 4 plot grid that makes up the farm field.</summary>
    public class FarmGrid : MonoBehaviour
    {
        public const int Cols = 6;
        public const int Rows = 4;
        private const float Spacing = 1.35f;

        private readonly List<FarmPlot> _plots = new List<FarmPlot>();

        public IReadOnlyList<FarmPlot> Plots
        {
            get { return _plots; }
        }

        public static float FieldWidth
        {
            get { return (Cols - 1) * Spacing; }
        }

        public static float FieldDepth
        {
            get { return (Rows - 1) * Spacing; }
        }

        public void Build()
        {
            // A darker soil bed under the whole field so the grid reads as one field.
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "FieldBed";
            bed.transform.SetParent(transform, false);
            bed.transform.localPosition = new Vector3(0f, 0.008f, 0f);
            bed.transform.localScale = new Vector3(FieldWidth + 1.3f, 0.04f, FieldDepth + 1.3f);
            bed.GetComponent<Renderer>().material =
                MaterialCache.Get(new Color(0.40f, 0.27f, 0.16f), SurfacePattern.Soil);
            Destroy(bed.GetComponent<Collider>());

            float width = FieldWidth;
            float depth = FieldDepth;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var go = new GameObject("Plot_" + c + "_" + r);
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(
                        c * Spacing - width / 2f, 0f, r * Spacing - depth / 2f);

                    var collider = go.AddComponent<BoxCollider>();
                    collider.size = new Vector3(1.15f, 0.6f, 1.15f);
                    collider.center = new Vector3(0f, 0.2f, 0f);

                    var plot = go.AddComponent<FarmPlot>();
                    plot.Init();
                    _plots.Add(plot);
                }
            }
        }

        public void OnNewDay()
        {
            for (int i = 0; i < _plots.Count; i++)
                _plots[i].OnNewDay();
        }

        public List<PlotSaveData> CollectSaveData()
        {
            var list = new List<PlotSaveData>(_plots.Count);
            for (int i = 0; i < _plots.Count; i++)
                list.Add(_plots[i].GetSaveData());
            return list;
        }

        public void LoadSaveData(List<PlotSaveData> data)
        {
            if (data == null) return;
            int count = Mathf.Min(data.Count, _plots.Count);
            for (int i = 0; i < count; i++)
                _plots[i].LoadSaveData(data[i]);
        }
    }
}
