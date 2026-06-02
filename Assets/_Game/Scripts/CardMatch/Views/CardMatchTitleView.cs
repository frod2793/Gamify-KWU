using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 초화면(타이틀)의 UI를 관리하는 View 클래스입니다.
    ///         게임 시작 버튼, 게임 방법 팝업 등의 상호작용을 처리합니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardMatchTitleView : MonoBehaviour
    {
        #region SerializeField
        [Header("타이틀 화면 요소")]
        [SerializeField] private GameObject m_titlePanel;
        [SerializeField] private Button m_startButton;
        [SerializeField] private Button m_howToPlayButton;

        [Header("게임 방법 팝업")]
        [SerializeField] private GameObject m_howToPlayPopup;
        [SerializeField] private Button m_closePopupButton;
        #endregion

        #region Private Fields
        private Action m_onGameStart;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            if (m_startButton != null)
            {
                m_startButton.onClick.AddListener(func_OnStartButtonClick);
            }
            if (m_howToPlayButton != null)
            {
                m_howToPlayButton.onClick.AddListener(func_OnHowToPlayButtonClick);
            }
            if (m_closePopupButton != null)
            {
                m_closePopupButton.onClick.AddListener(func_OnClosePopupButtonClick);
            }
        }

        private void OnDestroy()
        {
            if (m_startButton != null)
            {
                m_startButton.onClick.RemoveListener(func_OnStartButtonClick);
            }
            if (m_howToPlayButton != null)
            {
                m_howToPlayButton.onClick.RemoveListener(func_OnHowToPlayButtonClick);
            }
            if (m_closePopupButton != null)
            {
                m_closePopupButton.onClick.RemoveListener(func_OnClosePopupButtonClick);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// [기능]: 타이틀 뷰를 초기화합니다. 게임 시작 콜백을 등록합니다.
        /// [작성자]: 김지연
        /// </summary>
        /// <param name="onGameStart">게임 시작 시 호출될 콜백</param>
        public void Initialize(Action onGameStart)
        {
            m_onGameStart = onGameStart;
            Show();

            if (m_howToPlayPopup != null)
            {
                m_howToPlayPopup.SetActive(false);
            }

            Debug.Log("[CardMatchTitleView] 타이틀 화면 초기화 완료");
        }

        /// <summary>
        /// [기능]: 타이틀 패널을 표시합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void Show()
        {
            if (m_titlePanel != null)
            {
                m_titlePanel.SetActive(true);
            }
        }

        /// <summary>
        /// [기능]: 타이틀 패널을 숨깁니다.
        /// [작성자]: 김지연
        /// </summary>
        public void Hide()
        {
            if (m_titlePanel != null)
            {
                m_titlePanel.SetActive(false);
            }
        }
        #endregion

        #region UI Event Callbacks
        /// <summary>
        /// [기능]: [게임 시작] 버튼 클릭 시 호출됩니다. 타이틀 화면을 닫고 인게임을 시작합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void func_OnStartButtonClick()
        {
            Debug.Log("[CardMatchTitleView] 게임 시작 버튼 클릭");
            Hide();

            if (m_onGameStart != null)
            {
                m_onGameStart.Invoke();
            }
        }

        /// <summary>
        /// [기능]: [게임 방법] 버튼 클릭 시 호출됩니다. 게임 방법 팝업을 표시합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void func_OnHowToPlayButtonClick()
        {
            Debug.Log("[CardMatchTitleView] 게임 방법 버튼 클릭");
            if (m_howToPlayPopup != null)
            {
                m_howToPlayPopup.SetActive(true);
                m_howToPlayPopup.transform.localScale = Vector3.zero;
                m_howToPlayPopup.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack);
            }
        }

        /// <summary>
        /// [기능]: 게임 방법 팝업의 [X] 닫기 버튼 클릭 시 호출됩니다.
        /// [작성자]: 김지연
        /// </summary>
        public void func_OnClosePopupButtonClick()
        {
            Debug.Log("[CardMatchTitleView] 게임 방법 팝업 닫기 클릭");
            if (m_howToPlayPopup != null)
            {
                m_howToPlayPopup.transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        m_howToPlayPopup.SetActive(false);
                    });
            }
        }
        #endregion
    }
}
