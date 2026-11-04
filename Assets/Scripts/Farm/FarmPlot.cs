using System.Collections;
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
    /// One soil plot. Tap flow: Empty -> plant the selected seed,
    /// Growing -> water (1.8x faster), Ready -> harvest and sell.
    /// The whole visual is built from primitives plus generated textures.
    /// </summary>
    public class FarmPlot : MonoBehaviour
    {
        public PlotState State { get; private set; }
        public int CropIndex { get; private set; }
        public float Progress { get; private set; }
        public bool Watered { get; private set; }

        private CropVisual _crop;
        private Transform _cropRoot;
        private Renderer _soilRenderer;
        private Renderer[] _furrowRenderers;
        private Material _soilDryMat;
        private Material _soilWetMat;

        private float _sparkleTimer;
        private float _lastAppliedProgress = -1f;
        private bool _readySignalled;

        public void Init()
        {
            _soilDryMat = MaterialCache.Get(new Color(0.56f, 0.37f, 0.21f), SurfacePattern.Soil);
            _soilWetMat = MaterialCache.Get(new Color(0.33f, 0.20f, 0.11f), SurfacePattern.SoilWet);

            State = PlotState.Empty;
            CropIndex = 0;
            Progress = 0f;
            Watered = false;

            // --- soil slab ---
            var soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "Soil";
            soil.transform.SetParent(transform, false);
            soil.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            soil.transform.localScale = new Vector3(1f, 0.12f, 1f);
            _soilRenderer = soil.GetComponent<Renderer>();
            _soilRenderer.material = _soilDryMat;
            Destroy(soil.GetComponent<Collider>());

            // --- tilled furrows ---
            _furrowRenderers = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var furrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                furrow.name = "Furrow";
                furrow.transform.SetParent(transform, false);
                furrow.transform.localPosition = new Vector3(-0.28f + i * 0.28f, 0.13f, 0f);
                furrow.transform.localScale = new Vector3(0.10f, 0.035f, 0.86f);
                var renderer = furrow.GetComponent<Renderer>();
                renderer.material = _soilDryMat;
                _furrowRenderers[i] = renderer;
                Destroy(furrow.GetComponent<Collider>());
            }

            // --- a few pebbles so empty plots still read as "soil" ---
            for (int i = 0; i < 4; i++)
            {
                var pebble = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pebble.name = "Pebble";
                pebble.transform.SetParent(transform, false);
                pebble.transform.localPosition = new Vector3(Random.Range(-0.38f, 0.38f), 0.135f, Random.Range(-0.38f, 0.38f));
                float size = Random.Range(0.05f, 0.09f);
                pebble.transform.localScale = new Vector3(size, size * 0.6f, size);
                pebble.transform.localRotation = Quaternion.Euler(Random.Range(0f, 30f), Random.Range(0f, 360f), 0f);
                pebble.GetComponent<Renderer>().material =
                    MaterialCache.Get(new Color(0.42f, 0.33f, 0.26f), SurfacePattern.Stone);
                Destroy(pebble.GetComponent<Collider>());
            }

            _cropRoot = new GameObject("CropRoot").transform;
            _cropRoot.SetParent(transform, false);
            _cropRoot.localPosition = new Vector3(0f, 0.12f, 0f);
        }

        private void Update()
        {
            if (State == PlotState.Growing)
            {
                var crop = CropDatabase.Get(CropIndex);
                float speed = Watered ? 1.8f : 1f;
                Progress += (Time.deltaTime * speed) / Mathf.Max(1f, crop.GrowSeconds);
                if (Progress >= 1f)
                {
                    Progress = 1f;
                    State = PlotState.Ready;
                    OnBecameReady();
                }
                ApplyVisual(false);
            }
            else if (State == PlotState.Ready)
            {
                ApplyVisual(false);

                _sparkleTimer -= Time.deltaTime;
                if (_sparkleTimer <= 0f)
                {
                    _sparkleTimer = Random.Range(2.5f, 5f);
                    if (FxManager.Instance != null)
                    {
                        var crop = CropDatabase.Get(CropIndex);
                        FxManager.Instance.Sparkle(transform.position,
                            Color.Lerp(crop.RipeColor, Color.white, 0.4f));
                    }
                }
            }
        }

        /// <summary>New day: crops get a small boost and the soil dries out.</summary>
        public void OnNewDay()
        {
            if (State != PlotState.Growing) return;

            Progress = Mathf.Min(1f, Progress + 0.25f);
            Watered = false;
            if (Progress >= 1f)
            {
                State = PlotState.Ready;
                OnBecameReady();
            }
            ApplyVisual(true);
        }

        public void OnTap()
        {
            switch (State)
            {
                case PlotState.Empty:
                    TryPlant();
                    break;
                case PlotState.Growing:
                    TryWater();
                    break;
                case PlotState.Ready:
                    Harvest();
                    break;
            }
        }

        private void TryPlant()
        {
            var manager = GameManager.Instance;
            if (manager == null) return;

            var crop = CropDatabase.Get(manager.SelectedCropIndex);
            if (!manager.SpendCoins(crop.SeedCost))
            {
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast(Localization.T("not_enough_coins"), true);
                return;
            }

            CropIndex = manager.SelectedCropIndex;
            Progress = 0f;
            Watered = false;
            State = PlotState.Growing;
            BuildCrop();

            if (SfxManager.Instance != null) SfxManager.Instance.PlayPlant();
            if (FxManager.Instance != null) FxManager.Instance.Dust(transform.position + Vector3.up * 0.1f);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("planted") + " " + crop.DisplayName);

            Pulse();
            manager.SaveNow();
            ApplyVisual(true);
        }

        private void TryWater()
        {
            if (Watered)
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast(Localization.T("already_watered"));
                return;
            }

            Watered = true;
            if (SfxManager.Instance != null) SfxManager.Instance.PlayWater();
            if (FxManager.Instance != null) FxManager.Instance.Splash(transform.position);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("watered"));

            Pulse();
            ApplyVisual(true);
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
        }

        private void Harvest()
        {
            var crop = CropDatabase.Get(CropIndex);
            var manager = GameManager.Instance;

            if (manager != null)
            {
                manager.RegisterHarvest(crop.SellPrice);
            }

            if (SfxManager.Instance != null) SfxManager.Instance.PlayHarvest();
            if (FxManager.Instance != null)
            {
                FxManager.Instance.Harvest(transform.position, crop.RipeColor);
                FxManager.Instance.CoinPop(transform.position, crop.SellPrice);
            }
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("harvested") + crop.SellPrice, false, true);

            State = PlotState.Empty;
            Progress = 0f;
            Watered = false;
            if (_crop != null)
            {
                _crop.Destroy();
                _crop = null;
            }

            Pulse();
            if (manager != null) manager.SaveNow();
            ApplyVisual(true);
        }

        private void OnBecameReady()
        {
            if (_readySignalled) return;
            _readySignalled = true;
            if (SfxManager.Instance != null) SfxManager.Instance.PlayReady();
            if (FxManager.Instance != null)
                FxManager.Instance.Sparkle(transform.position + Vector3.up * 0.3f, new Color(1f, 0.95f, 0.6f));
            _sparkleTimer = 1.5f;
        }

        private void BuildCrop()
        {
            if (_crop != null) _crop.Destroy();
            _readySignalled = false;
            _lastAppliedProgress = -1f;
            _crop = CropVisual.Build(CropIndex, _cropRoot);
            ApplyVisual(true);
        }

        private void ApplyVisual(bool force)
        {
            if (_soilRenderer != null)
            {
                var soilMat = Watered ? _soilWetMat : _soilDryMat;
                if (_soilRenderer.sharedMaterial != soilMat)
                {
                    _soilRenderer.sharedMaterial = soilMat;
                    if (_furrowRenderers != null)
                    {
                        for (int i = 0; i < _furrowRenderers.Length; i++)
                        {
                            if (_furrowRenderers[i] != null)
                                _furrowRenderers[i].sharedMaterial = soilMat;
                        }
                    }
                }
            }

            if (_crop == null) return;

            bool visible = State != PlotState.Empty;
            if (_crop.Root != null) _crop.Root.SetActive(visible);
            if (!visible) return;

            float t = State == PlotState.Ready ? 1f : Progress;
            if (!force && Mathf.Abs(t - _lastAppliedProgress) < 0.004f && State != PlotState.Ready)
                return;

            _lastAppliedProgress = t;
            _crop.SetGrowth(t, State == PlotState.Ready);
        }

        private void Pulse()
        {
            if (isActiveAndEnabled)
                StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            Vector3 baseScale = Vector3.one;
            float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.06f;
                transform.localScale = baseScale * s;
                yield return null;
            }
            transform.localScale = baseScale;
        }

        // ---------- save ----------

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
            CropIndex = Mathf.Clamp(data.CropIndex, 0, CropDatabase.Count - 1);
            Progress = Mathf.Clamp01(data.Progress);
            Watered = data.Watered;
            _readySignalled = State == PlotState.Ready;

            if (State != PlotState.Empty)
                BuildCrop();
            else
                ApplyVisual(true);
        }
    }
}
