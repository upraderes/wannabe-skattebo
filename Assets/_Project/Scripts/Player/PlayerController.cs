using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>
    /// The kid. Stays at z = 0 while the world scrolls toward it.
    /// Swipe left/right to change lane, up to jump, down to slide.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        const float JumpVelocity = 9f;
        const float Gravity = -28f;
        const float SlideTime = 0.7f;
        const float StumbleTime = 0.4f;

        public int Lane { get; private set; }            // -1, 0, 1
        public float TargetX => Lane * TrackSpawner.LaneWidth;
        public bool IsGrounded => _y <= 0f && _vy <= 0f;
        public bool IsSliding => _slideTimer > 0f;
        public bool IsStumbling => _stumbleTimer > 0f;
        public float MagnetRadius { get; set; }
        public float LaneChangeTime { get; set; } = 0.16f;

        public System.Action<Collider> OnHit;

        ModelFactory.KidRig _rig;
        BoxCollider _collider;
        readonly SwipeInput _input = new SwipeInput();
        float _x, _y, _vy;
        float _slideTimer, _stumbleTimer;
        float _runPhase;
        bool _caught;
        float _caughtTime;

        public void Build(ModelFactory.KidLook look)
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.isTrigger = true;
            _rig = ModelFactory.BuildKid(transform, look);
            ResetForRun();
        }

        /// <summary>Rebuilds the visual when the career stage or gear changes.</summary>
        public void SetLook(ModelFactory.KidLook look)
        {
            bool helmet = _rig.Helmet.gameObject.activeSelf;
            _rig.Root.gameObject.SetActive(false);
            Destroy(_rig.Root.gameObject);
            _rig = ModelFactory.BuildKid(transform, look);
            _rig.Helmet.gameObject.SetActive(helmet);
        }

        public void ResetForRun()
        {
            Lane = 0;
            _x = _y = _vy = 0f;
            _slideTimer = _stumbleTimer = 0f;
            _caught = false;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            _rig.Root.localRotation = Quaternion.identity;
            _rig.Root.localPosition = Vector3.zero;
            SetCollider(false);
        }

        public void SetHelmet(bool on) => _rig.Helmet.gameObject.SetActive(on);

        public void Stumble()
        {
            _stumbleTimer = StumbleTime;
        }

        public void PlayCaught()
        {
            _caught = true;
            _caughtTime = 0f;
        }

        /// <summary>Idle animation for the menu / results backdrop.</summary>
        public void TickIdle(float dt)
        {
            _runPhase += dt * 3f;
            _rig.ArmL.localEulerAngles = new Vector3(Mathf.Sin(_runPhase) * 8f, 0, 0);
            _rig.Root.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(_runPhase)) * 0.03f, 0);
        }

        public void TickRun(float dt, float speed)
        {
            if (_caught)
            {
                // tackled: topple forward
                _caughtTime += dt;
                float t = Mathf.Clamp01(_caughtTime / 0.35f);
                _rig.Root.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 80f, t), 0, 0);
                _rig.Root.localPosition = new Vector3(0, Mathf.Lerp(0f, 0.3f, t), 0);
                return;
            }

            HandleInput();

            // lateral
            float laneSpeed = TrackSpawner.LaneWidth / Mathf.Max(0.05f, LaneChangeTime);
            _x = Mathf.MoveTowards(_x, TargetX, laneSpeed * dt);

            // vertical
            if (_y > 0f || _vy > 0f)
            {
                _vy += Gravity * (_slideTimer > 0f ? 2.5f : 1f) * dt;
                _y += _vy * dt;
                if (_y <= 0f) { _y = 0f; _vy = 0f; }
            }

            if (_slideTimer > 0f)
            {
                _slideTimer -= dt;
                if (_slideTimer <= 0f) SetCollider(false);
            }
            if (_stumbleTimer > 0f) _stumbleTimer -= dt;

            transform.position = new Vector3(_x, _y, 0f);
            Animate(dt, speed);
        }

        void HandleInput()
        {
            var dir = _input.Poll();
            if (dir == SwipeDir.None) return;

            switch (dir)
            {
                case SwipeDir.Left:
                    if (!IsStumbling && Lane > -1) Lane--;
                    break;
                case SwipeDir.Right:
                    if (!IsStumbling && Lane < 1) Lane++;
                    break;
                case SwipeDir.Up:
                    if (IsGrounded)
                    {
                        _vy = JumpVelocity;
                        _y = 0.001f;
                        _slideTimer = 0f;
                        SetCollider(false);
                    }
                    break;
                case SwipeDir.Down:
                    _slideTimer = SlideTime;
                    SetCollider(true);
                    break;
            }
        }

        void SetCollider(bool sliding)
        {
            if (sliding)
            {
                _collider.center = new Vector3(0, 0.4f, 0);
                _collider.size = new Vector3(0.7f, 0.8f, 0.9f);
            }
            else
            {
                _collider.center = new Vector3(0, 0.85f, 0);
                _collider.size = new Vector3(0.7f, 1.7f, 0.6f);
            }
        }

        void Animate(float dt, float speed)
        {
            _runPhase += dt * (6f + speed * 0.5f);
            float swing = Mathf.Sin(_runPhase) * 50f;
            bool airborne = _y > 0.01f;

            if (IsSliding)
            {
                _rig.Root.localRotation = Quaternion.Euler(-55f, 0, 0);
                _rig.Root.localPosition = new Vector3(0, 0.25f, 0);
                _rig.LegL.localEulerAngles = new Vector3(-70f, 0, 0);
                _rig.LegR.localEulerAngles = new Vector3(-70f, 0, 0);
                return;
            }

            float lean = (TargetX - _x) * 6f;
            float tilt = IsStumbling ? Mathf.Sin(_stumbleTimer * 40f) * 12f : 12f;
            _rig.Root.localRotation = Quaternion.Euler(tilt, 0, -lean);
            _rig.Root.localPosition = new Vector3(0, airborne ? 0f : Mathf.Abs(Mathf.Sin(_runPhase)) * 0.08f, 0);

            if (airborne)
            {
                _rig.LegL.localEulerAngles = new Vector3(-40f, 0, 0);
                _rig.LegR.localEulerAngles = new Vector3(20f, 0, 0);
                _rig.ArmL.localEulerAngles = new Vector3(-150f, 0, 0);
            }
            else
            {
                _rig.LegL.localEulerAngles = new Vector3(swing, 0, 0);
                _rig.LegR.localEulerAngles = new Vector3(-swing, 0, 0);
                _rig.ArmL.localEulerAngles = new Vector3(-swing * 0.8f, 0, 0);
            }
            // ball arm stays tucked (high and tight!)
            _rig.ArmR.localEulerAngles = new Vector3(-20f, 0, 15f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!_caught) OnHit?.Invoke(other);
        }
    }
}
