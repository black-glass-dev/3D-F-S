using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Fixed-angle isometric camera for mobile: drag to pan, pinch to zoom,
    /// double tap to reset. The angle never rotates — that is what makes the
    /// game 2.5D and keeps touch panning predictable.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        private static readonly Vector3 ViewOffset = new Vector3(18f, 18f, -18f);

        private Camera _cam;

        private Vector3 _focus;
        private Vector3 _targetFocus;
        private float _size = 7f;
        private float _targetSize = 7f;

        private Vector3 _defaultFocus = Vector3.zero;
        private float _defaultSize = 7f;

        private float _minSize = 4f;
        private float _maxSize = 11f;
        private Vector2 _xRange = new Vector2(-8f, 8f);
        private Vector2 _zRange = new Vector2(-7f, 7f);

        private bool _dragging;
        private Vector3 _lastMousePos;
        private float _lastPinchDistance;
        private float _lastTapTime = -1f;
        private Vector3 _lastTapPosition;

        public void Init(Vector3 focus, float size, Vector2 xRange, Vector2 zRange)
        {
            _cam = GetComponent<Camera>();
            _defaultFocus = focus;
            _defaultSize = size;
            _xRange = Normalize(xRange);
            _zRange = Normalize(zRange);

            _focus = focus;
            _targetFocus = focus;
            _size = size;
            _targetSize = size;
            _minSize = Mathf.Max(3f, size * 0.55f);
            _maxSize = size * 1.7f;

            ApplyTransform();
        }

        private static Vector2 Normalize(Vector2 range)
        {
            if (range.x > range.y)
                return new Vector2(range.y, range.x);
            return range;
        }

        private void Update()
        {
            HandleTouch();
            HandleMouse();

            _focus = Vector3.Lerp(_focus, _targetFocus, Time.deltaTime * 9f);
            _size = Mathf.Lerp(_size, _targetSize, Time.deltaTime * 9f);
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            transform.position = _focus + ViewOffset;
            transform.LookAt(_focus);
            if (_cam != null)
            {
                _cam.orthographic = true;
                _cam.orthographicSize = _size;
            }
        }

        private void PanByScreenDelta(Vector2 deltaPixels)
        {
            if (_cam == null) return;
            float worldPerPixel = (_size * 2f) / Mathf.Max(1f, Screen.height);

            var right = transform.right;
            right.y = 0f;
            right.Normalize();

            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 move = -(right * deltaPixels.x + forward * deltaPixels.y) * worldPerPixel;
            _targetFocus += move;
            _targetFocus.x = Mathf.Clamp(_targetFocus.x, _xRange.x, _xRange.y);
            _targetFocus.z = Mathf.Clamp(_targetFocus.z, _zRange.x, _zRange.y);
            _targetFocus.y = 0f;
        }

        private void HandleTouch()
        {
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    RegisterTapCandidate(touch.position);
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    PanByScreenDelta(touch.deltaPosition);
                }
                _lastPinchDistance = 0f;
            }
            else if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                float distance = Vector2.Distance(a.position, b.position);
                if (_lastPinchDistance > 0f)
                {
                    _targetSize = Mathf.Clamp(_targetSize + (_lastPinchDistance - distance) * 0.012f,
                        _minSize, _maxSize);
                }
                _lastPinchDistance = distance;
            }
            else
            {
                _lastPinchDistance = 0f;
            }
        }

        private void HandleMouse()
        {
            if (Input.touchCount > 0) return;

            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wheel) > 0.001f)
                _targetSize = Mathf.Clamp(_targetSize - wheel * 2.4f, _minSize, _maxSize);

            if (Input.GetMouseButtonDown(0))
            {
                _dragging = true;
                _lastMousePos = Input.mousePosition;
                RegisterTapCandidate(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0))
                _dragging = false;

            if (_dragging && Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                PanByScreenDelta(delta);
                _lastMousePos = Input.mousePosition;
            }
        }

        private void RegisterTapCandidate(Vector3 screenPosition)
        {
            bool isDoubleTap = Time.unscaledTime - _lastTapTime < 0.32f &&
                               Vector3.Distance(screenPosition, _lastTapPosition) < 60f;

            if (isDoubleTap)
                ResetView();

            _lastTapTime = Time.unscaledTime;
            _lastTapPosition = screenPosition;
        }

        public void ResetView()
        {
            _targetFocus = _defaultFocus;
            _targetSize = _defaultSize;
        }
    }
}
