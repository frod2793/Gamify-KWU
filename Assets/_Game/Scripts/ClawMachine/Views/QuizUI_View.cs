using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: UI Canvas 상단에 퀴즈 질문 및 상세 설명을 출력하고 제한시간 타이머를 제어하는 UI View
    /// [작성자]: 윤승종
    /// </summary>
    public class QuizUI_View : MonoBehaviour
    {
        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("퀴즈 질문을 표시할 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_questionText;

        [SerializeField]
        [Tooltip("남은 제한 시간을 표시할 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_timeText;

        [SerializeField]
        [Tooltip("답안에 대한 상세 설명 내용을 표시할 텍스트 컴포넌트입니다. (스크롤 뷰 내에 위치)")]
        private TextMeshProUGUI m_explanationText;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private Color m_originalTextColor;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            if (m_questionText != null)
            {
                m_originalTextColor = m_questionText.color;
            }
            else
            {
                m_originalTextColor = Color.white;
            }
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged -= HandleStateChanged;
                m_viewModel.OnTimeChanged -= UpdateTimeUI;
            }

            if (m_questionText != null)
            {
                DOTween.Kill(m_questionText);
            }
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 이벤트 구독
            m_viewModel.OnStateChanged += HandleStateChanged;
            m_viewModel.OnTimeChanged += UpdateTimeUI;

            // 초기 문제 및 제한 시간 타이머 UI 동기화 갱신
            UpdateQuizUI();
            UpdateTimeUI(m_viewModel.TimeLeft);
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 뷰모델로부터 실시간 남은 제한시간을 전달받아 UI에 출력합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateTimeUI(float timeLeft)
        {
            if (m_timeText != null)
            {
                m_timeText.text = $"남은 시간: {Mathf.CeilToInt(timeLeft)}초";
            }
        }

        /// <summary>
        /// [기능]: 뷰모델에 등록된 현재 퀴즈 질문 및 상세 설명을 텍스트 컴포넌트에 출력하고 시각 요소를 리셋합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateQuizUI()
        {
            if (m_viewModel == null || m_questionText == null)
            {
                return;
            }

            var quiz = m_viewModel.CurrentQuiz;
            if (quiz != null)
            {
                m_questionText.text = quiz.Question;
                m_questionText.color = m_originalTextColor;
                m_questionText.transform.localScale = Vector3.one;

                if (m_explanationText != null)
                {
                    m_explanationText.text = quiz.Explanation;
                }
            }
            else
            {
                m_questionText.text = "인형뽑기 문제를 준비 중입니다...";
                if (m_explanationText != null)
                {
                    m_explanationText.text = string.Empty;
                }
            }
        }

        private void HandleStateChanged(ClawStateType state)
        {
            // 재수강 등으로 게임이 리셋되어 Idle 상태로 복귀했을 때 퀴즈 UI 및 타이머를 다시 안전하게 업데이트해줍니다.
            if (state == ClawStateType.Idle)
            {
                UpdateQuizUI();
                UpdateTimeUI(m_viewModel.TimeLeft);
            }
        }
        #endregion
    }
}
