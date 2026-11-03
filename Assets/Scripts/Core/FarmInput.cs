using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmSim
{
    /// <summary>Turns taps (touch or mouse) into plot interactions via raycast.</summary>
    public class FarmInput : MonoBehaviour
    {
        private Camera _cam;
        private Vector3 _downPos;
        private bool _downOnUi;

        public void Init(Camera cam)
        {
            _cam = cam;
        }

        private void Update()
        {
            if (_cam == null) return;

            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    _downPos = t.position;
                    _downOnUi = IsOverUi(t.fingerId);
                }
                else if (t.phase == TouchPhase.Ended)
                {
                    TryTap(t.position);
                }
            }
            else if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _downPos = Input.mousePosition;
                    _downOnUi = IsOverUi(-1);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    TryTap(Input.mousePosition);
                }
            }
        }

        private bool IsOverUi(int fingerId)
        {
            if (EventSystem.current == null) return false;
            return fingerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private void TryTap(Vector3 screenPos)
        {
            if (_downOnUi) return;
            // A tap barely moves; a drag is camera panning.
            if (Vector3.Distance(_downPos, screenPos) > 18f) return;

            Ray ray = _cam.ScreenPointToRay(screenPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                var plot = hit.collider.GetComponentInParent<FarmPlot>();
                if (plot != null)
                    plot.OnTap();
            }
        }
    }
}
