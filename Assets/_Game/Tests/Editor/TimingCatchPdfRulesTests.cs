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
            AdvanceToPlaying(viewModel);
            viewModel.UpdateTick(6.01f);

            Assert.AreEqual(1, viewModel.MissCount);
            Assert.AreEqual(TimingCatchPhase.JudgeResult, viewModel.Phase);
            int score = viewModel.Score;
            viewModel.EvaluateInput();
            Assert.AreEqual(score, viewModel.Score);
        }

        [Test]
        public void ViewModel_UsesIntroBeforePlayingAndRejectsInputDuringIntro()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            viewModel.StartGame();

            Assert.AreEqual(TimingCatchPhase.Intro, viewModel.Phase);
            Assert.IsFalse(viewModel.InputEnabled);
            viewModel.EvaluateInput();
            Assert.AreEqual(0, viewModel.Score);

            AdvanceToPlaying(viewModel);
            Assert.IsTrue(viewModel.InputEnabled);
        }

        [Test]
        public void ViewModel_AllGreatTurns_CapsAtTwoThousandWithRoundBonuses()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            viewModel.StartGame();
            AdvanceToPlaying(viewModel);
            for (int turn = 0; turn < 12; turn++)
            {
                viewModel.UpdateTick(.5f / model.CurrentSpeed);
                viewModel.EvaluateInput();
                AdvanceToPlayingOrCompleted(viewModel);
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
            AdvanceToPlaying(viewModel);

            for (int turn = 0; turn < 3; turn++)
            {
                viewModel.UpdateTick(.5f / model.CurrentSpeed);
                viewModel.EvaluateInput();
                AdvanceToPlayingOrCompleted(viewModel);
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
            AdvanceToPlaying(viewModel);

            for (int turn = 0; turn < 12; turn++)
            {
                viewModel.UpdateTick(.5f / model.CurrentSpeed);
                viewModel.EvaluateInput();
                if (turn < 11) AdvanceToPlayingOrCompleted(viewModel);
            }

            Assert.AreEqual(TimingCatchPhase.JudgeResult, latest.Phase);
            Assert.AreEqual(4, latest.CurrentRound);
            Assert.AreEqual(3, latest.CurrentTurn);
            Assert.AreEqual(12, latest.CurrentTurnTotal);
            Assert.AreEqual(TimingCatchDifficulty.Hard, latest.Difficulty);
            Assert.IsFalse(latest.InputEnabled);
        }

        [Test]
        public void ViewModel_TurnGapIsExactlyOneSecondWithJudgeVisible()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            TimingCatchGameState latest = default;
            viewModel.OnStateChanged += state => latest = state;
            viewModel.StartGame();
            AdvanceToPlaying(viewModel);

            viewModel.UpdateTick(6.01f);

            viewModel.UpdateTick(.99f);
            Assert.AreNotEqual(TimingCatchPhase.Playing, viewModel.Phase);
            Assert.AreEqual(TimingCatchJudgeType.Miss, latest.JudgeType);
            viewModel.UpdateTick(.02f);
            Assert.AreEqual(TimingCatchPhase.Playing, viewModel.Phase);
        }

        [Test]
        public void ViewModel_RoundGapIsExactlyTwoSeconds()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            viewModel.StartGame();
            AdvanceToPlaying(viewModel);
            for (int turn = 0; turn < 2; turn++)
            {
                viewModel.UpdateTick(.5f / model.CurrentSpeed);
                viewModel.EvaluateInput();
                AdvanceToPlayingOrCompleted(viewModel);
            }

            viewModel.UpdateTick(.5f / model.CurrentSpeed);
            viewModel.EvaluateInput();

            viewModel.UpdateTick(1.99f);
            Assert.AreNotEqual(TimingCatchPhase.Playing, viewModel.Phase);
            viewModel.UpdateTick(.02f);
            Assert.AreEqual(TimingCatchPhase.Playing, viewModel.Phase);
        }

        [Test]
        public void ViewModel_FinalTurnKeepsTwoSecondGapBeforeOutro()
        {
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config);
            viewModel.StartGame();
            AdvanceToPlaying(viewModel);
            for (int turn = 0; turn < 11; turn++)
            {
                viewModel.UpdateTick(.5f / model.CurrentSpeed);
                viewModel.EvaluateInput();
                AdvanceToPlayingOrCompleted(viewModel);
            }

            viewModel.UpdateTick(.5f / model.CurrentSpeed);
            viewModel.EvaluateInput();

            viewModel.UpdateTick(1.99f);
            Assert.AreEqual(TimingCatchPhase.JudgeResult, viewModel.Phase);
            viewModel.UpdateTick(.02f);
            Assert.AreEqual(TimingCatchPhase.Outro, viewModel.Phase);
        }

        [Test]
        public void View_MapsStartRoundsAndFinalIntermissionToSlideSprites()
        {
            MethodInfo getSlideIndex = typeof(TimingCatchGameView).GetMethod("GetSlideIndex", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(getSlideIndex);

            var start = new TimingCatchGameState { Phase = TimingCatchPhase.Intro };
            var roundTwo = new TimingCatchGameState { CurrentRound = 2, CurrentTurnTotal = 4 };
            var finish = new TimingCatchGameState { Phase = TimingCatchPhase.Outro };
            Assert.AreEqual(0, getSlideIndex.Invoke(null, new object[] { start }));
            Assert.AreEqual(2, getSlideIndex.Invoke(null, new object[] { roundTwo }));
            Assert.AreEqual(5, getSlideIndex.Invoke(null, new object[] { finish }));
        }

        [Test]
        public void View_UpdateJudgeZone_ClearsHorizontalOffsetAndSizeWhilePreservingVerticalValues()
        {
            GameObject viewObject = new GameObject("TimingCatchGameView");
            GameObject parentObject = new GameObject("Gauge", typeof(RectTransform));
            GameObject zoneObject = new GameObject("GreatZone", typeof(RectTransform));
            zoneObject.transform.SetParent(parentObject.transform, false);
            try
            {
                RectTransform zone = zoneObject.GetComponent<RectTransform>();
                zone.anchorMin = new Vector2(.1f, .23f);
                zone.anchorMax = new Vector2(.9f, .77f);
                zone.anchoredPosition = new Vector2(170.22f, 21f);
                zone.sizeDelta = new Vector2(700f, 33f);

                MethodInfo updateJudgeZone = typeof(TimingCatchGameView).GetMethod(
                    "UpdateJudgeZone",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                Assert.IsNotNull(updateJudgeZone);
                updateJudgeZone.Invoke(
                    viewObject.AddComponent<TimingCatchGameView>(),
                    new object[] { zone, .1f }
                );

                Assert.AreEqual(.4f, zone.anchorMin.x, .0001f);
                Assert.AreEqual(.6f, zone.anchorMax.x, .0001f);
                Assert.AreEqual(.23f, zone.anchorMin.y, .0001f);
                Assert.AreEqual(.77f, zone.anchorMax.y, .0001f);
                Assert.AreEqual(0f, zone.anchoredPosition.x, .0001f);
                Assert.AreEqual(21f, zone.anchoredPosition.y, .0001f);
                Assert.AreEqual(0f, zone.sizeDelta.x, .0001f);
                Assert.AreEqual(33f, zone.sizeDelta.y, .0001f);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(parentObject);
            }
        }

        [Test]
        public void JudgeCalculator_UsesGreatOnlyAndTimeoutIsMiss()
        {
            var calculator = new TimingCatchJudgeCalculator();

            Assert.AreEqual(TimingCatchJudgeType.Great, calculator.Evaluate(.59f, .1f, 0f));
            Assert.AreEqual(TimingCatchJudgeType.Miss, calculator.Evaluate(.61f, .1f, 0f));
        }

        private static void AdvanceToPlaying(TimingCatchGameViewModel viewModel)
        {
            AdvanceToPlayingOrCompleted(viewModel);
            Assert.AreEqual(TimingCatchPhase.Playing, viewModel.Phase);
        }

        private static void AdvanceToPlayingOrCompleted(TimingCatchGameViewModel viewModel)
        {
            for (int i = 0; i < 10 && viewModel.Phase != TimingCatchPhase.Playing && viewModel.Phase != TimingCatchPhase.Completed; i++)
            {
                viewModel.UpdateTick(10f);
            }
        }
    }
}
