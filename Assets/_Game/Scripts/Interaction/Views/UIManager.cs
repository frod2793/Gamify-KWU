using UnityEngine;

namespace GameArifiction.Interaction
{
    /// <summary>
    /// [기능]: 게임 내 상호작용 UI 및 표지판 UI 등 서브 UI들을 총괄 제어하는 최상위 UI 매니저 클래스
    /// [작성자]: 윤승종
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
        #endregion

        #region 공개 메서드
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
