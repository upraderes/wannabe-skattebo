using System;
using System.Collections.Generic;
using UnityEngine;

namespace WannabeSkattebo
{
    public enum StatType { Speed, Power, Agility, Magnet }

    /// <summary>An upgradeable piece of gear bought with banked points.</summary>
    [Serializable]
    public class GearItem
    {
        public string Id;
        public string Name;
        public string Description;
        public StatType Stat;
        public int MaxLevel;
        public int BaseCost;
        public float CostGrowth;

        public GearItem(string id, string name, string description, StatType stat, int maxLevel, int baseCost, float costGrowth)
        {
            Id = id; Name = name; Description = description; Stat = stat;
            MaxLevel = maxLevel; BaseCost = baseCost; CostGrowth = costGrowth;
        }
    }

    public enum EnemyKind { Neighbor, Teacher, OpposingPlayer, Police, IceAgent, FedAgent }

    /// <summary>A career stage: new setting, new chasers, higher bar to clear.</summary>
    [Serializable]
    public class CareerStage
    {
        public string Id;
        public string Name;
        public string Tagline;
        public int GoalScore;          // best score needed to unlock the NEXT stage
        public int RequiredGearLevels; // total gear levels needed to unlock THIS stage
        public float BaseSpeed;
        public float MaxSpeed;
        public float RampDistance;     // metres to reach MaxSpeed
        public EnemyKind[] Enemies;
        public Color Ground;
        public Color Sky;
        public bool Playable;          // MVP: only the backyard has content

        // How the kid looks once he has reached this stage of his career
        public float PlayerScale = 1f;
        public Color Jersey = new Color(0.10f, 0.25f, 0.75f);
        public Color Trim = new Color(1f, 0.85f, 0.1f);
        public Color Pants = new Color(0.95f, 0.95f, 0.95f);
        public bool FootballHelmet;    // youth league and up play in a real helmet

        public float SpeedAt(float distance)
        {
            float t = Mathf.Clamp01(distance / Mathf.Max(1f, RampDistance));
            // ease-out so the early game ramps quickly then plateaus
            t = 1f - (1f - t) * (1f - t);
            return Mathf.Lerp(BaseSpeed, MaxSpeed, t);
        }
    }

    /// <summary>Static content database. Swap to ScriptableObjects once there is authored art.</summary>
    public static class GameCatalog
    {
        public static readonly IReadOnlyList<GearItem> Gear = new List<GearItem>
        {
            // Premium prices: the first level costs a few good runs
            // and maxing a line is a long-term goal.
            new GearItem("cleats",  "Cleats",         "+6% run speed per level",              StatType.Speed,   5, 2500, 2.1f),
            new GearItem("pads",    "Shoulder Pads",  "Smash big stuff without stumbling",   StatType.Power,   5, 3000, 2.1f),
            new GearItem("gloves",  "Sticky Gloves",  "Pull footballs in from further away", StatType.Magnet,  5, 2000, 2.0f),
            new GearItem("juke",    "Juke Training",  "Faster lane changes",                 StatType.Agility, 5, 2750, 2.1f),
        };

        public static readonly IReadOnlyList<CareerStage> Stages = new List<CareerStage>
        {
            new CareerStage {
                Id = "backyard", PlayerScale = 0.8f, Jersey = new Color(0.9f, 0.25f, 0.25f), Trim = Color.white, Pants = new Color(0.25f, 0.4f, 0.75f), FootballHelmet = false,
                Name = "The Backyard", Tagline = "Truck the trash cans. Dodge the neighbors.",
                GoalScore = 5000, RequiredGearLevels = 0, BaseSpeed = 9f, MaxSpeed = 20f, RampDistance = 1500f,
                Enemies = new[] { EnemyKind.Neighbor, EnemyKind.Teacher },
                Ground = new Color(0.36f, 0.78f, 0.32f), Sky = new Color(0.55f, 0.82f, 1f), Playable = true },
            new CareerStage {
                Id = "youth", PlayerScale = 0.88f, Jersey = new Color(0.15f, 0.65f, 0.3f), Trim = new Color(1f, 0.9f, 0.2f), Pants = Color.white, FootballHelmet = true,
                Name = "Youth League", Tagline = "Pee-wee defenders want that ball.",
                GoalScore = 12000, RequiredGearLevels = 4, BaseSpeed = 10f, MaxSpeed = 22f, RampDistance = 1400f,
                Enemies = new[] { EnemyKind.OpposingPlayer, EnemyKind.Teacher },
                Ground = new Color(0.25f, 0.65f, 0.25f), Sky = new Color(0.6f, 0.85f, 1f) },
            new CareerStage {
                Id = "highschool", PlayerScale = 0.96f, Jersey = new Color(0.1f, 0.25f, 0.75f), Trim = new Color(1f, 0.85f, 0.1f), Pants = new Color(1f, 0.85f, 0.1f), FootballHelmet = true,
                Name = "High School", Tagline = "Friday night lights.",
                GoalScore = 25000, RequiredGearLevels = 8, BaseSpeed = 11f, MaxSpeed = 24f, RampDistance = 1300f,
                Enemies = new[] { EnemyKind.OpposingPlayer, EnemyKind.Police },
                Ground = new Color(0.2f, 0.55f, 0.2f), Sky = new Color(0.15f, 0.15f, 0.35f) },
            new CareerStage {
                Id = "college", PlayerScale = 1.05f, Jersey = new Color(1f, 0.5f, 0.1f), Trim = Color.white, Pants = Color.white, FootballHelmet = true,
                Name = "College", Tagline = "Campus chaos, bigger hits.",
                GoalScore = 45000, RequiredGearLevels = 13, BaseSpeed = 12f, MaxSpeed = 26f, RampDistance = 1200f,
                Enemies = new[] { EnemyKind.OpposingPlayer, EnemyKind.Police, EnemyKind.IceAgent },
                Ground = new Color(0.3f, 0.6f, 0.3f), Sky = new Color(1f, 0.7f, 0.5f) },
            new CareerStage {
                Id = "nfl", PlayerScale = 1.15f, Jersey = new Color(0.05f, 0.1f, 0.3f), Trim = new Color(0.85f, 0.7f, 0.3f), Pants = new Color(0.85f, 0.7f, 0.3f), FootballHelmet = true,
                Name = "The League", Tagline = "Earn the #44 jersey.",
                GoalScore = 80000, RequiredGearLevels = 18, BaseSpeed = 13f, MaxSpeed = 28f, RampDistance = 1100f,
                Enemies = new[] { EnemyKind.OpposingPlayer, EnemyKind.FedAgent, EnemyKind.IceAgent },
                Ground = new Color(0.18f, 0.5f, 0.18f), Sky = new Color(0.1f, 0.1f, 0.25f) },
        };

        public static GearItem FindGear(string id)
        {
            foreach (var g in Gear) if (g.Id == id) return g;
            return null;
        }
    }
}
