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
        [Header("스테이지/속도")]
        [SerializeField]
        [Tooltip("총 스테이지 수(요구사항 기준: 7단계).")]
        private int m_maxStageCount = 7;

        [SerializeField]
        [Tooltip("첫 스테이지 게이지 이동 속도.")]
        private float m_startGaugeSpeed = 1f;

        [SerializeField]
        [Tooltip("스테이지 클리어 시 누적 증가되는 게이지 속도.")]
        private float m_stageSpeedIncrease = 0.2f;

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

        [Header("스테이지 진행/타임아웃")]
        [SerializeField]
        [Tooltip("입력이 없어도 스테이지를 강제 종료하는 제한시간(초). 0 이하면 비활성.")]
        private float m_stageTimeoutSeconds = 5.5f;

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

        [Header("모드")]
        [SerializeField]
        [Tooltip("true 면 Perfect/Good 2단계, false 면 Perfect/Good/Miss 3단계.")]
        private bool m_useTwoJudgeLevels = false;

        public int MaxStageCount
        {
            get
            {
                if (m_maxStageCount < 1)
                {
                    return 1;
                }
                return m_maxStageCount;
            }
        }

        public float StartGaugeSpeed => m_startGaugeSpeed;
        public float StageSpeedIncrease => m_stageSpeedIncrease;

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
    }
}

