using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>Run score: distance points + smash points, with a combo chain and power-up multiplier.</summary>
    public class ScoreKeeper
    {
        public const float ComboWindow = 2.0f;
        public const int MaxCombo = 5;

        public int Score { get; private set; }
        public int Smashes { get; private set; }
        public int Combo { get; private set; } = 1;
        public float PowerUpMultiplier { get; set; } = 1f;

        float _distanceCarry;
        float _comboTimer;

        public void Reset()
        {
            Score = 0; Smashes = 0; Combo = 1; PowerUpMultiplier = 1f;
            _distanceCarry = 0f; _comboTimer = 0f;
        }

        public void Tick(float dt)
        {
            if (_comboTimer > 0f)
            {
                _comboTimer -= dt;
                if (_comboTimer <= 0f) Combo = 1;
            }
        }

        /// <summary>1 point per metre run (times the power-up multiplier).</summary>
        public void AddDistance(float metres)
        {
            _distanceCarry += metres * PowerUpMultiplier;
            int whole = Mathf.FloorToInt(_distanceCarry);
            if (whole > 0)
            {
                Score += whole;
                _distanceCarry -= whole;
            }
        }

        /// <summary>Awards smash points and extends the combo. Returns points awarded.</summary>
        public int AddSmash(int basePoints)
        {
            if (_comboTimer > 0f) Combo = Mathf.Min(MaxCombo, Combo + 1);
            _comboTimer = ComboWindow;
            Smashes++;
            int pts = Mathf.RoundToInt(basePoints * Combo * PowerUpMultiplier);
            Score += pts;
            return pts;
        }

        public int AddPickup(int basePoints)
        {
            int pts = Mathf.RoundToInt(basePoints * PowerUpMultiplier);
            Score += pts;
            return pts;
        }

        public void BreakCombo()
        {
            Combo = 1;
            _comboTimer = 0f;
        }
    }
}
