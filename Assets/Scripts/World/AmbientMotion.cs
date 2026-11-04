using UnityEngine;

namespace FarmSim
{
    /// <summary>Constant rotation — windmill blades, water wheels.</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 Axis = Vector3.forward;
        public float DegreesPerSecond = 26f;

        private void Update()
        {
            transform.Rotate(Axis, DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>Slow drifting clouds that wrap around the map.</summary>
    public class Drifter : MonoBehaviour
    {
        public Vector3 Velocity = new Vector3(0.55f, 0f, 0f);
        public float WrapDistance = 34f;

        private Vector3 _start;

        private void Start()
        {
            _start = transform.position;
        }

        private void Update()
        {
            transform.position += Velocity * Time.deltaTime;
            if (Vector3.Distance(transform.position, _start) > WrapDistance)
                transform.position = _start;
        }
    }

    /// <summary>
    /// Scrolls a water texture so ponds feel alive. The material is injected by
    /// the builder (a unique copy) so this never calls renderer.material on a
    /// statically batched object, and the scroll stays cheap.
    /// </summary>
    public class WaterFlow : MonoBehaviour
    {
        public Vector2 Speed = new Vector2(0.035f, 0.015f);
        public Material Surface;

        private void Update()
        {
            if (Surface == null) return;
            var offset = Surface.mainTextureOffset;
            offset += Speed * Time.deltaTime;
            Surface.mainTextureOffset = offset;
        }
    }

    /// <summary>Gentle vertical bob for props like the sign or scarecrow.</summary>
    public class Bobber : MonoBehaviour
    {
        public float Amplitude = 0.05f;
        public float Speed = 1.6f;

        private Vector3 _start;

        private void Start()
        {
            _start = transform.localPosition;
        }

        private void Update()
        {
            transform.localPosition = _start + Vector3.up * Mathf.Sin(Time.time * Speed) * Amplitude;
        }
    }
}
