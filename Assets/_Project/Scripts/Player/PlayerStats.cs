namespace WannabeSkattebo
{
    /// <summary>Derived gameplay numbers from gear levels.</summary>
    public readonly struct PlayerStats
    {
        public readonly int Speed, Power, Agility, Magnet;

        public PlayerStats(int speed, int power, int agility, int magnet)
        {
            Speed = speed; Power = power; Agility = agility; Magnet = magnet;
        }

        public static PlayerStats FromSave(SaveData save)
        {
            int speed = 0, power = 0, agility = 0, magnet = 0;
            foreach (var item in GameCatalog.Gear)
            {
                int lvl = save.GetGearLevel(item.Id);
                switch (item.Stat)
                {
                    case StatType.Speed: speed += lvl; break;
                    case StatType.Power: power += lvl; break;
                    case StatType.Agility: agility += lvl; break;
                    case StatType.Magnet: magnet += lvl; break;
                }
            }
            return new PlayerStats(speed, power, agility, magnet);
        }

        public float SpeedMultiplier => 1f + 0.06f * Speed;
        /// <summary>Seconds to move one lane.</summary>
        public float LaneChangeTime => 0.16f * (1f - 0.08f * Agility);
        /// <summary>Radius (m) footballs are pulled from without the magnet power-up.</summary>
        public float MagnetRadius => 0.8f * Magnet;
        /// <summary>Obstacles with toughness above this cause a stumble.</summary>
        public int SmashPower => 1 + Power;
    }
}
