using System;
using GameArifiction.Player;
using VContainer;

namespace GameArifiction.TimingCatch
{
    public sealed class TimingCatchGameViewModel : ITimingCatchJudgeEventSource
    {
        private readonly TimingCatchGameModel m_model;
        private readonly ITimingJudgeCalculator m_judgeCalculator;
        private readonly TimingCatchGameConfigSO m_config;
        private float m_playTime;
        private float m_intermissionRemaining;
        private int m_totalScore;
        private int m_greatCount;
        private int m_missCount;
        private int m_consecutiveGreat;
        private int m_greatBonusCount;
        private bool m_pendingResult;
        private bool m_resultPublished;

        public event Action<TimingCatchGameState> OnStateChanged;
        public event Action<TimingCatchJudgeType> OnJudgeEvaluated;
        public event Action<TimingCatchGameResultDTO> OnGameResult;

        public int Score => m_totalScore;
        public int GreatCount => m_greatCount;
        public int MissCount => m_missCount;
        public int ConsecutiveGreat => m_consecutiveGreat;
        public int GreatBonusCount => m_greatBonusCount;
        public bool IsIntermission => m_intermissionRemaining > 0f;
        public float IntermissionRemaining => m_intermissionRemaining;
        public bool InputEnabled => m_model != null && m_model.IsRunning && !IsIntermission;

        [Inject]
        public TimingCatchGameViewModel(TimingCatchGameModel model, ITimingJudgeCalculator judgeCalculator, TimingCatchGameConfigSO config)
        {
            m_model = model;
            m_judgeCalculator = judgeCalculator;
            m_config = config;
        }

        public void StartGame()
        {
            if (m_model == null || m_config == null) return;
            m_model.ResetState();
            m_playTime = 0f;
            m_intermissionRemaining = 0f;
            m_totalScore = 0;
            m_greatCount = 0;
            m_missCount = 0;
            m_consecutiveGreat = 0;
            m_greatBonusCount = 0;
            m_pendingResult = false;
            m_resultPublished = false;
            PushState();
        }

        public void UpdateTick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (m_intermissionRemaining > 0f)
            {
                m_intermissionRemaining = Math.Max(0f, m_intermissionRemaining - deltaTime);
                if (m_intermissionRemaining <= 0f && m_pendingResult) PublishResult();
                PushState();
                return;
            }
            if (m_model == null || !m_model.IsRunning || m_model.IsFinished) return;

            m_model.UpdateGauge(deltaTime);
            m_playTime += deltaTime;
            PushState();
            if (m_model.IsTurnTimeout) ApplyJudge(TimingCatchJudgeType.Miss);
        }

        public void EvaluateInput()
        {
            if (!InputEnabled || m_judgeCalculator == null) return;
            TimingCatchJudgeType judge = m_judgeCalculator.Evaluate(
                m_model.GaugeNormalized,
                m_model.CurrentGreatZoneHalfWidth,
                0f);
            ApplyJudge(judge == TimingCatchJudgeType.Great ? TimingCatchJudgeType.Great : TimingCatchJudgeType.Miss);
        }

        public void NotifyState() => PushState();

        private void ApplyJudge(TimingCatchJudgeType judge)
        {
            if (!InputEnabled || m_config == null) return;
            bool isGreat = judge == TimingCatchJudgeType.Great;
            if (isGreat)
            {
                m_greatCount++;
                m_consecutiveGreat++;
                m_totalScore += m_config.GetDifficultyGreatScore(m_model.CurrentDifficulty);
                if (m_consecutiveGreat >= m_config.TurnsPerRound)
                {
                    m_totalScore += m_config.ThreeGreatBonus;
                    m_greatBonusCount++;
                }
            }
            else
            {
                m_missCount++;
                m_consecutiveGreat = 0;
            }

            OnJudgeEvaluated?.Invoke(isGreat ? TimingCatchJudgeType.Great : TimingCatchJudgeType.Miss);
            TimingCatchDifficulty completedDifficulty = m_model.CurrentDifficulty;
            m_model.AdvanceToNextTurn();
            bool roundEnded = completedDifficulty == TimingCatchDifficulty.Hard;
            if (m_model.IsFinished)
            {
                m_pendingResult = true;
                m_intermissionRemaining = m_config.HardIntermissionSeconds;
            }
            else
            {
                if (roundEnded) m_consecutiveGreat = 0;
                m_intermissionRemaining = roundEnded
                    ? m_config.HardIntermissionSeconds
                    : m_config.EasyNormalIntermissionSeconds;
            }
            PushState();
            if (m_intermissionRemaining <= 0f && m_pendingResult) PublishResult();
        }

        private void PublishResult()
        {
            if (m_resultPublished || m_config == null) return;
            m_resultPublished = true;
            m_pendingResult = false;
            OnGameResult?.Invoke(new TimingCatchGameResultDTO
            {
                TotalScore = m_totalScore,
                MaxPossibleScore = m_config.MaxPossibleScore,
                GreatCount = m_greatCount,
                MissCount = m_missCount,
                GreatBonusCount = m_greatBonusCount,
                MinigameGrade = ConvertToGrade(m_totalScore, m_config.MaxPossibleScore),
                MinigameId = "TimingCatch",
                PlayTimeSeconds = m_playTime,
            });
        }

        private MinigameGrade ConvertToGrade(int score, int maxScore)
        {
            if (maxScore <= 0) return MinigameGrade.F;
            float ratio = (float)score / maxScore;
            if (ratio >= m_config.GradeAThresholdRatio) return MinigameGrade.A;
            if (ratio >= m_config.GradeBThresholdRatio) return MinigameGrade.B;
            if (ratio >= m_config.GradeCThresholdRatio) return MinigameGrade.C;
            if (ratio >= m_config.GradeDThresholdRatio) return MinigameGrade.D;
            return MinigameGrade.F;
        }

        private void PushState()
        {
            if (m_model == null || m_config == null) return;
            bool isFinished = m_model.IsFinished;
            int displayRound = isFinished ? m_config.RoundCount : m_model.CurrentRoundNumber;
            int displayTurnInRound = isFinished ? m_config.TurnsPerRound : m_model.CurrentTurnNumber;
            int displayTurnTotal = isFinished ? m_config.TotalTurnCount : Math.Min(m_model.CurrentTurn + 1, m_config.TotalTurnCount);
            TimingCatchDifficulty displayDifficulty = isFinished ? TimingCatchDifficulty.Hard : m_model.CurrentDifficulty;
            float displayGreatWidth = isFinished ? m_config.CreateDifficultyGreatZoneWidthsSnapshot()[(int)TimingCatchDifficulty.Hard] : m_model.CurrentGreatZoneWidth;
            var state = new TimingCatchGameState
            {
                Gauge = m_model.GaugeNormalized,
                GreatZoneWidth = displayGreatWidth,
                CurrentRound = displayRound,
                CurrentTurn = displayTurnInRound,
                CurrentTurnTotal = displayTurnTotal,
                Round = displayRound,
                Turn = displayTurnInRound,
                TurnInRound = displayTurnInRound,
                TotalTurn = displayTurnTotal,
                Difficulty = displayDifficulty,
                Score = m_totalScore,
                GreatCount = m_greatCount,
                MissCount = m_missCount,
                ConsecutiveGreat = m_consecutiveGreat,
                GreatBonusCount = m_greatBonusCount,
                IsRunning = m_model.IsRunning,
                IsIntermission = IsIntermission,
                IsFinished = m_model.IsFinished && !m_pendingResult,
                InputEnabled = InputEnabled,
                IntermissionRemaining = m_intermissionRemaining,
                TurnElapsed = m_model.TurnElapsed,
                TurnTimeout = m_model.TurnTimeoutSeconds,
                GreatScore = m_config.GetDifficultyGreatScore(displayDifficulty),
            };
            OnStateChanged?.Invoke(state);
        }
    }
}
