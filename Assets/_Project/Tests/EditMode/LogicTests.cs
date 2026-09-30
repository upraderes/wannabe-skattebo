using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace WannabeSkattebo.Tests
{
    public class ScoreKeeperTests
    {
        [Test]
        public void Distance_AwardsOnePointPerMetre()
        {
            var s = new ScoreKeeper();
            s.AddDistance(0.6f);
            s.AddDistance(0.6f);
            Assert.AreEqual(1, s.Score);
            s.AddDistance(10f);
            Assert.AreEqual(11, s.Score);
        }

        [Test]
        public void ConsecutiveSmashes_BuildCombo_UpToMax()
        {
            var s = new ScoreKeeper();
            Assert.AreEqual(50, s.AddSmash(50));   // x1
            Assert.AreEqual(100, s.AddSmash(50));  // x2
            Assert.AreEqual(150, s.AddSmash(50));  // x3
            for (int i = 0; i < 10; i++) s.AddSmash(10);
            Assert.AreEqual(ScoreKeeper.MaxCombo, s.Combo);
        }

        [Test]
        public void Combo_ExpiresAfterWindow()
        {
            var s = new ScoreKeeper();
            s.AddSmash(10);
            s.AddSmash(10);
            Assert.AreEqual(2, s.Combo);
            s.Tick(ScoreKeeper.ComboWindow + 0.01f);
            Assert.AreEqual(1, s.Combo);
        }

        [Test]
        public void DoublePoints_DoublesSmashAndDistance()
        {
            var s = new ScoreKeeper { PowerUpMultiplier = 2f };
            Assert.AreEqual(100, s.AddSmash(50));
            s.AddDistance(5f);
            Assert.AreEqual(110, s.Score);
        }
    }

    public class EconomyTests
    {
        [Test]
        public void Cost_GrowsWithLevel_AndIsNegativeAtMax()
        {
            var cleats = GameCatalog.FindGear("cleats");
            int c0 = Economy.CostForNextLevel(cleats, 0);
            int c1 = Economy.CostForNextLevel(cleats, 1);
            Assert.AreEqual(cleats.BaseCost, c0);
            Assert.Greater(c1, c0);
            Assert.AreEqual(-1, Economy.CostForNextLevel(cleats, cleats.MaxLevel));
        }

        [Test]
        public void TryBuy_DeductsWallet_AndRaisesLevel()
        {
            var save = new SaveData { Wallet = 10000 };
            Assert.AreEqual(PurchaseResult.Ok, Economy.TryBuy(save, "pads"));
            Assert.AreEqual(1, save.GetGearLevel("pads"));
            Assert.AreEqual(10000 - GameCatalog.FindGear("pads").BaseCost, save.Wallet);
        }

        [Test]
        public void TryBuy_FailsWithoutFunds_OrAtMax()
        {
            var save = new SaveData { Wallet = 0 };
            Assert.AreEqual(PurchaseResult.NotEnoughPoints, Economy.TryBuy(save, "pads"));
            save.Wallet = 1_000_000;
            save.SetGearLevel("pads", GameCatalog.FindGear("pads").MaxLevel);
            Assert.AreEqual(PurchaseResult.MaxLevel, Economy.TryBuy(save, "pads"));
            Assert.AreEqual(PurchaseResult.UnknownItem, Economy.TryBuy(save, "jetpack"));
        }

        [Test]
        public void BankRun_UpdatesWalletAndBests()
        {
            var save = new SaveData();
            Economy.BankRun(save, 1200, 300);
            Economy.BankRun(save, 800, 500);
            Assert.AreEqual(2000, save.Wallet);
            Assert.AreEqual(1200, save.BestScore);
            Assert.AreEqual(500, save.BestDistance);
            Assert.AreEqual(2, save.TotalRuns);
        }

        [Test]
        public void StageUnlock_NeedsBothScoreAndGear()
        {
            var save = new SaveData();
            var backyard = GameCatalog.Stages[0];
            var youth = GameCatalog.Stages[1];

            Assert.IsFalse(Economy.BankRun(save, backyard.GoalScore, 100), "score alone should not unlock");
            Assert.AreEqual(0, save.HighestStageUnlocked);

            save.SetGearLevel("cleats", youth.RequiredGearLevels);
            Assert.IsTrue(Economy.RefreshUnlocks(save));
            Assert.AreEqual(1, save.HighestStageUnlocked);
        }
    }

    public class SaveSystemTests
    {
        string _path;

        [SetUp]
        public void SetUp() => _path = Path.Combine(Path.GetTempPath(), $"wsk_save_{System.Guid.NewGuid():N}.json");

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }

        [Test]
        public void SaveAndLoad_RoundTrips()
        {
            var a = new SaveSystem(_path);
            a.Load();
            a.Data.Wallet = 4242;
            a.Data.BestScore = 777;
            a.Data.SetGearLevel("gloves", 3);
            a.Save();

            var b = new SaveSystem(_path);
            var data = b.Load();
            Assert.AreEqual(4242, data.Wallet);
            Assert.AreEqual(777, data.BestScore);
            Assert.AreEqual(3, data.GetGearLevel("gloves"));
        }

        [Test]
        public void CorruptFile_StartsFresh()
        {
            File.WriteAllText(_path, "{not json");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var data = new SaveSystem(_path).Load();
            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Wallet);
        }
    }

    public class StageAndStatsTests
    {
        [Test]
        public void Speed_RampsFromBaseToMax()
        {
            var s = GameCatalog.Stages[0];
            Assert.AreEqual(s.BaseSpeed, s.SpeedAt(0f), 0.001f);
            Assert.AreEqual(s.MaxSpeed, s.SpeedAt(s.RampDistance * 2f), 0.001f);
            Assert.Greater(s.SpeedAt(s.RampDistance * 0.5f), s.BaseSpeed);
        }

        [Test]
        public void Gear_FeedsPlayerStats()
        {
            var save = new SaveData();
            save.SetGearLevel("cleats", 2);
            save.SetGearLevel("pads", 3);
            var stats = PlayerStats.FromSave(save);
            Assert.AreEqual(2, stats.Speed);
            Assert.AreEqual(4, stats.SmashPower);
            Assert.Greater(stats.SpeedMultiplier, 1f);
        }

        [Test]
        public void EveryStage_HasEnemies()
        {
            foreach (var s in GameCatalog.Stages)
                Assert.IsNotEmpty(s.Enemies, s.Id);
        }
    }
}
