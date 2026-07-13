using UnityEngine;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 게임 난이도/점수/등급 기준 설정을 보관하는 ScriptableObject.
    /// [작성자]: 윤승종
    /// </summary>
    [CreateAssetMenu(fileName = "TimingCatchConfigSO", menuName = "Gamify-KWU/TimingCatchConfigSO")]
    public sealed class TimingCatchGameConfigSO : ScriptableObject
    {
        #region 상수 (Constants)
        private const float SafeDefaultGaugeSpeed = 1f;
        #endregion

        #region 스테이지 설정 (Stage Settings)
        [Header("스테이지/속도")]
        [SerializeField]
        [Tooltip("스테이지 순서대로 적용할 게이지 이동 속도. 배열 인덱스 0은 1스테이지입니다.")]
        private float[] m_stageGaugeSpeeds =
        {
            1f,
            1.2f,
            1.4f,
            1.6f,
            1.8f,
            2f,
            2.2f
        };
        #endregion

        #region 판정 설정 (Judge Settings)
        [Header("판정 구간")]
        [SerializeField]
        [Tooltip("Perfect 판정 시작 폭(0~0.5).")]
        private float m_startPerfectWindow = 0.05f;

        [SerializeField]
        [Tooltip("Good 판정 시작 폭(0~0.5).")]
        private float m_startGoodWindow = 0.1f;

        [SerializeField]
        [Tooltip("Perfect 구간이 스테이지 진행마다 감소하는 값.")]
        private float m_perfectWindowDecay = 0.005f;

        [SerializeField]
        [Tooltip("Good 구간이 스테이지 진행마다 감소하는 값.")]
        private float m_goodWindowDecay = 0.008f;

        [SerializeField]
        [Tooltip("Perfect 최소 폭 하한선.")]
        private float m_minPerfectWindow = 0.015f;

        [SerializeField]
        [Tooltip("Good 최소 폭 하한선.")]
        private float m_minGoodWindow = 0.03f;
        #endregion

        #region 진행 설정 (Progress Settings)
        [Header("스테이지 진행/타임아웃")]
        [SerializeField]
        [Tooltip("입력이 없어도 스테이지를 강제 종료하는 제한시간(초). 0 이하면 비활성.")]
        private float m_stageTimeoutSeconds = 5.5f;
        #endregion

        #region 점수 설정 (Score Settings)
        [Header("점수/등급")]
        [SerializeField]
        [Tooltip("Perfect 판정 점수.")]
        private int m_perfectScore = 100;

        [SerializeField]
        [Tooltip("Good 판정 점수.")]
        private int m_goodScore = 60;

        [SerializeField]
        [Tooltip("단일 미니게임에서 A 판정 임계 비율(0~1).")]
        private float m_gradeAThresholdRatio = 0.9f;

        [SerializeField]
        [Tooltip("단일 미니게임에서 B 판정 임계 비율(0~1).")]
        private float m_gradeBThresholdRatio = 0.75f;

        [SerializeField]
        [Tooltip("단일 미니게임에서 C 판정 임계 비율(0~1).")]
        private float m_gradeCThresholdRatio = 0.6f;

        [SerializeField]
        [Tooltip("단일 미니게임에서 D 판정 임계 비율(0~1).")]
        private float m_gradeDThresholdRatio = 0.45f;
        #endregion

        #region 모드 설정 (Mode Settings)
        [Header("모드")]
        [SerializeField]
        [Tooltip("true 면 Perfect/Good 2단계, false 면 Perfect/Good/Miss 3단계.")]
        private bool m_useTwoJudgeLevels = false;
        #endregion

        #region 공개 프로퍼티 (Public Properties)
        public int MaxStageCount
        {
            get
            {
                if (m_stageGaugeSpeeds == null || m_stageGaugeSpeeds.Length == 0)
                {
                    return 1;
                }

                return m_stageGaugeSpeeds.Length;
            }
        }

        public float StartPerfectWindow => m_startPerfectWindow;
        public float StartGoodWindow => m_startGoodWindow;
        public float PerfectWindowDecay => m_perfectWindowDecay;
        public float GoodWindowDecay => m_goodWindowDecay;
        public float MinPerfectWindow => m_minPerfectWindow;
        public float MinGoodWindow => m_minGoodWindow;

        public float StageTimeoutSeconds => m_stageTimeoutSeconds;

        public int PerfectScore => m_perfectScore;
        public int GoodScore => m_goodScore;
        public int MaxPossibleScore => MaxStageCount * PerfectScore;

        public float GradeAThresholdRatio => m_gradeAThresholdRatio;
        public float GradeBThresholdRatio => m_gradeBThresholdRatio;
        public float GradeCThresholdRatio => m_gradeCThresholdRatio;
        public float GradeDThresholdRatio => m_gradeDThresholdRatio;

        public bool UseTwoJudgeLevels => m_useTwoJudgeLevels;
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 모델 초기화용 스테이지 속도 배열 복사본을 생성합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 스테이지별 독립 속도와 빈 배열/음수 값 방어 추가.
        /// </summary>
        public float[] CreateStageGaugeSpeedsSnapshot()
        {
            int stageCount = MaxStageCount;
            var snapshot = new float[stageCount];

            if (m_stageGaugeSpeeds == null || m_stageGaugeSpeeds.Length == 0)
            {
                snapshot[0] = SafeDefaultGaugeSpeed;
                return snapshot;
            }

            for (int i = 0; i < stageCount; i++)
            {
                float speed = m_stageGaugeSpeeds[i];
                if (speed < 0f)
                {
                    speed = 0f;
                }

                snapshot[i] = speed;
            }

            return snapshot;
        }
        #endregion
    }
}
