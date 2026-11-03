using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>Builds the 6 x 4 plot grid that makes up the MVP farm field.</summary>
    public class FarmGrid : MonoBehaviour
    {
        public const int Cols = 6;
        public const int Rows = 4;
        private const float Spacing = 1.25f;

        private readonly List<FarmPlot> _plots = new List<FarmPlot>();

        public void Build()
        {
            var soilDry = MaterialCache.Get(new Color(0.42f, 0.28f, 0.15f));
            var soilWet = MaterialCache.Get(new Color(0.25f, 0.16f, 0.09f));

            float width = (Cols - 1) * Spacing;
            float depth = (Rows - 1) * Spacing;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var go = new GameObject("Plot_" + c + "_" + r);
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(c * Spacing - width / 2f, 0f, r * Spacing - depth / 2f);

                    // Tap target
                    var col = go.AddComponent<BoxCollider>();
                    col.size = new Vector3(1.1f, 0.5f, 1.1f);
                    col.center = new Vector3(0f, 0.15f, 0f);

                    var plot = go.AddComponent<FarmPlot>();
                    plot.Init(soilDry, soilWet);
                    _plots.Add(plot);
                }
            }
        }

        public void OnNewDay()
        {
            foreach (var plot in _plots)
                plot.OnNewDay();
        }

        public List<PlotSaveData> CollectSaveData()
        {
            var list = new List<PlotSaveData>(_plots.Count);
            foreach (var plot in _plots)
                list.Add(plot.GetSaveData());
            return list;
        }

        public void LoadSaveData(List<PlotSaveData> data)
        {
            if (data == null) return;
            int n = Mathf.Min(data.Count, _plots.Count);
            for (int i = 0; i < n; i++)
                _plots[i].LoadSaveData(data[i]);
        }
    }
}
