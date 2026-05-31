using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: UI Canvas 상단에 제한시간 타이머를 제어하고 게임 시작 전 퀴즈 문제 팝업을 연동하는 UI View.
    /// [작성자]: 윤승종
    /// </summary>
    public class QuizUI_View : MonoBehaviour
    {
        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("남은 제한 시간을 표시할 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_timeText;

        [Header("퀴즈 문제 팝업 UI (Inspector)")]
        [SerializeField]
        [Tooltip("게임 시작 전 퀴즈 문제를 모달처럼 보여줄 팝업 패널입니다.")]
        private GameObject m_quizPopupPanel;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            // 초기 팝업 패널 비활성화 방어막 가동
            if (m_quizPopupPanel != null)
            {
                m_quizPopupPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged -= HandleStateChanged;
                m_viewModel.OnTimeChanged -= UpdateTimeUI;
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

            // 초기 제한 시간 타이머 UI 동기화 갱신
            UpdateTimeUI(m_viewModel.TimeLeft);

            if (m_quizPopupPanel != null)
            {
                m_quizPopupPanel.SetActive(false);
            }
        }
        #endregion

        #region UI 이벤트 콜백 (Public Methods)
        /// <summary>
        /// [기능]: 퀴즈 문제 확인(시작) 버튼 클릭 시 호출되어 뷰모델의 퀴즈 종료 및 카운트다운을 트리거합니다.
        ///         (에디터 상에서 Button.OnClick 이벤트에 직접 등록하여 사용합니다.)
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnStartButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.func_CompleteQuizReveal();
            }
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

        private void HandleStateChanged(ClawStateType state)
        {
            // 재수강 등으로 게임이 리셋되어 Idle 상태로 복귀했을 때 타이머를 다시 안전하게 업데이트해줍니다.
            if (state == ClawStateType.Idle)
            {
                UpdateTimeUI(m_viewModel.TimeLeft);
            }

            // [퀴즈 문제 팝업 흐름 제어]: QuizReveal 상태일 때 팝업 패널을 띄웁니다.
            if (state == ClawStateType.QuizReveal)
            {
                if (m_quizPopupPanel != null)
                {
                    m_quizPopupPanel.SetActive(true);
                }
            }
            else
            {
                // 그 외 플레이 및 카운트다운 등의 상태에서는 팝업을 안전히 하이드시킵니다.
                if (m_quizPopupPanel != null)
                {
                    m_quizPopupPanel.SetActive(false);
                }
            }
        }
        #endregion
    }
}
