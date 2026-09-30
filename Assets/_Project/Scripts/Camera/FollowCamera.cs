using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>Chase camera behind and above the kid. Adapts FOV so all 3 lanes fit on a portrait phone.</summary>
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        const float DesiredHorizontalFov = 52f;

        public Transform Target;

        Camera _cam;
        Vector3 _vel;
        float _shake;

        void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        public void Shake(float amount)
        {
            _shake = Mathf.Max(_shake, amount);
        }

        public void SnapToTarget()
        {
            if (Target == null) return;
            transform.position = DesiredPosition();
            transform.LookAt(LookPoint());
        }

        Vector3 DesiredPosition()
        {
            var t = Target.position;
            return new Vector3(t.x * 0.55f, 5.6f + t.y * 0.35f, -7.5f);
        }

        Vector3 LookPoint()
        {
            var t = Target.position;
            return new Vector3(t.x * 0.7f, 0.8f + t.y * 0.25f, 9f);
        }

        void LateUpdate()
        {
            if (Target == null) return;

            float hRad = DesiredHorizontalFov * Mathf.Deg2Rad;
            float vFov = 2f * Mathf.Atan(Mathf.Tan(hRad * 0.5f) / Mathf.Max(0.1f, _cam.aspect)) * Mathf.Rad2Deg;
            _cam.fieldOfView = Mathf.Clamp(vFov, 50f, 80f);

            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref _vel, 0.08f);
            transform.LookAt(LookPoint());

            if (_shake > 0f)
            {
                transform.position += Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 1.5f);
            }
        }
    }
}
