using UnityEngine;
using GamifyKWU.UI.Title;

namespace GameArifiction.Interaction
{
    /// <summary>
    /// [기능]: 게임 내 상호작용 UI 및 표지판 UI 등 서브 UI들을 총괄 제어하는 최상위 UI 매니저 클래스
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-28
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 인트로 연출 동안 상호작용 UI(m_interactionUI)가 노출되지 않도록 제어하는 비활성화/활성화 인터페이스 추가
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region UI 참조
        [Header("서브 UI 관리")]
        [SerializeField]
        [Tooltip("상호작용 조작 버튼 UI 뷰입니다.")]
        private InteractionUI_View m_interactionUI;

        [SerializeField]
        [Tooltip("표지판 상세 내용 출력 UI 뷰입니다.")]
        private SignboardUI_View m_signboardUI;

        [SerializeField]
        [Tooltip("타이틀 화면 및 패널 제어 뷰입니다.")]
        private TitleView m_titleView;
        #endregion

        #region 내부 필드 (Private Fields)
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        #endregion

        #region 초기화 (Initialization)
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [기능]: 상호작용 UI의 강제 차단/활성화 상태를 외부(인트로 제어기 등)에서 세팅합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void SetInteractionUIActive(bool isActive)
        {
            if (m_interactionUI != null)
            {
                m_interactionUI.gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// [기능]: 표지판 상세 팝업창을 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        /// <param name="content">표시할 표지판 본문 내용</param>
        public void ShowSignboard(string content)
        {
            if (m_signboardUI != null)
            {
                m_signboardUI.ShowSignboard(content);
            }
        }

        /// <summary>
        /// [기능]: 표지판 상세 팝업창을 닫습니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void HideSignboard()
        {
            if (m_signboardUI != null)
            {
                m_signboardUI.func_CloseSignboardPanel();
            }
        }
        #endregion
    }
}
