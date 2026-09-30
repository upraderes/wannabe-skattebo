using UnityEngine;

namespace WannabeSkattebo
{
    public enum PickupKind { Football, Magnet, Helmet, BeastMode, DoublePoints }

    public static class PowerUpInfo
    {
        public static float Duration(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Magnet: return 8f;
                case PickupKind.Helmet: return 20f;      // or until it absorbs one catch
                case PickupKind.BeastMode: return 5f;
                case PickupKind.DoublePoints: return 10f;
                default: return 0f;
            }
        }

        public static string Label(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Magnet: return "MAGNET";
                case PickupKind.Helmet: return "HELMET";
                case PickupKind.BeastMode: return "BEAST MODE";
                case PickupKind.DoublePoints: return "2X POINTS";
                default: return "";
            }
        }
    }

    /// <summary>Footballs (points) and power-ups. Spins, bobs, and can be pulled in by the magnet.</summary>
    public class Pickup : Spawnable
    {
        public const int FootballPoints = 10;

        public PickupKind Kind { get; private set; }

        Transform _visual;
        Collider _collider;
        float _baseY;
        bool _magnetized;

        public static Pickup Create(PickupKind kind)
        {
            var go = new GameObject("Pickup_" + kind);
            var p = go.AddComponent<Pickup>();
            p.Kind = kind;
            p._visual = new GameObject("Visual").transform;
            p._visual.SetParent(go.transform, false);
            ModelFactory.BuildPickupVisual(kind, p._visual);
            float size = kind == PickupKind.Football ? 0.8f : 1.1f;
            p._collider = ModelFactory.AddTrigger(go, Vector3.zero, Vector3.one * size);
            return p;
        }

        public override void OnSpawned()
        {
            _baseY = transform.localPosition.y;
            _magnetized = false;
            _collider.enabled = true;
            _visual.localScale = Vector3.one;
        }

        public void Collect()
        {
            _collider.enabled = false;
            _visual.localScale = Vector3.zero;
        }

        public override void Tick(float dt, PlayerController player)
        {
            if (!_collider.enabled) return;
            _visual.localRotation = Quaternion.Euler(0, Time.time * 180f, 0);

            if (Kind == PickupKind.Football)
            {
                Vector3 toPlayer = player.transform.position + Vector3.up * 0.9f - transform.position;
                if (!_magnetized && toPlayer.magnitude < player.MagnetRadius) _magnetized = true;
                if (_magnetized)
                {
                    // move in world space toward the kid; converted back into our parent's space
                    Vector3 step = toPlayer.normalized * Mathf.Min(toPlayer.magnitude, 30f * dt);
                    transform.position += step;
                    return;
                }
            }
            var p = transform.localPosition;
            p.y = _baseY + Mathf.Sin(Time.time * 4f + p.z) * 0.12f;
            transform.localPosition = p;
        }
    }
}
