using UnityEngine;

namespace FarmSim
{
    public enum PlotState
    {
        Empty = 0,
        Growing = 1,
        Ready = 2
    }

    /// <summary>
    /// One soil plot. Tap flow: Empty -> plant selected seed,
    /// Growing -> water (speeds growth), Ready -> harvest & sell.
    /// All visuals are procedural low-poly primitives (no external assets).
    /// </summary>
    public class FarmPlot : MonoBehaviour
    {
        public PlotState State { get; private set; }
        public int CropIndex { get; private set; }
        public float Progress { get; private set; }
        public bool Watered { get; private set; }

        private GameObject _cropRoot;
        private GameObject _sprout;
        private GameObject _fruit;
        private Renderer _soilRenderer;
        private Material _soilDry;
        private Material _soilWet;

        public void Init(Material soilDry, Material soilWet)
        {
            _soilDry = soilDry;
            _soilWet = soilWet;
            State = PlotState.Empty;
            CropIndex = 0;
            Progress = 0f;

            // Soil block
            var soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.transform.SetParent(transform, false);
            soil.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            soil.transform.localScale = new Vector3(1f, 0.12f, 1f);
            soil.name = "Soil";
            _soilRenderer = soil.GetComponent<Renderer>();
            _soilRenderer.material = soilDry;
            // The tap collider lives on the plot root (added by FarmGrid).

            // Furrow lines for a tilled look
            for (int i = -1; i <= 1; i++)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.transform.SetParent(transform, false);
                line.transform.localPosition = new Vector3(i * 0.3f, 0.12f, 0f);
                line.transform.localScale = new Vector3(0.07f, 0.03f, 0.92f);
                line.GetComponent<Renderer>().material = soilWet;
                Object.Destroy(line.GetComponent<Collider>());
            }

            _cropRoot = new GameObject("Crop");
            _cropRoot.transform.SetParent(transform, false);
            _cropRoot.transform.localPosition = new Vector3(0f, 0.11f, 0f);

            _sprout = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _sprout.name = "Sprout";
            _sprout.transform.SetParent(_cropRoot.transform, false);
            _sprout.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            Object.Destroy(_sprout.GetComponent<Collider>());

            _fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _fruit.name = "Fruit";
            _fruit.transform.SetParent(_cropRoot.transform, false);
            _fruit.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            Object.Destroy(_fruit.GetComponent<Collider>());

            RefreshVisual();
        }

        private void Update()
        {
            if (State == PlotState.Ready)
            {
                RefreshVisual(); // bobbing animation for ripe crops
                return;
            }
            if (State != PlotState.Growing) return;
            var crop = CropDatabase.Get(CropIndex);
            float speed = Watered ? 1.8f : 1f;
            Progress += (Time.deltaTime * speed) / crop.GrowSeconds;
            if (Progress >= 1f)
            {
                Progress = 1f;
                State = PlotState.Ready;
            }
            RefreshVisual();
        }

        /// <summary>Small growth boost when a new day starts; dry soil dries out.</summary>
        public void OnNewDay()
        {
            if (State == PlotState.Growing)
            {
                Progress = Mathf.Min(1f, Progress + 0.25f);
                if (Progress >= 1f) State = PlotState.Ready;
                Watered = false;
                RefreshVisual();
            }
        }

        public void OnTap()
        {
            switch (State)
            {
                case PlotState.Empty:
                {
                    var crop = CropDatabase.Get(GameManager.Instance.SelectedCropIndex);
                    if (!GameManager.Instance.SpendCoins(crop.SeedCost))
                    {
                        UIManager.Instance.ShowToast(Localization.T("not_enough_coins"));
                        return;
                    }
                    CropIndex = GameManager.Instance.SelectedCropIndex;
                    Progress = 0f;
                    Watered = false;
                    State = PlotState.Growing;
                    RefreshVisual();
                    UIManager.Instance.ShowToast(Localization.T("planted") + " " + crop.DisplayName);
                    GameManager.Instance.SaveNow();
                    break;
                }
                case PlotState.Growing:
                {
                    if (!Watered)
                    {
                        Watered = true;
                        UIManager.Instance.ShowToast(Localization.T("watered"));
                    }
                    else
                    {
                        UIManager.Instance.ShowToast(Localization.T("already_watered"));
                    }
                    RefreshVisual();
                    break;
                }
                case PlotState.Ready:
                {
                    var crop = CropDatabase.Get(CropIndex);
                    GameManager.Instance.AddCoins(crop.SellPrice);
                    UIManager.Instance.ShowToast(Localization.T("harvested") + crop.SellPrice);
                    State = PlotState.Empty;
                    Progress = 0f;
                    Watered = false;
                    RefreshVisual();
                    GameManager.Instance.SaveNow();
                    break;
                }
            }
        }

        private void RefreshVisual()
        {
            if (_soilRenderer != null)
                _soilRenderer.material = Watered ? _soilWet : _soilDry;

            if (_cropRoot == null) return;

            var crop = CropDatabase.Get(CropIndex);
            bool hasCrop = State != PlotState.Empty;
            _cropRoot.SetActive(hasCrop);
            if (!hasCrop) return;

            float t = State == PlotState.Ready ? 1f : Progress;
            // Sprout: green ball that grows with progress
            float sproutScale = Mathf.Lerp(0.12f, 0.55f, t);
            _sprout.transform.localScale = new Vector3(sproutScale, sproutScale * 1.25f, sproutScale);
            _sprout.GetComponent<Renderer>().material = SharedMaterial(crop.SproutColor);
            // Fruit: appears in the second half, full & ripe-coloured when ready
            bool showFruit = t > 0.5f;
            _fruit.SetActive(showFruit);
            if (showFruit)
            {
                float fruitScale = Mathf.Lerp(0.10f, 0.42f, (t - 0.5f) * 2f);
                _fruit.transform.localScale = Vector3.one * fruitScale;
                var fruitRenderer = _fruit.GetComponent<Renderer>();
                fruitRenderer.material = SharedMaterial(State == PlotState.Ready ? crop.RipeColor : crop.SproutColor);
                if (State == PlotState.Ready)
                {
                    // Gentle bob so ready crops catch the eye
                    float bob = Mathf.Sin(Time.time * 3f + transform.position.x) * 0.05f;
                    _fruit.transform.localPosition = new Vector3(0f, 0.42f + bob, 0f);
                }
            }
        }

        private static Material SharedMaterial(Color color)
        {
            // Small per-colour cache to avoid creating materials every frame.
            return MaterialCache.Get(color);
        }

        public PlotSaveData GetSaveData()
        {
            return new PlotSaveData
            {
                State = (int)State,
                CropIndex = CropIndex,
                Progress = Progress,
                Watered = Watered
            };
        }

        public void LoadSaveData(PlotSaveData data)
        {
            State = (PlotState)data.State;
            CropIndex = data.CropIndex;
            Progress = data.Progress;
            Watered = data.Watered;
            RefreshVisual();
        }
    }

    /// <summary>Tiny colour -> material cache shared by all plots.</summary>
    public static class MaterialCache
    {
        private static readonly System.Collections.Generic.Dictionary<Color, Material> Cache =
            new System.Collections.Generic.Dictionary<Color, Material>();

        public static Material Get(Color color)
        {
            Material mat;
            if (!Cache.TryGetValue(color, out mat))
            {
                mat = new Material(Shader.Find("Standard")) { color = color };
                Cache[color] = mat;
            }
            return mat;
        }
    }
}
