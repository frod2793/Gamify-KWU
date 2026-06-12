using UnityEngine;
using GameArifiction.Player;

namespace GameArifiction.UI.FinalResult
{
    /// <summary>
    /// [기능]: 최종 결과 팝업을 임의 등급 데이터로 강제 호출하여 검증할 수 있는 디버그용 테스트 트리거 컴포넌트입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class FinalResultPopupTestTrigger : MonoBehaviour
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

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 테스트용 버튼의 OnClick 이벤트 등에서 연동하여 호출할 수 있는 트리거 커맨드입니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 최초 구현
        /// </summary>
        public void func_OnTestTriggerClick()
        {
            if (m_targetPopupView != null)
            {
                m_targetPopupView.ShowPopup(m_testGrade, m_testMessage);
                Debug.Log($"[FinalResultPopupTestTrigger] 디버그 버튼 트리거 -> 테스트 팝업 연출 구동 (학점: {m_testGrade})");
            }
            else
            {
                Debug.LogError("[FinalResultPopupTestTrigger] 대상 FinalResultPopupView가 인스펙터 상에 할당되지 않았습니다.");
            }
        }

        /// <summary>
        /// [기능]: 엔딩 테스트용 버튼의 OnClick 이벤트 등에서 연동하여 엔딩 연출을 트리거하는 디버그 커맨드입니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 엔딩 연출 테스트 트리거 신규 추가
        /// </summary>
        public void func_OnEndingTestTriggerClick()
        {
            if (m_targetEndingView != null)
            {
                m_targetEndingView.func_TestPlayEndingSequence();
                Debug.Log("[FinalResultPopupTestTrigger] 디버그 버튼 트리거 -> 엔딩 시퀀스 연출 강제 구동");
            }
            else
            {
                Debug.LogError("[FinalResultPopupTestTrigger] 대상 GameEndingView가 인스펙터 상에 할당되지 않았습니다.");
            }
        }

        #endregion

        #else

        #region 유니티 생명주기 (Unity Lifecycle)

        /// <summary>
        /// [기능]: 릴리즈 빌드 환경에서는 보안 및 무결성을 유지하기 위해 인게임 디버그 오버레이/트리거 오브젝트를 씬에서 완전히 자동 파괴합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 이력 추가
        /// </summary>
        private void Awake()
        {
            Debug.Log("[FinalResultPopupTestTrigger] 릴리즈 빌드 환경이 감지되어 디버그 트리거 컴포넌트 및 게임 오브젝트를 완전히 소멸시킵니다.");
            Destroy(gameObject);
        }

        #endregion

        #endif
    }
}
