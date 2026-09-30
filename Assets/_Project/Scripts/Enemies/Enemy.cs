using UnityEngine;

namespace WannabeSkattebo
{
    public enum EnemyBehavior { Walker, LaneSwitcher, Charger }

    /// <summary>
    /// People who want to stop the kid. They cannot be smashed: touching one ends the run
    /// (unless a Helmet or Beast Mode is active).
    /// </summary>
    public class Enemy : Spawnable
    {
        public EnemyKind Kind { get; private set; }
        public EnemyBehavior Behavior { get; private set; }

        Transform _body;
        Transform[] _limbs;
        Collider _collider;
        float _walkSpeed;
        bool _switched;
        float _targetX;
        float _animTime;
        bool _knockedAway;
        Vector3 _knockVelocity;

        public static Enemy Create(EnemyKind kind)
        {
            var go = new GameObject("Enemy_" + kind);
            var e = go.AddComponent<Enemy>();
            e.Kind = kind;
            e.Behavior = BehaviorFor(kind);
            e._limbs = ModelFactory.BuildPerson(kind, go.transform);
            e._body = go.transform.Find("Body");
            e._collider = ModelFactory.AddTrigger(go, new Vector3(0, 1.1f, 0), new Vector3(0.9f, 2.2f, 0.6f));
            return e;
        }

        public static EnemyBehavior BehaviorFor(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Teacher:
                case EnemyKind.IceAgent:
                    return EnemyBehavior.LaneSwitcher;
                case EnemyKind.OpposingPlayer:
                case EnemyKind.Police:
                case EnemyKind.FedAgent:
                    return EnemyBehavior.Charger;
                default:
                    return EnemyBehavior.Walker;
            }
        }

        public static string DisplayName(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Neighbor: return "the grumpy neighbor";
                case EnemyKind.Teacher: return "the teacher";
                case EnemyKind.OpposingPlayer: return "a linebacker";
                case EnemyKind.Police: return "the police";
                case EnemyKind.IceAgent: return "an ICE agent";
                default: return "the feds";
            }
        }

        public override void OnSpawned()
        {
            _switched = false;
            _knockedAway = false;
            _collider.enabled = true;
            _targetX = transform.localPosition.x;
            _animTime = Random.value * 10f;
            _walkSpeed = Behavior == EnemyBehavior.Walker ? 1.5f : 2.5f;
            _body.localPosition = Vector3.zero;
            _body.localEulerAngles = new Vector3(0, 180, 0);
        }

        public override void Tick(float dt, PlayerController player)
        {
            var pos = transform.localPosition;

            if (_knockedAway)
            {
                _knockVelocity.y -= 25f * dt;
                _body.localPosition += _knockVelocity * dt;
                _body.localRotation *= Quaternion.Euler(0, 0, 540f * dt * Mathf.Sign(_knockVelocity.x));
                return;
            }

            // distance ahead of the player in world space
            float ahead = transform.position.z - player.transform.position.z;

            switch (Behavior)
            {
                case EnemyBehavior.LaneSwitcher:
                    if (!_switched && ahead < 24f && ahead > 6f)
                    {
                        _switched = true;
                        // step one lane toward the player's current lane
                        float delta = Mathf.Clamp(player.TargetX - pos.x, -TrackSpawner.LaneWidth, TrackSpawner.LaneWidth);
                        _targetX = pos.x + delta;
                    }
                    pos.x = Mathf.MoveTowards(pos.x, _targetX, 4.5f * dt);
                    break;
                case EnemyBehavior.Charger:
                    if (ahead < 30f) _walkSpeed = 7f;
                    break;
            }

            pos.z -= _walkSpeed * dt;
            transform.localPosition = pos;

            // simple walk cycle
            _animTime += dt * (_walkSpeed * 2.5f + 2f);
            float swing = Mathf.Sin(_animTime) * 35f;
            _limbs[0].localEulerAngles = new Vector3(swing, 0, 0);
            _limbs[1].localEulerAngles = new Vector3(-swing, 0, 0);
            float reach = Mathf.Sin(_animTime * 0.5f) * 10f;
            _limbs[2].localEulerAngles = new Vector3(reach, -10f, 0);
            _limbs[3].localEulerAngles = new Vector3(-reach, 10f, 0);
        }

        /// <summary>Juked or bounced off by a power-up: fly out of the lane.</summary>
        public void KnockAway(float playerX)
        {
            _knockedAway = true;
            _collider.enabled = false;
            float dir = transform.position.x >= playerX ? 1f : -1f;
            _knockVelocity = new Vector3(dir * 8f, 7f, 6f);
        }
    }
}
