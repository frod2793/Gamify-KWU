using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)의 속도, 스폰 주기, 보너스/패널티 등 주요 밸런스 설정치를 총괄하는 ScriptableObject
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    [CreateAssetMenu(fileName = "GradeRunnerConfigSO", menuName = "Gamify-KWU/GradeRunnerConfigSO")]
    public class GradeRunnerConfigSO : ScriptableObject
    {
        #region 내부 필드 (Private Fields)

        [Header("게임 제한 시간")]
        [SerializeField]
        [Tooltip("미니게임 전체 제한시간(초)입니다. 기본값 30초.")]
        private float m_gameDuration = 30f;

        [Header("페이즈 2 전환 시간")]
        [SerializeField]
        [Tooltip("2페이즈(남은시간 10초 이하) 전환 시점(초)입니다. 기본값 10초.")]
        private float m_phase2TransitionTime = 10f;

        [Header("학점 밸런스")]
        [SerializeField]
        [Tooltip("학점의 최대 상한선입니다. 기본값 5.0점.")]
        private float m_maxGradePoint = 5f;

        [SerializeField]
        [Tooltip("미니게임 시작 시 주어지는 기본 학점입니다. 기본값 2.5점.")]
        private float m_startGradePoint = 2.5f;

        [SerializeField]
        [Tooltip("코드(장애물) 충돌 시 감점되는 학점입니다. 기본값 0.5점.")]
        private float m_codePenalty = 0.5f;

        [SerializeField]
        [Tooltip("족보(아이템) 획득 시 가점되는 학점입니다. 기본값 1.0점.")]
        private float m_cheatSheetBonus = 1.0f;

        [Header("코드(장애물) 1페이즈 스폰 주기")]
        [SerializeField]
        [Tooltip("1페이즈(30초~10초) 동안의 코드 투하 최소 간격입니다. 기본값 0.5초.")]
        private float m_codeSpawnIntervalMinP1 = 0.5f;
        [SerializeField]
        [Tooltip("1페이즈(30초~10초) 동안의 코드 투하 최대 간격입니다. 기본값 0.7초.")]
        private float m_codeSpawnIntervalMaxP1 = 0.7f;

        [Header("코드(장애물) 2페이즈 스폰 주기")]
        [SerializeField]
        [Tooltip("2페이즈(10초 이하) 동안의 코드 투하 최소 간격입니다. 기본값 0.3초.")]
        private float m_codeSpawnIntervalMinP2 = 0.3f;
        [SerializeField]
        [Tooltip("2페이즈(10초 이하) 동안의 코드 투하 최대 간격입니다. 기본값 0.5초.")]
        private float m_codeSpawnIntervalMaxP2 = 0.5f;

        [Header("낙하 소요 시간")]
        [SerializeField]
        [Tooltip("오브젝트가 생성되어 바닥까지 낙하하는데 걸리는 시간(초)의 최소 범위입니다.")]
        private float m_fallDurationMin = 6f;
        [SerializeField]
        [Tooltip("오브젝트가 생성되어 바닥까지 낙하하는데 걸리는 시간(초)의 최대 범위입니다.")]
        private float m_fallDurationMax = 8f;

        [Header("플레이어 기동력")]
        [SerializeField]
        [Tooltip("플레이어가 화면 좌우 끝을 편도로 이동하는 데 걸리는 최소 시간(초)입니다.")]
        private float m_playerTraverseDurationMin = 4f;
        [SerializeField]
        [Tooltip("플레이어가 화면 좌우 끝을 편도로 이동하는 데 걸리는 최대 시간(초)입니다.")]
        private float m_playerTraverseDurationMax = 5f;

        [Header("기타 설정")]
        [SerializeField]
        [Tooltip("피드백 텍스트(+1.0 / -0.5)가 화면에 머무르는 시간(초)입니다.")]
        private float m_feedbackDisplayDuration = 1.0f;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public float GameDuration => m_gameDuration;
        public float Phase2TransitionTime => m_phase2TransitionTime;
        public float MaxGradePoint => m_maxGradePoint;
        public float StartGradePoint => m_startGradePoint;
        public float CodePenalty => m_codePenalty;
        public float CheatSheetBonus => m_cheatSheetBonus;

        public float CodeSpawnIntervalMinP1 => m_codeSpawnIntervalMinP1;
        public float CodeSpawnIntervalMaxP1 => m_codeSpawnIntervalMaxP1;

        public float CodeSpawnIntervalMinP2 => m_codeSpawnIntervalMinP2;
        public float CodeSpawnIntervalMaxP2 => m_codeSpawnIntervalMaxP2;

        public float FallDurationMin => m_fallDurationMin;
        public float FallDurationMax => m_fallDurationMax;

        public float PlayerTraverseDurationMin => m_playerTraverseDurationMin;
        public float PlayerTraverseDurationMax => m_playerTraverseDurationMax;

        public float FeedbackDisplayDuration => m_feedbackDisplayDuration;

        #endregion
    }
}
