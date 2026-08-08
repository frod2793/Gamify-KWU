using NUnit.Framework;
using UnityEngine;
using GameArifiction.TimingCatch;

namespace GameArifiction.Tests.Editor
{
    [TestFixture]
    public sealed class TimingCatchGameModelTests
    {
        private TimingCatchGameConfigSO m_config;

        [SetUp]
        public void SetUp() => m_config = ScriptableObject.CreateInstance<TimingCatchGameConfigSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_config);

        [Test]
        public void DefaultConfig_HasTwelveTurnsAndRepeatsEasyNormalHard()
        {
            var model = new TimingCatchGameModel(m_config);
            Assert.AreEqual(12, model.MaxTurnCount);
            Assert.AreEqual(TimingCatchDifficulty.Easy, model.CurrentDifficulty);
            model.AdvanceToNextTurn();
            Assert.AreEqual(TimingCatchDifficulty.Normal, model.CurrentDifficulty);
            model.AdvanceToNextTurn();
            Assert.AreEqual(TimingCatchDifficulty.Hard, model.CurrentDifficulty);
            model.AdvanceToNextTurn();
            Assert.AreEqual(TimingCatchDifficulty.Easy, model.CurrentDifficulty);
            Assert.AreEqual(2, model.CurrentRound);
        }

        [Test]
        public void GaugeSpeed_IsTwoNormalizedUnitsPerRoundTrip()
        {
            var model = new TimingCatchGameModel(m_config);
            Assert.AreEqual(1f, model.CurrentSpeed, .0001f);
            model.AdvanceToNextTurn();
            Assert.AreEqual(4f / 3f, model.CurrentSpeed, .0001f);
            model.AdvanceToNextTurn();
            Assert.AreEqual(2f, model.CurrentSpeed, .0001f);
        }

        [Test]
        public void Gauge_CompletesOneRoundTripInConfiguredTime()
        {
            var model = new TimingCatchGameModel(m_config);
            model.UpdateGauge(.5f);
            Assert.AreEqual(.5f, model.GaugeNormalized, .0001f);
            model.UpdateGauge(.5f);
            Assert.AreEqual(1f, model.GaugeNormalized, .0001f);
            model.UpdateGauge(.5f);
            Assert.AreEqual(.5f, model.GaugeNormalized, .0001f);
            model.UpdateGauge(.5f);
            Assert.AreEqual(0f, model.GaugeNormalized, .0001f);
        }
    }
}
