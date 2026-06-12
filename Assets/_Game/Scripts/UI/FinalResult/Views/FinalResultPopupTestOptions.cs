using UnityEngine;
using GameArifiction.Player;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameArifiction.UI.FinalResult
{
    /// <summary>
    /// [기능]: 최종 결과 팝업, 엔딩 및 키오스크 조건부 테스트를 제어하기 위한 디버그용 테스트 옵션 컴포넌트입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class FinalResultPopupTestOptions : MonoBehaviour
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD

        #region UI 참조 (Inspector)

        [Header("테스트 대상 및 데이터")]
        [SerializeField]
        [Tooltip("테스트를 수행할 최종 결과 팝업 View 컴포넌트입니다.")]
        private FinalResultPopupView m_targetPopupView;

        [SerializeField]
        [Tooltip("테스트를 수행할 엔딩 화면 View 컴포넌트입니다.")]
        private GameEndingView m_targetEndingView;

        [SerializeField]
        [Tooltip("테스트용으로 출력할 미니게임 학점 등급입니다.")]
        private MinigameGrade m_testGrade = MinigameGrade.A;

        [SerializeField]
        [TextArea(3, 6)]
        [Tooltip("테스트용으로 출력할 교수님의 피드백 멘트 내용입니다.")]
        private string m_testMessage = "디버그 모드로 구동되는 결과 팝업 테스트 멘트입니다. 타이핑 연출 및 스킵 기능이 정상 작동합니까?";

        [Header("키오스크 테스트 설정")]
        [SerializeField]
        [Tooltip("활성화/비활성화 테스트를 적용할 로비의 키오스크 컴포넌트입니다.")]
        private FinalResultInteractableView m_targetKiosk;

        [SerializeField]
        [Tooltip("테스트 성적을 강제 주입할 플레이어 데이터 에셋입니다.")]
        private PlayerSO m_playerSO;

        [Header("임의 조건 테스트 옵션")]
        [SerializeField]
        [Tooltip("조건 테스트 시 CardMatch 게임의 클리어 여부입니다.")]
        private bool m_clearCardMatch = true;

        [SerializeField]
        [Tooltip("조건 테스트 시 CraneGame 게임의 클리어 여부입니다.")]
        private bool m_clearCraneGame = true;

        [SerializeField]
        [Tooltip("조건 테스트 시 GradeRunner 게임의 클리어 여부입니다.")]
        private bool m_clearGradeRunner = true;

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 테스트용 팝업 연출을 강제 트리거합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// </summary>
        public void func_OnTestTriggerClick()
        {
            if (m_targetPopupView != null)
            {
                m_targetPopupView.ShowPopup(m_testGrade, m_testMessage);
                Debug.Log($"[FinalResultPopupTestOptions] 디버그 버튼 트리거 -> 테스트 팝업 연출 구동 (학점: {m_testGrade})");
            }
            else
            {
                Debug.LogError("[FinalResultPopupTestOptions] 대상 FinalResultPopupView가 인스펙터 상에 할당되지 않았습니다.");
            }
        }

        /// <summary>
        /// [기능]: 엔딩 시퀀스 연출을 강제 구동합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// </summary>
        public void func_OnEndingTestTriggerClick()
        {
            if (m_targetEndingView != null)
            {
                m_targetEndingView.func_TestPlayEndingSequence();
                Debug.Log("[FinalResultPopupTestOptions] 디버그 버튼 트리거 -> 엔딩 시퀀스 연출 강제 구동");
            }
            else
            {
                Debug.LogError("[FinalResultPopupTestOptions] 대상 GameEndingView가 인스펙터 상에 할당되지 않았습니다.");
            }
        }

        /// <summary>
        /// [기능]: 인스펙터에 설정한 세 개 미니게임의 개별 클리어 설정(true/false) 상태를 PlayerSO에 주입하여 조건별 키오스크 활성화를 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// </summary>
        public void func_OnKioskConditionTestClick()
        {
            if (m_playerSO != null)
            {
                // 먼저 성적 데이터를 초기화하여 정확한 조건 대입을 검증합니다.
                m_playerSO.ResetData();
                
                m_playerSO.SetMinigameGrade("CardMatch", m_clearCardMatch ? MinigameGrade.A : MinigameGrade.None);
                m_playerSO.SetMinigameGrade("CraneGame", m_clearCraneGame ? MinigameGrade.A : MinigameGrade.None);
                m_playerSO.SetMinigameGrade("GradeRunner", m_clearGradeRunner ? MinigameGrade.A : MinigameGrade.None);
                
                Debug.Log($"[FinalResultPopupTestOptions] 디버그 트리거 -> 조건부 성적 강제 주입 완료. (CardMatch: {m_clearCardMatch}, CraneGame: {m_clearCraneGame}, GradeRunner: {m_clearGradeRunner})");

                if (m_targetKiosk != null)
                {
                    m_targetKiosk.RefreshKioskState();
                }
            }
            else
            {
                Debug.LogError("[FinalResultPopupTestOptions] PlayerSO 레퍼런스가 할당되지 않았습니다.");
            }
        }

        /// <summary>
        /// [기능]: 플레이어의 모든 미니게임 성적을 리셋(비활성화 상태)시켜 키오스크를 꺼짐 상태로 강제 복원합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// </summary>
        public void func_OnKioskDeactivateTestClick()
        {
            if (m_playerSO != null)
            {
                m_playerSO.ResetData();
                Debug.Log("[FinalResultPopupTestOptions] 디버그 트리거 -> 플레이어 세션 데이터(성적 포함) 초기화 완료.");

                if (m_targetKiosk != null)
                {
                    m_targetKiosk.RefreshKioskState();
                }
            }
        }

        /// <summary>
        /// [기능]: 플레이어 데이터 에셋(PlayerSO)에 기록된 모든 세션 정보 및 점수, 마지막 좌표 등을 일괄 완전 초기화(Reset)합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// </summary>
        public void func_OnResetPlayerDataClick()
        {
            if (m_playerSO != null)
            {
                m_playerSO.ResetData();
                Debug.Log("[FinalResultPopupTestOptions] 디버그 트리거 -> 플레이어 데이터(세션 및 성적) 전체 초기화 완료.");

                if (m_targetKiosk != null)
                {
                    m_targetKiosk.RefreshKioskState();
                }
            }
            else
            {
                Debug.LogError("[FinalResultPopupTestOptions] PlayerSO 레퍼런스가 할당되지 않았습니다.");
            }
        }

        #endregion

        #else

        #region 유니티 생명주기 (Unity Lifecycle)

        /// <summary>
        /// [기능]: 릴리즈 빌드 환경에서는 보안 및 무결성을 유지하기 위해 인게임 디버그 오브젝트를 씬에서 완전히 자동 파괴합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void Awake()
        {
            Debug.Log("[FinalResultPopupTestOptions] 릴리즈 빌드 환경이 감지되어 디버그 옵션 컴포넌트를 소멸시킵니다.");
            Destroy(gameObject);
        }

        #endregion

        #endif
    }

    #if UNITY_EDITOR
    /// <summary>
    /// [기능]: FinalResultPopupTestOptions 컴포넌트의 디버깅 동작을 인스펙터 버튼으로 직접 실행할 수 있도록 하는 커스텀 에디터 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    [CustomEditor(typeof(FinalResultPopupTestOptions))]
    public class FinalResultPopupTestOptionsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 속성들을 먼저 렌더링합니다.
            DrawDefaultInspector();

            FinalResultPopupTestOptions options = (FinalResultPopupTestOptions)target;

            GUILayout.Space(15);
            GUILayout.Label("인스펙터 디버그 실행 도구", EditorStyles.boldLabel);

            if (GUILayout.Button("결과 팝업 테스트 실행", GUILayout.Height(30)))
            {
                options.func_OnTestTriggerClick();
            }

            if (GUILayout.Button("엔딩 시퀀스 테스트 실행", GUILayout.Height(30)))
            {
                options.func_OnEndingTestTriggerClick();
            }

            if (GUILayout.Button("키오스크 조건 테스트 실행", GUILayout.Height(30)))
            {
                options.func_OnKioskConditionTestClick();
            }

            if (GUILayout.Button("키오스크 성적 리셋 실행", GUILayout.Height(30)))
            {
                options.func_OnKioskDeactivateTestClick();
            }

            if (GUILayout.Button("플레이어 데이터 전체 초기화", GUILayout.Height(30)))
            {
                options.func_OnResetPlayerDataClick();
            }
        }
    }
    #endif
}
