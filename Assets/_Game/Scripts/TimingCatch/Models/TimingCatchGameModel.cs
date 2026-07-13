using System;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 게임 게이지 이동, 스테이지 진행, 난이도 적용을 관리하는 순수 C# 모델.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchGameModel
    {
        #region 내부 필드 (Private Fields)
        private readonly int m_maxStageCount;
        private readonly float[] m_stageGaugeSpeeds;
        private readonly float m_startPerfectWindow;
        private readonly float m_startGoodWindow;
        private readonly float m_perfectWindowDecay;
        private readonly float m_goodWindowDecay;
        private readonly float m_minPerfectWindow;
        private readonly float m_minGoodWindow;
        private readonly float m_stageTimeoutSeconds;

        private int m_currentStage;
        private float m_gaugePosition;
        private float m_currentSpeed;
        private float m_stageElapsed;
        private bool m_isRunning;
        private int m_direction = 1;
        #endregion

        #region 초기화 (Initialization)
        public TimingCatchGameModel(TimingCatchGameConfigSO config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "[TimingCatchGameModel] 설정 데이터가 없습니다.");
            }

            m_stageGaugeSpeeds = config.CreateStageGaugeSpeedsSnapshot();
            m_maxStageCount = m_stageGaugeSpeeds.Length;
            m_startPerfectWindow = config.StartPerfectWindow;
            m_startGoodWindow = config.StartGoodWindow;
            m_perfectWindowDecay = config.PerfectWindowDecay;
            m_goodWindowDecay = config.GoodWindowDecay;
            m_minPerfectWindow = config.MinPerfectWindow;
            m_minGoodWindow = config.MinGoodWindow;
            m_stageTimeoutSeconds = config.StageTimeoutSeconds;

            ResetState();
        }
        #endregion

        #region 프로퍼티 (Properties)
        public int CurrentStage => m_currentStage;
        public int MaxStageCount => m_maxStageCount;
        public float GaugeNormalized => m_gaugePosition;
        public float CurrentSpeed => m_currentSpeed;
        public float StageElapsed => m_stageElapsed;
        public float StageTimeoutSeconds => m_stageTimeoutSeconds;
        public bool IsRunning => m_isRunning;
        public bool IsFinished => m_currentStage >= m_maxStageCount;
        public bool IsStageTimeout => m_stageTimeoutSeconds > 0f && m_stageElapsed >= m_stageTimeoutSeconds;
        public float CurrentPerfectWindow => GetCurrentWindow(
            m_startPerfectWindow,
            m_perfectWindowDecay,
            m_minPerfectWindow
        );
        public float CurrentGoodWindow => GetCurrentWindow(
            m_startGoodWindow,
            m_goodWindowDecay,
            m_minGoodWindow
        );
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 게임 진행 데이터를 초기화하고 시작 상태로 진입합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 게이지 정렬값을 중앙 기준 0.5로 리셋하고 방향 반전 플래그 초기화.
        /// </summary>
        public void ResetState()
        {
            m_currentStage = 0;
            m_gaugePosition = 0.5f;
            m_currentSpeed = GetStageGaugeSpeed(m_currentStage);
            m_stageElapsed = 0f;
            m_isRunning = true;
            m_direction = 1;
        }

        /// <summary>
        /// [기능]: 프레임 단위 시간으로 게이지 위치를 갱신합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 좌우 경계에서 방향 반전 처리를 강화.
        /// </summary>
        public void UpdateGauge(float deltaTime)
        {
            if (m_isRunning == false)
            {
                return;
            }

            m_stageElapsed += deltaTime;
            m_gaugePosition += m_direction * m_currentSpeed * deltaTime;

            if (m_gaugePosition <= 0f)
            {
                m_gaugePosition = 0f;
                m_direction = 1;
            }
            else if (m_gaugePosition >= 1f)
            {
                m_gaugePosition = 1f;
                m_direction = -1;
            }
        }

        /// <summary>
        /// [기능]: 현재 스테이지 판정을 완료하고 다음 스테이지로 이동합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 스테이지 전환 시 게이지 속도/경과시간 갱신.
        /// </summary>
        public void AdvanceToNextStage()
        {
            m_currentStage++;
            m_stageElapsed = 0f;

            if (m_currentStage >= m_maxStageCount)
            {
                m_isRunning = false;
                return;
            }

            m_currentSpeed = GetStageGaugeSpeed(m_currentStage);
            m_gaugePosition = 0.5f;
            m_direction = 1;
        }

        /// <summary>
        /// [기능]: 현재 스테이지에서 진행 중인 판정 임계치 쌍을 반환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 최소 폭 보장 로직 명시.
        /// </summary>
        public float GetCurrentWindow(float startWindow, float decay, float minWindow)
        {
            float target = startWindow - (decay * m_currentStage);
            if (target < minWindow)
            {
                target = minWindow;
            }
            if (target > 0.5f)
            {
                target = 0.5f;
            }
            if (target < 0f)
            {
                target = 0f;
            }
            return target;
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 요청한 스테이지 인덱스에 대응하는 안전한 게이지 속도를 반환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 스테이지별 독립 속도 조회와 배열 범위 방어 추가.
        /// </summary>
        private float GetStageGaugeSpeed(int stageIndex)
        {
            if (m_stageGaugeSpeeds == null || m_stageGaugeSpeeds.Length == 0)
            {
                return 1f;
            }

            int safeIndex = stageIndex;
            if (safeIndex < 0)
            {
                safeIndex = 0;
            }
            else if (safeIndex >= m_stageGaugeSpeeds.Length)
            {
                safeIndex = m_stageGaugeSpeeds.Length - 1;
            }

            return m_stageGaugeSpeeds[safeIndex];
        }
        #endregion
    }
}
