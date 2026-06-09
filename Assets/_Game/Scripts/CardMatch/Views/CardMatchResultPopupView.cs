using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using GameArifiction.Player;
using UnityEngine.SceneManagement;
using EasyTransition;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 게임 결과 팝업의 UI를 관리하는 View 클래스입니다.
    ///         학점, 뒤집기 횟수, 학점별 멘트를 표시하고 다음 게임으로의 전환을 처리합니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardMatchResultPopupView : MonoBehaviour
    {
        #region SerializeField
        [Header("결과 팝업 요소")]
        [SerializeField] private GameObject m_resultPanel;
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private TextMeshProUGUI m_gradeText;
        [SerializeField] private Image m_gradeImage;
        
        [Header("학점 이미지 세팅 (A, B, C, D, F)")]
        [SerializeField] private Sprite[] m_gradeSprites;

        [Header("기타 정보")]
        [SerializeField] private TextMeshProUGUI m_flipCountText;
        [SerializeField] private TextMeshProUGUI m_messageText;
        [SerializeField] private Button m_nextButton;

        [Header("트랜지션 연출 설정")]
        [SerializeField] private TransitionSettings m_transitionSettings;
        [SerializeField] private float m_transitionDelay = 0.5f;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            if (m_nextButton != null)
            {
                m_nextButton.onClick.AddListener(func_OnNextButtonClick);
            }
        }

        private void OnDestroy()
        {
            if (m_nextButton != null)
            {
                m_nextButton.onClick.RemoveListener(func_OnNextButtonClick);
            }

            if (m_resultPanel != null)
            {
                m_resultPanel.transform.DOKill();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// [기능]: 게임 결과 팝업을 표시합니다. 학점, 뒤집기 횟수, 멘트를 갱신합니다.
        /// [작성자]: 김지연
        /// </summary>
        /// <param name="grade">산출된 학점 등급</param>
        /// <param name="message">학점에 대응하는 멘트</param>
        /// <param name="flipCount">총 뒤집기 횟수</param>
        public void Show(MinigameGrade grade, string message, int flipCount)
        {
            Debug.Log($"[CardMatchResultPopupView] 결과 팝업 표시: 학점 {grade}, 뒤집기 횟수 {flipCount}");

            if (m_gradeText != null)
            {
                m_gradeText.gameObject.SetActive(false);
            }

            if (m_gradeImage != null && m_gradeSprites != null)
            {
                m_gradeImage.gameObject.SetActive(true);
                
                int index = (int)grade - 1; // A를 0번 인덱스로 설정
                
                if (index >= 0 && index < m_gradeSprites.Length)
                {
                    m_gradeImage.sprite = m_gradeSprites[index];
                    m_gradeImage.preserveAspect = true;
                }
                else
                {
                    Debug.LogWarning($"[CardMatchResultPopupView] 학점 {grade}에 해당하는 이미지가 배열에 없습니다.");
                }
            }
            if (m_flipCountText != null)
            {
                m_flipCountText.text = flipCount.ToString();
            }
            if (m_messageText != null)
            {
                m_messageText.text = message;
            }

            if (m_resultPanel != null)
            {
                m_resultPanel.SetActive(true);
                m_resultPanel.transform.localScale = Vector3.zero;
                m_resultPanel.transform.DOScale(Vector3.one, 0.4f)
                    .SetEase(Ease.OutBack);
            }
        }

        /// <summary>
        /// [기능]: 결과 팝업을 숨깁니다.
        /// [작성자]: 김지연
        /// </summary>
        public void Hide()
        {
            if (m_resultPanel != null)
            {
                m_resultPanel.SetActive(false);
            }
        }
        #endregion

        #region UI Event Callbacks
        /// <summary>
        /// [기능]: [다음으로] 버튼 클릭 시 호출됩니다.
        /// [작성자]: 김지연
        /// </summary>
        public void func_OnNextButtonClick()
        {
            Debug.Log("[CardMatchResultPopupView] 다음으로 버튼 클릭. 로비로 복귀합니다.");

            if (TransitionManager.Instance() != null)
            {
                if (m_transitionSettings != null)
                {
                    TransitionManager.Instance().Transition("Lobby", m_transitionSettings, m_transitionDelay);
                    return;
                }
            }

            SceneManager.LoadScene("Lobby");
        }
        #endregion
    }
}
