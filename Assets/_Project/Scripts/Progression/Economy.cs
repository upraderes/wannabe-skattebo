using UnityEngine;

namespace WannabeSkattebo
{
    public enum PurchaseResult { Ok, MaxLevel, NotEnoughPoints, UnknownItem }

    /// <summary>Gear pricing, purchases, run payouts and career unlock rules.</summary>
    public static class Economy
    {
        public static int CostForNextLevel(GearItem item, int currentLevel)
        {
            if (item == null || currentLevel >= item.MaxLevel) return -1;
            return Mathf.RoundToInt(item.BaseCost * Mathf.Pow(item.CostGrowth, currentLevel) / 10f) * 10;
        }

        public static PurchaseResult TryBuy(SaveData save, string gearId)
        {
            var item = GameCatalog.FindGear(gearId);
            if (item == null) return PurchaseResult.UnknownItem;
            int level = save.GetGearLevel(gearId);
            if (level >= item.MaxLevel) return PurchaseResult.MaxLevel;
            int cost = CostForNextLevel(item, level);
            if (save.Wallet < cost) return PurchaseResult.NotEnoughPoints;
            save.Wallet -= cost;
            save.SetGearLevel(gearId, level + 1);
            return PurchaseResult.Ok;
        }

        /// <summary>Banks a finished run. Returns true if a new career stage was unlocked.</summary>
        public static bool BankRun(SaveData save, int score, int distance)
        {
            save.Wallet += Mathf.Max(0, score);
            save.TotalRuns++;
            if (score > save.BestScore) save.BestScore = score;
            if (distance > save.BestDistance) save.BestDistance = distance;
            return RefreshUnlocks(save);
        }

        public static bool CanUnlockStage(SaveData save, int stageIndex)
        {
            var stages = GameCatalog.Stages;
            if (stageIndex <= 0) return true;
            if (stageIndex >= stages.Count) return false;
            var previous = stages[stageIndex - 1];
            var stage = stages[stageIndex];
            return save.BestScore >= previous.GoalScore && save.TotalGearLevels() >= stage.RequiredGearLevels;
        }

        public static bool RefreshUnlocks(SaveData save)
        {
            bool unlocked = false;
            while (save.HighestStageUnlocked + 1 < GameCatalog.Stages.Count
                   && CanUnlockStage(save, save.HighestStageUnlocked + 1))
            {
                save.HighestStageUnlocked++;
                unlocked = true;
            }
            return unlocked;
        }
    }
}
