using System;
using GameArifiction.Core.Audio;
using GameArifiction.Player;
using VContainer;

namespace GameArifiction.TimingCatch
{
    public sealed class TimingCatchGameViewModel : ITimingCatchJudgeEventSource
    {
        private readonly TimingCatchGameModel m_model;
        private readonly ITimingJudgeCalculator m_judgeCalculator;
        private readonly TimingCatchGameConfigSO m_config;
        private readonly ISoundService m_soundService;
        private float m_playTime;
        private float m_phaseRemaining;
        private int m_totalScore;
        private int m_greatCount;
        private int m_missCount;
        private int m_consecutiveGreat;
        private int m_greatBonusCount;
        private int m_dialogueIndex;
        private int m_displayRound;
        private int m_displayTurn;
        private int m_displayTurnTotal;
        private TimingCatchDifficulty m_displayDifficulty;
        private TimingCatchPhase m_phase;
        private TimingCatchJudgeType m_judgeType;
        private string m_dialogue = string.Empty;
        private int m_bonusScore;
        private bool m_resultPublished;

        public event Action<TimingCatchGameState> OnStateChanged;
        public event Action<TimingCatchJudgeType> OnJudgeEvaluated;
        public event Action<TimingCatchGameResultDTO> OnGameResult;

        public int Score => m_totalScore;
        public int GreatCount => m_greatCount;
        public int MissCount => m_missCount;
        public int ConsecutiveGreat => m_consecutiveGreat;
        public int GreatBonusCount => m_greatBonusCount;
        public TimingCatchPhase Phase => m_phase;
        public bool IsIntermission => m_phase == TimingCatchPhase.TurnInterval || m_phase == TimingCatchPhase.RoundInterval;
        public float IntermissionRemaining => IsIntermission ? m_phaseRemaining : 0f;
        public bool InputEnabled => m_model != null && m_model.IsRunning && m_phase == TimingCatchPhase.Playing;

        [Inject]
        public TimingCatchGameViewModel(
            TimingCatchGameModel model,
            ITimingJudgeCalculator judgeCalculator,
            TimingCatchGameConfigSO config,
            ISoundService soundService = null)
        {
            m_model = model;
            m_judgeCalculator = judgeCalculator;
            m_config = config;
            m_soundService = soundService;
        }

        public void StartGame()
        {
            if (m_model == null || m_config == null) return;

            m_model.ResetState();
            m_playTime = 0f;
            m_phaseRemaining = m_config.IntroLineDuration;
            m_totalScore = 0;
            m_greatCount = 0;
            m_missCount = 0;
            m_consecutiveGreat = 0;
            m_greatBonusCount = 0;
            m_dialogueIndex = 0;
            m_judgeType = TimingCatchJudgeType.None;
            m_bonusScore = 0;
            m_resultPublished = false;
            SetDisplayFromCurrentTurn();
            m_phase = TimingCatchPhase.Intro;
            m_dialogue = m_config.GetIntroDialogue(m_dialogueIndex);
            PushState();
        }

        public void UpdateTick(float deltaTime)
        {
            if (deltaTime <= 0f || m_model == null || m_config == null) return;

            if (m_phase == TimingCatchPhase.Playing)
            {
                m_model.UpdateGauge(deltaTime);
                m_playTime += deltaTime;
                PushState();
                if (m_model.IsTurnTimeout) ApplyJudge(TimingCatchJudgeType.Miss);
                return;
            }

            if (m_phase == TimingCatchPhase.Completed || m_phase == TimingCatchPhase.None) return;
            m_phaseRemaining = Math.Max(0f, m_phaseRemaining - deltaTime);
            if (m_phaseRemaining <= 0f) AdvancePhase();
            PushState();
        }

        public void EvaluateInput()
        {
            if (!InputEnabled || m_judgeCalculator == null) return;
            TimingCatchJudgeType judge = m_judgeCalculator.Evaluate(
                m_model.GaugeNormalized,
                m_model.CurrentGreatZoneHalfWidth,
                0f);
            ApplyJudge(judge);
        }

        public void NotifyState()
        {
            PushState();
        }

        private void ApplyJudge(TimingCatchJudgeType judge)
        {
            if (!InputEnabled) return;

            SetDisplayFromCurrentTurn();
            bool isGreat = judge == TimingCatchJudgeType.Great;
            m_judgeType = isGreat ? TimingCatchJudgeType.Great : TimingCatchJudgeType.Miss;
            m_bonusScore = 0;
            if (isGreat)
            {
                m_greatCount++;
                m_consecutiveGreat++;
                m_totalScore += m_config.GetDifficultyGreatScore(m_displayDifficulty);
                m_dialogue = m_config.GetGreatDialogue(m_displayRound - 1, m_displayTurn - 1);
                if (m_consecutiveGreat == m_config.TurnsPerRound)
                {
                    m_bonusScore = m_config.ThreeGreatBonus;
                    m_totalScore += m_bonusScore;
                    m_greatBonusCount++;
                }

                if (!string.IsNullOrWhiteSpace(m_config.GreatSfxPath) && m_soundService != null)
                {
                    m_soundService.PlaySFX(m_config.GreatSfxPath);
                }
            }
            else
            {
                m_missCount++;
                m_consecutiveGreat = 0;
                m_dialogue = m_config.MissDialogue;
            }

            OnJudgeEvaluated?.Invoke(m_judgeType);
            m_model.AdvanceToNextTurn();
            m_phase = TimingCatchPhase.JudgeResult;
            bool roundEnded = m_displayDifficulty == TimingCatchDifficulty.Hard;
            m_phaseRemaining = roundEnded ? m_config.HardIntermissionSeconds : m_config.EasyNormalIntermissionSeconds;
            PushState();
        }

        private void AdvancePhase()
        {
            switch (m_phase)
            {
                case TimingCatchPhase.Intro:
                    AdvanceIntro();
                    break;
                case TimingCatchPhase.RoundStart:
                    BeginPlaying();
                    break;
                case TimingCatchPhase.JudgeResult:
                    AdvanceJudgeResult();
                    break;
                case TimingCatchPhase.Outro:
                    AdvanceOutro();
                    break;
            }
        }

        private void AdvanceIntro()
        {
            m_dialogueIndex++;
            string nextDialogue = m_config.GetIntroDialogue(m_dialogueIndex);
            if (!string.IsNullOrEmpty(nextDialogue))
            {
                m_dialogue = nextDialogue;
                m_phaseRemaining = m_config.IntroLineDuration;
                return;
            }

            m_phase = TimingCatchPhase.RoundStart;
            m_phaseRemaining = m_config.RoundStartDuration;
            m_dialogue = string.Empty;
        }

        private void AdvanceJudgeResult()
        {
            if (m_model.IsFinished)
            {
                BeginOutro();
                return;
            }

            if (m_displayDifficulty == TimingCatchDifficulty.Hard)
            {
                m_consecutiveGreat = 0;
            }

            BeginPlaying();
        }

        private void BeginPlaying()
        {
            m_phase = TimingCatchPhase.Playing;
            m_phaseRemaining = 0f;
            m_dialogue = string.Empty;
            m_judgeType = TimingCatchJudgeType.None;
            m_bonusScore = 0;
            SetDisplayFromCurrentTurn();
        }

        private void BeginOutro()
        {
            m_phase = TimingCatchPhase.Outro;
            m_phaseRemaining = m_config.OutroLineDuration;
            m_dialogueIndex = 0;
            m_dialogue = m_config.GetOutroDialogue(m_dialogueIndex);
            m_judgeType = TimingCatchJudgeType.None;
            m_bonusScore = 0;
        }

        private void AdvanceOutro()
        {
            m_dialogueIndex++;
            string nextDialogue = m_config.GetOutroDialogue(m_dialogueIndex);
            if (!string.IsNullOrEmpty(nextDialogue))
            {
                m_dialogue = nextDialogue;
                m_phaseRemaining = m_config.OutroLineDuration;
                return;
            }

            m_phase = TimingCatchPhase.Completed;
            m_phaseRemaining = 0f;
            m_dialogue = string.Empty;
            PublishResult();
        }

        private void SetDisplayFromCurrentTurn()
        {
            if (m_model == null || m_config == null) return;
            m_displayRound = Math.Min(m_model.CurrentRoundNumber, m_config.RoundCount);
            m_displayTurn = Math.Min(m_model.CurrentTurnNumber, m_config.TurnsPerRound);
            m_displayTurnTotal = Math.Min(m_model.CurrentTurn + 1, m_config.TotalTurnCount);
            m_displayDifficulty = m_model.CurrentDifficulty;
        }

        private void PublishResult()
        {
            if (m_resultPublished) return;
            m_resultPublished = true;
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
            var state = new TimingCatchGameState
            {
                Gauge = m_model.GaugeNormalized,
                GreatZoneWidth = m_config.GetGreatZoneWidth(m_displayDifficulty),
                CurrentRound = m_displayRound,
                CurrentTurn = m_displayTurn,
                CurrentTurnTotal = m_displayTurnTotal,
                TotalTurnCount = m_config.TotalTurnCount,
                Round = m_displayRound,
                Turn = m_displayTurn,
                TurnInRound = m_displayTurn,
                TotalTurn = m_displayTurnTotal,
                Difficulty = m_displayDifficulty,
                Score = m_totalScore,
                GreatCount = m_greatCount,
                MissCount = m_missCount,
                ConsecutiveGreat = m_consecutiveGreat,
                GreatBonusCount = m_greatBonusCount,
                Phase = m_phase,
                JudgeType = m_judgeType,
                Dialogue = m_dialogue,
                BonusScore = m_bonusScore,
                StarScale = m_config.GetStarScale(m_consecutiveGreat),
                IsRunning = m_phase == TimingCatchPhase.Playing,
                IsIntermission = IsIntermission,
                IsFinished = m_phase == TimingCatchPhase.Completed,
                InputEnabled = InputEnabled,
                IntermissionRemaining = IntermissionRemaining,
                TurnElapsed = m_model.TurnElapsed,
                TurnTimeout = m_model.TurnTimeoutSeconds,
                GreatScore = m_config.GetDifficultyGreatScore(m_displayDifficulty),
            };
            OnStateChanged?.Invoke(state);
        }
    }
}
