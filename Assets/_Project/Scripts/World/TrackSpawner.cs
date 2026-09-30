using System.Collections.Generic;
using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>Ground tile; purely visual.</summary>
    public class GroundChunk : Spawnable
    {
        public static GroundChunk Create(int variant, Color ground, float length)
        {
            var go = new GameObject("Ground_" + variant);
            ModelFactory.BuildGroundChunk(go.transform, length, variant, ground);
            return go.AddComponent<GroundChunk>();
        }
    }

    /// <summary>
    /// Endless 3-lane track. The player stays at the origin; the world root scrolls toward -Z.
    /// Every RebaseEvery metres everything is shifted back so floats stay small (floating origin).
    /// </summary>
    public class TrackSpawner : MonoBehaviour
    {
        public const float LaneWidth = 2.5f;
        const float ChunkLength = 30f;
        const float SpawnAhead = 115f;
        const float DespawnBehind = -12f;
        const float RebaseEvery = 500f;
        const float RowSpacing = 10f;
        const float SafeStart = 40f;
        const int GroundVariants = 4;

        struct Weighted<T> { public T Item; public float MinDistance; public float Weight; }

        static readonly Weighted<ObstacleKind>[] ObstacleTable =
        {
            new Weighted<ObstacleKind> { Item = ObstacleKind.TrashCan,      MinDistance = 0,    Weight = 5 },
            new Weighted<ObstacleKind> { Item = ObstacleKind.Cone,          MinDistance = 0,    Weight = 4 },
            new Weighted<ObstacleKind> { Item = ObstacleKind.Gnome,         MinDistance = 0,    Weight = 3 },
            new Weighted<ObstacleKind> { Item = ObstacleKind.Flamingo,      MinDistance = 0,    Weight = 2 },
            new Weighted<ObstacleKind> { Item = ObstacleKind.Chicken,       MinDistance = 100,  Weight = 2 },
            new Weighted<ObstacleKind> { Item = ObstacleKind.Wheelbarrow,   MinDistance = 200,  Weight = 3 },
            new Weighted<ObstacleKind> { Item = ObstacleKind.Dog,           MinDistance = 300,  Weight = 2 },
            new Weighted<ObstacleKind> { Item = ObstacleKind.LawnMower,     MinDistance = 500,  Weight = 2 },
            new Weighted<ObstacleKind> { Item = ObstacleKind.TelephonePost, MinDistance = 700,  Weight = 1.5f },
        };

        static readonly Weighted<PickupKind>[] PowerUpTable =
        {
            new Weighted<PickupKind> { Item = PickupKind.Helmet,       Weight = 3 },
            new Weighted<PickupKind> { Item = PickupKind.Magnet,       Weight = 3 },
            new Weighted<PickupKind> { Item = PickupKind.DoublePoints, Weight = 2.5f },
            new Weighted<PickupKind> { Item = PickupKind.BeastMode,    Weight = 1.5f },
        };

        public Transform WorldRoot { get; private set; }

        SpawnPool _pool;
        CareerStage _stage;
        readonly List<Spawnable> _live = new List<Spawnable>();
        readonly List<GroundChunk> _chunks = new List<GroundChunk>();
        readonly List<Spawnable> _scratch = new List<Spawnable>();
        float _scroll;
        float _nextChunkZ;
        float _nextRowZ;
        float _distanceAtScrollZero;
        int _previousRowEnemyMask;

        public void Build(CareerStage stage)
        {
            _stage = stage;
            WorldRoot = new GameObject("WorldRoot").transform;
            WorldRoot.SetParent(transform, false);
            var storage = new GameObject("PoolStorage").transform;
            storage.SetParent(transform, false);
            storage.gameObject.SetActive(false);
            _pool = new SpawnPool(storage);

            for (int v = 0; v < GroundVariants; v++)
            {
                int variant = v;
                _pool.Register(GroundKey(v), () => GroundChunk.Create(variant, _stage.Ground, ChunkLength));
            }
            foreach (ObstacleKind k in System.Enum.GetValues(typeof(ObstacleKind)))
            {
                var kind = k;
                _pool.Register("obs_" + kind, () => Smashable.Create(kind));
            }
            foreach (EnemyKind k in System.Enum.GetValues(typeof(EnemyKind)))
            {
                var kind = k;
                _pool.Register("enemy_" + kind, () => Enemy.Create(kind));
            }
            foreach (PickupKind k in System.Enum.GetValues(typeof(PickupKind)))
            {
                var kind = k;
                _pool.Register("pickup_" + kind, () => Pickup.Create(kind));
            }
        }

        static string GroundKey(int v) => "ground_" + v;

        public void ResetTrack()
        {
            foreach (var s in _live) _pool.Release(s);
            foreach (var c in _chunks) _pool.Release(c);
            _live.Clear();
            _chunks.Clear();
            // leftover debris
            for (int i = WorldRoot.childCount - 1; i >= 0; i--) Destroy(WorldRoot.GetChild(i).gameObject);

            _scroll = 0f;
            _distanceAtScrollZero = 0f;
            _previousRowEnemyMask = 0;
            WorldRoot.localPosition = Vector3.zero;
            _nextChunkZ = -ChunkLength;
            _nextRowZ = SafeStart;
            Fill();
        }

        /// <summary>Scroll the world by dz metres and update everything on it.</summary>
        public void Advance(float dz, float dt, PlayerController player)
        {
            _scroll += dz;
            WorldRoot.localPosition = new Vector3(0f, 0f, -_scroll);
            Fill();

            _scratch.Clear();
            _scratch.AddRange(_live);
            foreach (var s in _scratch)
            {
                if (s.Released) continue;
                s.Tick(dt, player);
                if (s.transform.position.z < DespawnBehind)
                {
                    _pool.Release(s);
                    _live.Remove(s);
                }
            }
            for (int i = _chunks.Count - 1; i >= 0; i--)
            {
                if (_chunks[i].transform.position.z + ChunkLength < DespawnBehind)
                {
                    _pool.Release(_chunks[i]);
                    _chunks.RemoveAt(i);
                }
            }

            if (_scroll > RebaseEvery) Rebase();
        }

        void Fill()
        {
            while (_nextChunkZ - _scroll < SpawnAhead)
            {
                var chunk = (GroundChunk)_pool.Get(GroundKey(Random.Range(0, GroundVariants)), WorldRoot, new Vector3(0, 0, _nextChunkZ));
                _chunks.Add(chunk);
                _nextChunkZ += ChunkLength;
            }
            while (_nextRowZ - _scroll < SpawnAhead - 10f)
            {
                SpawnRow(_nextRowZ, _distanceAtScrollZero + _nextRowZ);
                _nextRowZ += RowSpacing;
            }
        }

        void Rebase()
        {
            float shift = _scroll;
            foreach (Transform child in WorldRoot)
            {
                child.localPosition -= new Vector3(0f, 0f, shift);
            }
            _distanceAtScrollZero += shift;
            _nextChunkZ -= shift;
            _nextRowZ -= shift;
            _scroll = 0f;
            WorldRoot.localPosition = Vector3.zero;
        }

        void SpawnRow(float z, float distance)
        {
            float difficulty = Mathf.Clamp01(distance / 2000f);
            int enemyMask = 0;

            // --- enemies (the only things you must avoid) ---
            int enemyCount = 0;
            float r = Random.value;
            if (r < 0.06f + 0.2f * difficulty) enemyCount = 2;
            else if (r < 0.28f + 0.3f * difficulty) enemyCount = 1;
            // never stack two full walls back to back
            if (enemyCount == 2 && CountBits(_previousRowEnemyMask) >= 2) enemyCount = 1;

            for (int i = 0; i < enemyCount; i++)
            {
                int lane = PickFreeLane(enemyMask);
                enemyMask |= 1 << lane;
                var kind = PickEnemy(enemyCount == 2);
                Spawn("enemy_" + kind, lane, 0f, z);
            }
            _previousRowEnemyMask = enemyMask;

            // --- smashables, footballs, power-ups in the other lanes ---
            for (int lane = 0; lane < 3; lane++)
            {
                if ((enemyMask & (1 << lane)) != 0) continue;
                float p = Random.value;
                if (p < 0.45f + 0.15f * difficulty)
                {
                    Spawn("obs_" + PickObstacle(distance), lane, 0f, z);
                }
                else if (p < 0.78f)
                {
                    bool arc = Random.value < 0.25f;
                    for (int b = 0; b < 5; b++)
                    {
                        float y = arc ? 0.9f + Mathf.Sin(b / 4f * Mathf.PI) * 1.6f : 0.9f;
                        Spawn("pickup_" + PickupKind.Football, lane, y, z - 4f + b * 2f);
                    }
                }
                else if (p < 0.81f && distance > 120f)
                {
                    Spawn("pickup_" + PickFromTable(PowerUpTable, float.MaxValue), lane, 1.1f, z);
                }
            }
        }

        void Spawn(string key, int laneIndex, float y, float z)
        {
            float x = (laneIndex - 1) * LaneWidth;
            _live.Add(_pool.Get(key, WorldRoot, new Vector3(x, y, z)));
        }

        static int PickFreeLane(int mask)
        {
            for (int tries = 0; tries < 10; tries++)
            {
                int lane = Random.Range(0, 3);
                if ((mask & (1 << lane)) == 0) return lane;
            }
            for (int lane = 0; lane < 3; lane++) if ((mask & (1 << lane)) == 0) return lane;
            return 1;
        }

        EnemyKind PickEnemy(bool mustStayInLane)
        {
            var roster = _stage.Enemies;
            var kind = roster[Random.Range(0, roster.Length)];
            if (mustStayInLane && Enemy.BehaviorFor(kind) == EnemyBehavior.LaneSwitcher)
            {
                foreach (var k in roster)
                    if (Enemy.BehaviorFor(k) != EnemyBehavior.LaneSwitcher) return k;
                return EnemyKind.Neighbor;
            }
            return kind;
        }

        static ObstacleKind PickObstacle(float distance) => PickFromTable(ObstacleTable, distance);

        static T PickFromTable<T>(Weighted<T>[] table, float distance)
        {
            float total = 0f;
            foreach (var w in table) if (distance >= w.MinDistance) total += w.Weight;
            float roll = Random.value * total;
            foreach (var w in table)
            {
                if (distance < w.MinDistance) continue;
                roll -= w.Weight;
                if (roll <= 0f) return w.Item;
            }
            return table[0].Item;
        }

        static int CountBits(int v)
        {
            int c = 0;
            while (v != 0) { c += v & 1; v >>= 1; }
            return c;
        }
    }
}
