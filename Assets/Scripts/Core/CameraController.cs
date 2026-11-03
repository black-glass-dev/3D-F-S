using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Fixed-angle isometric camera for mobile: drag to pan, pinch / wheel to zoom.
    /// The angle never rotates — that is what makes this 2.5D and touch-friendly.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public Vector3 Focus = Vector3.zero;
        public float OrthoSize = 6.5f;

        private const float MinSize = 3.5f;
        private const float MaxSize = 12f;
        private const float PanLimit = 9f;

        private Camera _cam;
        private Vector3 _lastMousePos;
        private bool _dragging;
        private float _lastPinchDistance;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            ApplyTransform();
        }

        private void Update()
        {
            HandleTouch();
            HandleMouse();
            OrthoSize = Mathf.Clamp(OrthoSize, MinSize, MaxSize);
            Focus.x = Mathf.Clamp(Focus.x, -PanLimit, PanLimit);
            Focus.z = Mathf.Clamp(Focus.z, -PanLimit, PanLimit);
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            // Classic isometric direction: equal parts x/y, looking back along -z
            var offset = new Vector3(18f, 18f, -18f);
            transform.position = Focus + offset;
            transform.LookAt(Focus);
            if (_cam != null)
            {
                _cam.orthographic = true;
                _cam.orthographicSize = OrthoSize;
            }
        }

        private void PanByScreenDelta(Vector2 deltaPixels)
        {
            if (_cam == null) return;
            float worldPerPixel = (OrthoSize * 2f) / Mathf.Max(1f, Screen.height);
            var right = transform.right;
            right.y = 0f;
            right.Normalize();
            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Focus -= (right * deltaPixels.x + forward * deltaPixels.y) * worldPerPixel;
        }

        private void HandleTouch()
        {
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved)
                    PanByScreenDelta(t.deltaPosition);
                _lastPinchDistance = 0f;
            }
            else if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                float dist = Vector2.Distance(a.position, b.position);
                if (_lastPinchDistance > 0f)
                    OrthoSize += (_lastPinchDistance - dist) * 0.01f;
                _lastPinchDistance = dist;
            }
        }

        private void HandleMouse()
        {
            if (Input.touchCount > 0) return;

            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wheel) > 0.001f)
                OrthoSize -= wheel * 2f;

            if (Input.GetMouseButtonDown(0))
            {
                _dragging = true;
                _lastMousePos = Input.mousePosition;
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

        /// <summary>True while the player is panning (used to tell a tap from a drag).</summary>
        public bool IsDraggingFar(Vector3 downPos, Vector3 upPos)
        {
            return Vector3.Distance(downPos, upPos) > 18f;
        }
    }
}
