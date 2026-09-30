using UnityEngine;

namespace WannabeSkattebo
{
    public enum ObstacleKind { TrashCan, Cone, Gnome, Wheelbarrow, LawnMower, TelephonePost, Flamingo, Chicken, Dog }

    public struct ObstacleDef
    {
        public ObstacleKind Kind;
        public int Toughness;   // player SmashPower below this -> stumble
        public int Points;
        public bool IsAnimal;   // animals scatter away unharmed instead of breaking
        public Color DebrisColor;
        public Vector3 HitSize;

        public static ObstacleDef For(ObstacleKind kind)
        {
            var p = ModelFactory.Palette.Metal;
            switch (kind)
            {
                case ObstacleKind.TrashCan:      return new ObstacleDef { Kind = kind, Toughness = 1, Points = 50,  DebrisColor = ModelFactory.Palette.Metal, HitSize = new Vector3(0.9f, 1.2f, 0.9f) };
                case ObstacleKind.Cone:          return new ObstacleDef { Kind = kind, Toughness = 1, Points = 30,  DebrisColor = ModelFactory.Palette.Orange, HitSize = new Vector3(0.7f, 0.8f, 0.7f) };
                case ObstacleKind.Gnome:         return new ObstacleDef { Kind = kind, Toughness = 1, Points = 40,  DebrisColor = ModelFactory.Palette.Red, HitSize = new Vector3(0.6f, 1.1f, 0.6f) };
                case ObstacleKind.Flamingo:      return new ObstacleDef { Kind = kind, Toughness = 1, Points = 40,  DebrisColor = ModelFactory.Palette.Pink, HitSize = new Vector3(0.6f, 1.6f, 0.8f) };
                case ObstacleKind.Wheelbarrow:   return new ObstacleDef { Kind = kind, Toughness = 2, Points = 80,  DebrisColor = ModelFactory.Palette.Red, HitSize = new Vector3(1f, 1f, 1.8f) };
                case ObstacleKind.LawnMower:     return new ObstacleDef { Kind = kind, Toughness = 3, Points = 150, DebrisColor = ModelFactory.Palette.Yellow, HitSize = new Vector3(1f, 1f, 1.2f) };
                case ObstacleKind.TelephonePost: return new ObstacleDef { Kind = kind, Toughness = 4, Points = 250, DebrisColor = ModelFactory.Palette.Wood, HitSize = new Vector3(0.8f, 5f, 0.8f) };
                case ObstacleKind.Chicken:       return new ObstacleDef { Kind = kind, Toughness = 1, Points = 60,  IsAnimal = true, DebrisColor = ModelFactory.Palette.Fence, HitSize = new Vector3(0.6f, 0.9f, 0.7f) };
                case ObstacleKind.Dog:           return new ObstacleDef { Kind = kind, Toughness = 2, Points = 90,  IsAnimal = true, DebrisColor = ModelFactory.Palette.Wood, HitSize = new Vector3(0.6f, 1f, 1.1f) };
                default:                         return new ObstacleDef { Kind = kind, Toughness = 1, Points = 10, DebrisColor = p, HitSize = Vector3.one };
            }
        }
    }

    /// <summary>
    /// Every obstacle is smashable: running into it destroys it and scores points.
    /// Tougher objects than the player's Power make the kid stumble (lane lock + combo break).
    /// </summary>
    public class Smashable : Spawnable
    {
        public ObstacleDef Def { get; private set; }

        Transform _visual;
        Collider _collider;
        bool _smashed;
        float _fleeTimer;
        Vector3 _fleeVelocity;

        public static Smashable Create(ObstacleKind kind)
        {
            var def = ObstacleDef.For(kind);
            var go = new GameObject("Obstacle_" + kind);
            var s = go.AddComponent<Smashable>();
            s.Def = def;
            s._visual = new GameObject("Visual").transform;
            s._visual.SetParent(go.transform, false);
            ModelFactory.BuildObstacleVisual(kind, s._visual);
            s._collider = ModelFactory.AddTrigger(go, new Vector3(0, def.HitSize.y * 0.5f, 0), def.HitSize);
            return s;
        }

        public override void OnSpawned()
        {
            _smashed = false;
            _fleeTimer = 0f;
            _collider.enabled = true;
            _visual.gameObject.SetActive(true);
            _visual.localPosition = Vector3.zero;
            _visual.localRotation = Quaternion.Euler(0, Random.Range(-20f, 20f), 0);
        }

        /// <summary>Returns true if the hit made the player stumble.</summary>
        public bool Smash(int smashPower, bool unstoppable)
        {
            if (_smashed) return false;
            _smashed = true;
            _collider.enabled = false;

            if (Def.IsAnimal)
            {
                // animals are never hurt: they leap out of the way and run off
                float dir = transform.position.x >= 0f ? 1f : -1f;
                _fleeVelocity = new Vector3(dir * 7f, 6f, 4f);
                _fleeTimer = 1.2f;
            }
            else
            {
                _visual.gameObject.SetActive(false);
                Debris.Burst(transform.parent, transform.localPosition + Vector3.up * Def.HitSize.y * 0.5f,
                    Def.DebrisColor, Mathf.Clamp(Def.Toughness * 4, 5, 14));
            }
            return !unstoppable && smashPower < Def.Toughness;
        }

        public override void Tick(float dt, PlayerController player)
        {
            if (_fleeTimer <= 0f) return;
            _fleeTimer -= dt;
            _fleeVelocity.y -= 20f * dt;
            _visual.localPosition += _fleeVelocity * dt;
            if (_visual.localPosition.y < 0f)
            {
                _visual.localPosition = new Vector3(_visual.localPosition.x, 0f, _visual.localPosition.z);
                _fleeVelocity.y = 0f;
            }
            _visual.localRotation = Quaternion.LookRotation(new Vector3(_fleeVelocity.x, 0f, _fleeVelocity.z));
            if (_fleeTimer <= 0f) _visual.gameObject.SetActive(false);
        }
    }

    /// <summary>Cheap chunky debris that flies out when something is smashed. Lives in world-root space.</summary>
    public class Debris : MonoBehaviour
    {
        Vector3 _velocity;
        Vector3 _spin;
        float _life;

        public static void Burst(Transform parent, Vector3 localPos, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = ModelFactory.Part(PrimitiveType.Cube, parent, localPos + Random.insideUnitSphere * 0.3f,
                    Vector3.one * Random.Range(0.12f, 0.3f), i % 3 == 0 ? Color.white : color);
                var d = go.AddComponent<Debris>();
                d._velocity = new Vector3(Random.Range(-4f, 4f), Random.Range(4f, 9f), Random.Range(2f, 10f));
                d._spin = Random.insideUnitSphere * 720f;
                d._life = Random.Range(0.8f, 1.3f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _velocity.y -= 22f * dt;
            transform.localPosition += _velocity * dt;
            transform.localRotation *= Quaternion.Euler(_spin * dt);
            if (transform.localPosition.y < 0.1f && _velocity.y < 0f)
            {
                _velocity = new Vector3(_velocity.x * 0.5f, -_velocity.y * 0.3f, _velocity.z * 0.5f);
            }
            _life -= dt;
            if (_life < 0.3f) transform.localScale *= 1f - dt * 6f;
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
