using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using GameArifiction.TimingCatch;

namespace GameArifiction.Tests.Editor
{
    [TestFixture]
    public sealed class TimingCatchPdfRulesTests
    {
        private TimingCatchGameConfigSO m_config;

        [SetUp]
        public void SetUp() => m_config = ScriptableObject.CreateInstance<TimingCatchGameConfigSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_config);

        [Test]
        public void DefaultConfig_UsesFourRoundsOfThreeDifficultyTurns()
        {
            Assert.AreEqual(4, m_config.RoundCount);
            Assert.AreEqual(3, m_config.TurnsPerRound);
            Assert.AreEqual(12, m_config.TotalTurnCount);
            Assert.AreEqual(6f, m_config.TurnTimeoutSeconds);
            CollectionAssert.AreEqual(new[] { 2f, 1.5f, 1f }, m_config.CreateDifficultyRoundTripSecondsSnapshot());
            CollectionAssert.AreEqual(new[] { .2f, .15f, .1f }, m_config.CreateDifficultyGreatZoneWidthsSnapshot());
        }

        [Test]
        public void Model_UsesNormalizedRoundTripSpeedAndGreatWidth()
        {
            var model = new TimingCatchGameModel(m_config);

            Assert.AreEqual(1f, model.CurrentSpeed, .0001f);
            Assert.AreEqual(.2f, model.CurrentGreatZoneWidth, .0001f);
            model.AdvanceToNextTurn();
            Assert.AreEqual(1.3333333f, model.CurrentSpeed, .0001f);
            Assert.AreEqual(.15f, model.CurrentGreatZoneWidth, .0001f);
        }

        [Test]
        public void ViewModel_MissResetsComboAndIntermissionBlocksInput()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            viewModel.StartGame();
            viewModel.UpdateTick(6.01f);

            Assert.AreEqual(1, viewModel.MissCount);
            Assert.IsTrue(viewModel.IsIntermission);
            int score = viewModel.Score;
            viewModel.EvaluateInput();
            Assert.AreEqual(score, viewModel.Score);
        }

        [Test]
        public void ViewModel_AllGreatTurns_CapsAtTwoThousandWithRoundBonuses()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            viewModel.StartGame();
            for (int turn = 0; turn < 12; turn++)
            {
                viewModel.UpdateTick(.5f / model.CurrentSpeed);
                viewModel.EvaluateInput();
                viewModel.UpdateTick(turn % 3 == 2 ? 2.01f : 1.01f);
            }

            Assert.AreEqual(2000, viewModel.Score);
            Assert.AreEqual(12, viewModel.GreatCount);
            Assert.AreEqual(4, viewModel.GreatBonusCount);
        }

        [Test]
        public void ViewModel_ReportsWholeTurnAndResetsRoundComboAfterExactIntermissions()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            TimingCatchGameState latest = default;
            viewModel.OnStateChanged += state => latest = state;
            viewModel.StartGame();

            for (int turn = 0; turn < 3; turn++)
            {
                viewModel.UpdateTick(.5f / model.CurrentSpeed);
                viewModel.EvaluateInput();
                if (turn == 0)
                {
                    Assert.AreEqual(1f, viewModel.IntermissionRemaining, .0001f);
                    viewModel.UpdateTick(.999f);
                    Assert.IsTrue(viewModel.IsIntermission);
                    viewModel.UpdateTick(.0011f);
                }
                else if (turn == 1)
                {
                    viewModel.UpdateTick(1.001f);
                }
                else
                {
                    Assert.AreEqual(2f, viewModel.IntermissionRemaining, .0001f);
                    viewModel.UpdateTick(2.001f);
                }
            }

            Assert.AreEqual(2, latest.CurrentRound);
            Assert.AreEqual(1, latest.CurrentTurn);
            Assert.AreEqual(4, latest.CurrentTurnTotal);
            Assert.AreEqual(4, latest.TotalTurn);
            Assert.AreEqual(1, latest.TurnInRound);
            Assert.AreEqual(0, latest.ConsecutiveGreat);
            Assert.IsTrue(viewModel.InputEnabled);
        }

        [Test]
        public void ViewModel_FinalHardIntermissionKeepsLastValidTurnDisplay()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            TimingCatchGameState latest = default;
            viewModel.OnStateChanged += state => latest = state;
            viewModel.StartGame();

            for (int turn = 0; turn < 12; turn++)
            {
                viewModel.UpdateTick(.5f / model.CurrentSpeed);
                viewModel.EvaluateInput();
                if (turn < 11) viewModel.UpdateTick(turn % 3 == 2 ? 2.001f : 1.001f);
            }

            Assert.IsTrue(latest.IsIntermission);
            Assert.AreEqual(4, latest.CurrentRound);
            Assert.AreEqual(3, latest.CurrentTurn);
            Assert.AreEqual(12, latest.CurrentTurnTotal);
            Assert.AreEqual(TimingCatchDifficulty.Hard, latest.Difficulty);
            Assert.IsFalse(latest.InputEnabled);
        }

        [Test]
        public void View_MapsStartRoundsAndFinalIntermissionToSlideSprites()
        {
            MethodInfo getSlideIndex = typeof(TimingCatchGameView).GetMethod("GetSlideIndex", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(getSlideIndex);

            var start = new TimingCatchGameState { CurrentRound = 1, CurrentTurnTotal = 1 };
            var roundTwo = new TimingCatchGameState { CurrentRound = 2, CurrentTurnTotal = 4 };
            var finish = new TimingCatchGameState { CurrentRound = 4, CurrentTurnTotal = 12, IsIntermission = true };
            Assert.AreEqual(0, getSlideIndex.Invoke(null, new object[] { start }));
            Assert.AreEqual(2, getSlideIndex.Invoke(null, new object[] { roundTwo }));
            Assert.AreEqual(5, getSlideIndex.Invoke(null, new object[] { finish }));
        }

        [Test]
        public void JudgeCalculator_UsesGreatOnlyAndTimeoutIsMiss()
        {
            var calculator = new TimingCatchJudgeCalculator();

            Assert.AreEqual(TimingCatchJudgeType.Great, calculator.Evaluate(.59f, .1f, 0f));
            Assert.AreEqual(TimingCatchJudgeType.Miss, calculator.Evaluate(.61f, .1f, 0f));
        }
    }
}
