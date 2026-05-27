using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace GameArifiction.Interaction
{
    /// <summary>
    /// [기능]: 표지판 확장 패널 대화창의 활성화 및 텍스트 데이터 바인딩을 담당하는 뷰 클래스
    /// [작성자]: 윤승종
    /// </summary>
    public class SignboardUI_View : MonoBehaviour
    {
        #region UI 참조
        [Header("안내판 패널 UI 설정")]
        [SerializeField]
        [Tooltip("안내판/표지판 전용 확장 UI 패널 오브젝트입니다.")]
        private GameObject m_signboardPanel;

        [SerializeField]
        [Tooltip("안내판 확장 패널 내 텍스트 컴포넌트입니다.")]
        private TMP_Text m_signboardContentText;

        [SerializeField]
        [Tooltip("안내판 확장 패널을 닫는 버튼 컴포넌트입니다.")]
        private Button m_closeSignboardButton;
        #endregion

        #region 유니티 생명주기
        private void Start()
        {
            if (m_closeSignboardButton != null)
            {
                m_closeSignboardButton.onClick.AddListener(func_CloseSignboardPanel);
            }

            if (m_signboardPanel != null)
            {
                m_signboardPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (m_closeSignboardButton != null)
            {
                m_closeSignboardButton.onClick.RemoveListener(func_CloseSignboardPanel);
            }
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [기능]: 표지판 확장 패널 대화창을 열고 텍스트를 기입합니다.
        /// [작성자]: 윤승종
        /// </summary>
        /// <param name="content">표지판 안내 판서 본문 내용</param>
        public void ShowSignboard(string content)
        {
            if (m_signboardPanel != null)
            {
                m_signboardPanel.SetActive(true);
            }

            if (m_signboardContentText != null)
            {
                m_signboardContentText.text = content;
            }
        }

        /// <summary>
        /// [기능]: 표지판 확장 패널 대화창을 닫습니다. (Close 버튼 연계 및 func_ 규칙 준수)
        /// [작성자]: 윤승종
        /// </summary>
        public void func_CloseSignboardPanel()
        {
            if (m_signboardPanel != null)
            {
                m_signboardPanel.SetActive(false);
            }
        }
        #endregion
    }
}
