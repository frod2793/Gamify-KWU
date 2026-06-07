using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameArifiction.Core.Audio;
using VContainer;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: UI Canvas 상단에 제한시간 타이머를 제어하고 게임 시작 전 퀴즈 문제 팝업 및 문제 다시보기 기능을 연동하는 UI View.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-06
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: ISoundService를 주입받아 퀴즈 노출 시 Sfx_claw_question 재생 및 버튼 클릭 시 터치음 일괄 연동 적용
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

        [SerializeField]
        [Tooltip("퀴즈 문제를 게임 중 다시 볼 수 있는 버튼입니다.")]
        private Button m_showQuizButton;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private ISoundService m_soundService;
        #endregion

        #region 의존성 주입 (Dependency Injection)
        /// <summary>
        /// [기능]: VContainer를 통해 공통 사운드 서비스를 주입받습니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public void Construct(ISoundService soundService)
        {
            m_soundService = soundService;
        }
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

            if (m_showQuizButton != null)
            {
                m_showQuizButton.onClick.RemoveListener(func_OnShowQuizButtonClick);
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

            if (m_showQuizButton != null)
            {
                m_showQuizButton.onClick.AddListener(func_OnShowQuizButtonClick);
            }
        }
        #endregion

        #region UI 이벤트 콜백 (Public Methods)
        /// <summary>
        /// [기능]: 퀴즈 문제 확인(시작) 버튼 클릭 시 호출되어 뷰모델의 퀴즈 종료 및 카운트다운을 트리거합니다.
        ///         (에디터 상에서 Button.OnClick 이벤트에 직접 등록하여 사용합니다.)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-06
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        public void func_OnStartButtonClicked()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }

            if (m_viewModel != null)
            {
                m_viewModel.func_CompleteQuizReveal();
            }

            if (m_quizPopupPanel != null)
            {
                m_quizPopupPanel.SetActive(false);
            }
        }

        /// <summary>
        /// [기능]: 게임 중 퀴즈 문제를 다시 확인하는 다시보기 팝업 패널을 노출시킵니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-06
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        public void func_OnShowQuizButtonClick()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }

            if (m_quizPopupPanel != null)
            {
                m_quizPopupPanel.SetActive(true);
            }
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 뷰모델로부터 실시간 남은 제한시간을 전달받아 "00초" 형식으로 UI에 출력합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-05
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        private void UpdateTimeUI(float timeLeft)
        {
            if (m_timeText != null)
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
                m_timeText.text = $"{seconds:00}초";
            }
        }

        private void HandleStateChanged(ClawStateType state)
        {
            // 재수강 등으로 게임이 리셋되어 Idle 상태로 복귀했을 때 타이머를 다시 안전하게 업데이트해줍니다.
            if (state == ClawStateType.Idle)
            {
                UpdateTimeUI(m_viewModel.TimeLeft);
            }

            // [퀴즈 문제 팝업 흐름 제어]: QuizReveal 상태일 때 팝업 패널을 띄우고 질문 효과음을 재생합니다.
            if (state == ClawStateType.QuizReveal)
            {
                if (m_quizPopupPanel != null)
                {
                    m_quizPopupPanel.SetActive(true);
                }

                if (m_soundService != null)
                {
                    m_soundService.PlaySFX(SoundDefine.Sfx_claw_question);
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
