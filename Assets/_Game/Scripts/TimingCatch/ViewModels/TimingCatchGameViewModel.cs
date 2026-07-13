using System;
using GameArifiction.Player;
using VContainer;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 게임 Model/판정/스코어를 제어하고 상태 이벤트를 발행하는 뷰모델.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchGameViewModel : ITimingCatchJudgeEventSource
    {
        #region 내부 필드 (Private Fields)
        private readonly TimingCatchGameModel m_model;
        private readonly ITimingJudgeCalculator m_judgeCalculator;
        private readonly TimingCatchGameConfigSO m_config;

        private float m_playTime;
        private int m_totalScore;
        private int m_perfectCount;
        private int m_goodCount;
        private int m_missCount;
        #endregion

        #region 이벤트 (Events)
        public event Action<TimingCatchGameState> OnStateChanged;
        public event Action<TimingCatchJudgeType> OnJudgeEvaluated;
        public event Action<TimingCatchGameResultDTO> OnGameResult;
        #endregion

        #region 내부 속성 (Internal Properties)
        private bool IsRunning => m_model != null && m_model.IsRunning;
        private bool IsComplete => m_model == null || m_model.IsFinished;
        #endregion

        #region 생성자 (Constructor)
        [Inject]
        public TimingCatchGameViewModel(
            TimingCatchGameModel model,
            ITimingJudgeCalculator judgeCalculator,
            TimingCatchGameConfigSO config)
        {
            m_model = model;
            m_judgeCalculator = judgeCalculator;
            m_config = config;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 게임 시작 시점에 모델/카운터를 초기화하고 상태 이벤트를 동기화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 시작과 함께 초깃값 이벤트 송신 추가.
        /// </summary>
        public void StartGame()
        {
            if (m_model == null || m_config == null)
            {
                return;
            }

            m_model.ResetState();
            m_totalScore = 0;
            m_perfectCount = 0;
            m_goodCount = 0;
            m_missCount = 0;
            m_playTime = 0f;

            PushState();
        }

        /// <summary>
        /// [기능]: 프레임별 게이지 갱신을 수행하고 타임아웃 판정 처리.
        /// [작성자]: 윤승종
        /// </summary>
        public void UpdateTick(float deltaTime)
        {
            if (IsRunning == false || IsComplete)
            {
                return;
            }

            m_model.UpdateGauge(deltaTime);
            m_playTime += deltaTime;
            PushState();

            if (m_model.IsStageTimeout)
            {
                ApplyJudge(TimingCatchJudgeType.Miss);
            }
        }

        /// <summary>
        /// [기능]: 플레이어 입력을 받아 현재 스테이지 판정 로직을 실행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void EvaluateInput()
        {
            if (IsRunning == false || IsComplete || m_model == null)
            {
                return;
            }

            TimingCatchJudgeType judge = m_judgeCalculator.Evaluate(
                m_model.GaugeNormalized,
                m_model.CurrentPerfectWindow,
                m_model.CurrentGoodWindow
            );

            ApplyJudge(judge);
        }

        /// <summary>
        /// [기능]: 현재 상태를 강제 전파합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void NotifyState()
        {
            PushState();
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        private void ApplyJudge(TimingCatchJudgeType judge)
        {
            if (m_model == null || m_config == null)
            {
                return;
            }

            TimingCatchJudgeType evaluatedJudge = judge;
            if (m_config.UseTwoJudgeLevels && evaluatedJudge == TimingCatchJudgeType.Miss)
            {
                evaluatedJudge = TimingCatchJudgeType.Good;
            }

            if (evaluatedJudge == TimingCatchJudgeType.Perfect)
            {
                m_totalScore += m_config.PerfectScore;
                m_perfectCount++;
            }
            else if (evaluatedJudge == TimingCatchJudgeType.Good)
            {
                m_totalScore += m_config.GoodScore;
                m_goodCount++;
            }
            else
            {
                m_missCount++;
            }

            OnJudgeEvaluated?.Invoke(evaluatedJudge);

            m_model.AdvanceToNextStage();
            PushState();

            if (m_model.IsFinished)
            {
                PublishResult();
            }
        }

        private void PublishResult()
        {
            if (m_model == null || m_config == null)
            {
                return;
            }

            var result = new TimingCatchGameResultDTO
            {
                TotalScore = m_totalScore,
                MaxPossibleScore = m_config.MaxPossibleScore,
                PerfectCount = m_perfectCount,
                GoodCount = m_goodCount,
                MissCount = m_missCount,
                MinigameGrade = ConvertToGrade(m_totalScore, m_config.MaxPossibleScore),
                MinigameId = "TimingCatch",
                PlayTimeSeconds = m_playTime,
                StageTimeoutSeconds = m_config.StageTimeoutSeconds
            };

            OnGameResult?.Invoke(result);
        }

        private GameArifiction.Player.MinigameGrade ConvertToGrade(int score, int maxScore)
        {
            if (maxScore <= 0)
            {
                return GameArifiction.Player.MinigameGrade.F;
            }

            float ratio = (float)score / (float)maxScore;
            if (ratio >= m_config.GradeAThresholdRatio)
            {
                return GameArifiction.Player.MinigameGrade.A;
            }
            if (ratio >= m_config.GradeBThresholdRatio)
            {
                return GameArifiction.Player.MinigameGrade.B;
            }
            if (ratio >= m_config.GradeCThresholdRatio)
            {
                return GameArifiction.Player.MinigameGrade.C;
            }
            if (ratio >= m_config.GradeDThresholdRatio)
            {
                return GameArifiction.Player.MinigameGrade.D;
            }

            return GameArifiction.Player.MinigameGrade.F;
        }

        private void PushState()
        {
            if (m_model == null || m_config == null)
            {
                return;
            }

            TimingCatchGameState state = new TimingCatchGameState();
            state.Gauge = m_model.GaugeNormalized;
            state.PerfectWindow = m_model.CurrentPerfectWindow;
            state.GoodWindow = m_model.CurrentGoodWindow;
            state.CurrentStage = m_model.CurrentStage;
            state.MaxStage = m_model.MaxStageCount;
            state.Score = m_totalScore;
            state.PerfectCount = m_perfectCount;
            state.GoodCount = m_goodCount;
            state.MissCount = m_missCount;
            state.IsRunning = m_model.IsRunning;
            state.IsFinished = m_model.IsFinished;
            state.StageElapsed = m_model.StageElapsed;
            state.StageTimeout = m_model.StageTimeoutSeconds;
            state.PerfectScore = m_config.PerfectScore;
            state.GoodScore = m_config.GoodScore;

            OnStateChanged?.Invoke(state);
        }
        #endregion
    }
}
