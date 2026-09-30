using System;
using System.Collections.Generic;
using UnityEngine;

namespace WannabeSkattebo
{
    public enum GameState { Menu, Running, Paused, Caught, Results }

    public class RunResult
    {
        public int Score;
        public int Distance;
        public int Smashes;
        public EnemyKind? CaughtBy;
        public bool NewBest;
        public bool UnlockedStage;
        public int BankedPoints;
    }

    /// <summary>Owns the run loop, collisions, power-ups and persistence. Builds the whole scene from code.</summary>
    public class GameManager : MonoBehaviour
    {
        const float CaughtDelay = 1.3f;
        const float MagnetPowerUpRadius = 7f;

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }
        public SaveData Save => _saves.Data;
        public CareerStage Stage { get; private set; }
        public ScoreKeeper Score { get; } = new ScoreKeeper();
        public float Distance { get; private set; }
        public float Speed { get; private set; }
        public float RunTime { get; private set; }
        public PlayerStats Stats { get; private set; }
        public RunResult LastRun { get; private set; }

        /// <summary>World position, text, colour.</summary>
        public event Action<Vector3, string, Color> Popup;

        SaveSystem _saves;
        PlayerController _player;
        TrackSpawner _track;
        FollowCamera _cam;
        Sfx _sfx;
        float _caughtTimer;
        EnemyKind _caughtBy;
        readonly Dictionary<PickupKind, float> _powerUps = new Dictionary<PickupKind, float>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            _saves = new SaveSystem();
            _saves.Load();
            Economy.RefreshUnlocks(Save);
            Stage = GameCatalog.Stages[0];
            Stats = PlayerStats.FromSave(Save);

            BuildEnvironment();

            _track = new GameObject("Track").AddComponent<TrackSpawner>();
            _track.Build(Stage);
            _track.ResetTrack();

            var kid = new GameObject("Kid");
            kid.AddComponent<Rigidbody>();
            _player = kid.AddComponent<PlayerController>();
            _player.Build(CurrentLook());
            _player.OnHit = HandleHit;

            _cam.Target = _player.transform;
            _cam.SnapToTarget();

            _sfx = new Sfx(gameObject);
            gameObject.AddComponent<GameUI>();

            State = GameState.Menu;
        }

        void BuildEnvironment()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Stage.Sky;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 140f;
            // (no ?? here: Unity objects use a custom null check)
            _cam = cam.GetComponent<FollowCamera>();
            if (_cam == null) _cam = cam.gameObject.AddComponent<FollowCamera>();

            Light sun = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Stage.Sky;
            RenderSettings.ambientEquatorColor = Color.Lerp(Stage.Sky, Stage.Ground, 0.5f);
            RenderSettings.ambientGroundColor = Stage.Ground * 0.6f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Stage.Sky;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 110f;
        }

        // ---------------- State transitions ----------------

        public void StartRun()
        {
            Stats = PlayerStats.FromSave(Save);
            _track.ResetTrack();
            _player.SetLook(CurrentLook());
            _player.ResetForRun();
            _player.LaneChangeTime = Stats.LaneChangeTime;
            _player.SetHelmet(false);
            _cam.SnapToTarget();
            Score.Reset();
            _powerUps.Clear();
            Distance = 0f;
            RunTime = 0f;
            LastRun = null;
            State = GameState.Running;
        }

        public void Pause()
        {
            if (State == GameState.Running) State = GameState.Paused;
        }

        public void Resume()
        {
            if (State == GameState.Paused) State = GameState.Running;
        }

        /// <summary>Ends the run voluntarily from the pause menu. Points are still banked.</summary>
        public void QuitRun()
        {
            if (State == GameState.Paused || State == GameState.Running) FinishRun(null);
        }

        public void GoToMenu()
        {
            _player.SetLook(CurrentLook());
            _track.ResetTrack();
            _player.ResetForRun();
            _player.SetHelmet(false);
            _cam.SnapToTarget();
            State = GameState.Menu;
        }

        /// <summary>The career stage the kid has reached (drives how he looks), not the level being played.</summary>
        public CareerStage CareerStage => GameCatalog.Stages[Mathf.Clamp(Save.HighestStageUnlocked, 0, GameCatalog.Stages.Count - 1)];

        ModelFactory.KidLook CurrentLook() => ModelFactory.KidLook.From(CareerStage, Save);

        public PurchaseResult BuyGear(string id)
        {
            var result = Economy.TryBuy(Save, id);
            if (result == PurchaseResult.Ok)
            {
                Economy.RefreshUnlocks(Save);
                _saves.Save();
                _player.SetLook(CurrentLook());
                _sfx.Play(_sfx.PowerUp, 0.6f);
            }
            return result;
        }

        void Caught(EnemyKind by)
        {
            State = GameState.Caught;
            _caughtBy = by;
            _caughtTimer = CaughtDelay;
            _player.PlayCaught();
            _cam.Shake(0.35f);
            _sfx.Play(_sfx.Whistle);
            Haptics.Heavy();
        }

        void FinishRun(EnemyKind? caughtBy)
        {
            int score = Score.Score;
            bool newBest = score > Save.BestScore;
            bool unlocked = Economy.BankRun(Save, score, Mathf.FloorToInt(Distance));
            _saves.Save();
            LastRun = new RunResult
            {
                Score = score,
                Distance = Mathf.FloorToInt(Distance),
                Smashes = Score.Smashes,
                CaughtBy = caughtBy,
                NewBest = newBest,
                UnlockedStage = unlocked,
                BankedPoints = score,
            };
            State = GameState.Results;
        }

        // ---------------- Loop ----------------

        void Update()
        {
            float dt = Time.deltaTime;
            switch (State)
            {
                case GameState.Running:
                    TickRun(dt);
                    break;
                case GameState.Caught:
                    _player.TickRun(dt, 0f);
                    _track.Advance(0f, dt, _player);
                    _caughtTimer -= dt;
                    if (_caughtTimer <= 0f) FinishRun(_caughtBy);
                    break;
                case GameState.Menu:
                case GameState.Results:
                    _player.TickIdle(dt);
                    break;
            }
        }

        void TickRun(float dt)
        {
            RunTime += dt;
            TickPowerUps(dt);

            float speed = Stage.SpeedAt(Distance) * Stats.SpeedMultiplier;
            if (IsActive(PickupKind.BeastMode)) speed *= 1.3f;
            if (_player.IsStumbling) speed *= 0.7f;
            Speed = speed;

            float dz = speed * dt;
            Distance += dz;
            Score.PowerUpMultiplier = IsActive(PickupKind.DoublePoints) ? 2f : 1f;
            Score.AddDistance(dz);
            Score.Tick(dt);

            _player.MagnetRadius = IsActive(PickupKind.Magnet) ? MagnetPowerUpRadius : Stats.MagnetRadius;
            _player.SetHelmet(IsActive(PickupKind.Helmet));
            _player.TickRun(dt, speed);
            _track.Advance(dz, dt, _player);
        }

        void TickPowerUps(float dt)
        {
            if (_powerUps.Count == 0) return;
            var keys = new List<PickupKind>(_powerUps.Keys);
            foreach (var k in keys)
            {
                _powerUps[k] -= dt;
                if (_powerUps[k] <= 0f) _powerUps.Remove(k);
            }
        }

        public bool IsActive(PickupKind kind) => _powerUps.ContainsKey(kind);

        public float Remaining(PickupKind kind) => _powerUps.TryGetValue(kind, out var t) ? t : 0f;

        public IEnumerable<KeyValuePair<PickupKind, float>> ActivePowerUps => _powerUps;

        // ---------------- Collisions ----------------

        void HandleHit(Collider other)
        {
            if (State != GameState.Running) return;

            var smashable = other.GetComponent<Smashable>();
            if (smashable != null)
            {
                bool beast = IsActive(PickupKind.BeastMode);
                bool stumble = smashable.Smash(Stats.SmashPower, beast);
                int pts = Score.AddSmash(smashable.Def.Points);
                string text = Score.Combo > 1 ? $"+{pts}  x{Score.Combo}" : $"+{pts}";
                Popup?.Invoke(other.transform.position + Vector3.up * 1.5f, text, ModelFactory.Palette.Yellow);
                _cam.Shake(0.08f + smashable.Def.Toughness * 0.05f);
                _sfx.Play(smashable.Def.Toughness >= 3 ? _sfx.BigSmash : _sfx.Smash);
                if (stumble)
                {
                    _player.Stumble();
                    Score.BreakCombo();
                    Popup?.Invoke(_player.transform.position + Vector3.up * 2.2f, "OOF! Need more Power", ModelFactory.Palette.Red);
                    Haptics.Light();
                }
                return;
            }

            var enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (IsActive(PickupKind.BeastMode))
                {
                    enemy.KnockAway(_player.transform.position.x);
                    _sfx.Play(_sfx.Juke);
                    Popup?.Invoke(other.transform.position + Vector3.up * 2f, "JUKED!", ModelFactory.Palette.Cyan);
                }
                else if (IsActive(PickupKind.Helmet))
                {
                    _powerUps.Remove(PickupKind.Helmet);
                    enemy.KnockAway(_player.transform.position.x);
                    _cam.Shake(0.25f);
                    _sfx.Play(_sfx.BigSmash);
                    Popup?.Invoke(other.transform.position + Vector3.up * 2f, "HELMET SAVE!", ModelFactory.Palette.Cyan);
                    Haptics.Light();
                }
                else
                {
                    Caught(enemy.Kind);
                }
                return;
            }

            var pickup = other.GetComponent<Pickup>();
            if (pickup != null)
            {
                pickup.Collect();
                if (pickup.Kind == PickupKind.Football)
                {
                    Score.AddPickup(Pickup.FootballPoints);
                    _sfx.Play(_sfx.Football, 0.5f);
                }
                else
                {
                    _powerUps[pickup.Kind] = PowerUpInfo.Duration(pickup.Kind);
                    _sfx.Play(_sfx.PowerUp);
                    Popup?.Invoke(_player.transform.position + Vector3.up * 2.2f, PowerUpInfo.Label(pickup.Kind), ModelFactory.Palette.Cyan);
                }
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Pause();
        }
    }

    public static class Haptics
    {
        public static void Light()
        {
            // Hook up a native UIImpactFeedbackGenerator plugin later; the built-in vibrate is too long for light taps.
        }

        public static void Heavy()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
