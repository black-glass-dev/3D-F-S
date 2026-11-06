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
    /// Growing -> water (1.8x faster), Ready -> harvest into the barn.
    /// Locked plots are overgrown land that can be bought to expand the farm.
    /// </summary>
    public class FarmPlot : MonoBehaviour
    {
        public PlotState State { get; private set; }
        public int CropIndex { get; private set; }
        public float Progress { get; private set; }
        public bool Watered { get; private set; }
        public bool Locked { get; private set; }
        public int Index { get; private set; }

        private CropVisual _crop;
        private Transform _cropRoot;
        private Transform _lockedRoot;
        private Renderer _soilRenderer;
        private Renderer[] _furrowRenderers;
        private Material _soilDryMat;
        private Material _soilWetMat;

        private float _sparkleTimer;
        private float _lastAppliedProgress = -1f;
        private bool _readySignalled;

        public void Init(bool locked, int index)
        {
            Index = index;
            Locked = locked;

            _soilDryMat = MaterialCache.Get(new Color(0.46f, 0.34f, 0.23f), SurfacePattern.Soil);
            _soilWetMat = MaterialCache.Get(new Color(0.27f, 0.19f, 0.14f), SurfacePattern.SoilWet);

            State = PlotState.Empty;
            CropIndex = 0;
            Progress = 0f;
            Watered = false;

            if (Locked)
            {
                BuildLockedVisual();
                return;
            }

            BuildSoilVisual();

            _cropRoot = new GameObject("CropRoot").transform;
            _cropRoot.SetParent(transform, false);
            _cropRoot.localPosition = new Vector3(0f, 0.12f, 0f);
        }

        // ---------- visuals ----------

        private void BuildSoilVisual()
        {
            // One renderer per plot: the slab, the furrows and a couple of pebble
            // bumps live in a single shared mesh (MeshFactory.PlotSoil).
            var soil = new GameObject("Soil");
            soil.transform.SetParent(transform, false);
            soil.AddComponent<MeshFilter>().sharedMesh = MeshFactory.PlotSoil;

            _soilRenderer = soil.AddComponent<MeshRenderer>();
            _soilRenderer.sharedMaterial = _soilDryMat;
            _soilRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _soilRenderer.receiveShadows = true;
            _furrowRenderers = null;
        }

        private void BuildLockedVisual()
        {
            _lockedRoot = new GameObject("Locked").transform;
            _lockedRoot.SetParent(transform, false);

            // overgrown grass tile
            var grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grass.name = "WildGrass";
            grass.transform.SetParent(_lockedRoot, false);
            grass.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            grass.transform.localScale = new Vector3(1.02f, 0.1f, 1.02f);
            grass.GetComponent<Renderer>().sharedMaterial =
                MaterialCache.Get(new Color(0.36f, 0.46f, 0.28f), SurfacePattern.GrassDark);
            Destroy(grass.GetComponent<Collider>());

            // "for sale" sign
            var wood = MaterialCache.Get(new Color(0.42f, 0.32f, 0.22f), SurfacePattern.Wood);
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "SignPost";
            post.transform.SetParent(_lockedRoot, false);
            post.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            post.transform.localScale = new Vector3(0.07f, 0.44f, 0.07f);
            post.GetComponent<Renderer>().sharedMaterial = wood;
            Destroy(post.GetComponent<Collider>());

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "SignBoard";
            board.transform.SetParent(_lockedRoot, false);
            board.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            board.transform.localScale = new Vector3(0.42f, 0.26f, 0.05f);
            board.GetComponent<Renderer>().sharedMaterial =
                MaterialCache.Get(new Color(0.76f, 0.70f, 0.56f), SurfacePattern.Wood);
            Destroy(board.GetComponent<Collider>());
        }

        public void Unlock()
        {
            if (!Locked) return;

            if (_lockedRoot != null)
            {
                Destroy(_lockedRoot.gameObject);
                _lockedRoot = null;
            }

            Locked = false;
            State = PlotState.Empty;
            Progress = 0f;
            Watered = false;

            BuildSoilVisual();

            _cropRoot = new GameObject("CropRoot").transform;
            _cropRoot.SetParent(transform, false);
            _cropRoot.localPosition = new Vector3(0f, 0.12f, 0f);

            if (FxManager.Instance != null) FxManager.Instance.Dust(transform.position + Vector3.up * 0.1f);
            Pulse();
        }

        // ---------- loop ----------

        private void Update()
        {
            if (Locked) return;

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
            if (Locked || State != PlotState.Growing) return;

            Progress = Mathf.Min(1f, Progress + 0.25f);
            Watered = false;
            if (Progress >= 1f)
            {
                State = PlotState.Ready;
                OnBecameReady();
            }
            ApplyVisual(true);
        }

        /// <summary>
        /// v1.3.0: catch a growing crop up on the wall-clock time the app spent
        /// closed. Ready plots are announced silently (no sound storm when a
        /// dozen fields ripen at once on load). Returns true if it ripened.
        /// </summary>
        public bool AddOfflineGrowth(float seconds)
        {
            if (Locked || State != PlotState.Growing || seconds <= 0f) return false;

            var crop = CropDatabase.Get(CropIndex);
            if (crop == null) return false;

            float speed = Watered ? 1.8f : 1f;
            Progress = Mathf.Min(1f, Progress + (seconds * speed) / Mathf.Max(1f, crop.GrowSeconds));

            bool ripened = Progress >= 1f;
            if (ripened)
            {
                Progress = 1f;
                State = PlotState.Ready;
                _readySignalled = true;
                _sparkleTimer = 2f;
            }
            ApplyVisual(true);
            return ripened;
        }

        public void OnTap()
        {
            var manager = GameManager.Instance;
            if (Locked)
            {
                if (manager != null) manager.BuyPlot(Index);
                return;
            }

            int tool = manager != null ? manager.ToolIndex : 0;
            switch (tool)
            {
                case 1: // planting only
                    if (State == PlotState.Empty) TryPlant();
                    else WrongTool("tool_need_empty");
                    return;
                case 2: // watering only
                    if (State == PlotState.Growing) TryWater();
                    else WrongTool("tool_need_growing");
                    return;
                case 3: // harvesting only
                    if (State == PlotState.Ready) Harvest();
                    else WrongTool("tool_need_ready");
                    return;
            }

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

        private static void WrongTool(string messageKey)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T(messageKey), true);
        }

        /// <summary>Sprinkler upgrade — water without a tap.</summary>
        public void AutoWater()
        {
            if (Locked || State != PlotState.Growing || Watered) return;
            Watered = true;
            ApplyVisual(true);
        }

        private void TryPlant()
        {
            var manager = GameManager.Instance;
            if (manager == null) return;

            int cropIndex = manager.SelectedCropIndex;
            if (!manager.IsCropUnlocked(cropIndex))
            {
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                return;
            }

            var crop = CropDatabase.Get(cropIndex);
            if (!manager.SpendCoins(crop.SeedCost))
            {
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast(Localization.T("not_enough_coins"), true);
                return;
            }

            CropIndex = cropIndex;
            Progress = 0f;
            // A repaired well waters the seed as it goes in.
            Watered = manager.HasWell;
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

            // The barn is finite: if it is full the crop stays in the field.
            if (manager != null && !manager.RegisterHarvest(CropIndex))
                return;

            int price = manager != null ? manager.MarketPrice[CropIndex] : crop.SellPrice;

            if (SfxManager.Instance != null) SfxManager.Instance.PlayHarvest();
            if (FxManager.Instance != null)
            {
                FxManager.Instance.Harvest(transform.position, crop.RipeColor);
                FxManager.Instance.CoinPop(transform.position, price);
            }
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(crop.DisplayName + " → " + Localization.T("barn"), false, true);

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
                Watered = Watered,
                Locked = Locked
            };
        }

        public void LoadSaveData(PlotSaveData data)
        {
            if (data == null) return;

            if (!data.Locked && Locked)
            {
                Unlock();
            }

            if (Locked) return;

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
