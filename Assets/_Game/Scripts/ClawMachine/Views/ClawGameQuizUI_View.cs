using DG.Tweening;
using TMPro;
using GamifyKWU.CraneGame.Data;
using UnityEngine;
using UnityEngine.UI;
using GameArifiction.Core.Audio;
using VContainer;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: UI Canvas 상단에 제한시간 타이머를 제어하고 게임 시작 전 퀴즈 문제 팝업 및 문제 다시보기 기능을 연동하는 UI View (클로게임 전용).
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawGameQuizUI_View : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("남은 제한 시간을 표시할 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_timeText;

        [Header("퀴즈 문제 팝업 UI (Inspector)")]
        [SerializeField]
        [Tooltip("게임 시작 전 퀴즈 문제를 모달처럼 보여줄 팝업 패널입니다.")]
        private GameObject m_quizPopupPanel;

        [SerializeField]
        [Tooltip("퀴즈 문제 제목을 표시할 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_titleText;

        [SerializeField]
        [Tooltip("퀴즈 질문을 표시할 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_questionText;

        [SerializeField]
        [Tooltip("퀴즈 힌트 텍스트를 단독 표시할 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_hintText;
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
        }
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: 뷰모델을 주입받아 이벤트를 구독하고 퀴즈 문제 텍스트를 시안 레이아웃에 맞춰 포맷팅하여 바인딩합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-21
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 시안의 ◆ 리스트 지시자, 구분선 및 힌트 텍스트의 분리 렌더링 동적 포맷팅 적용
        /// </summary>
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 이벤트 구독
            m_viewModel.OnStateChanged += HandleStateChanged;
            m_viewModel.OnTimeChanged += UpdateTimeUI;

            // 초기 제한 시간 타이머 UI 동기화 갱신
            UpdateTimeUI(m_viewModel.TimeLeft);

            // 퀴즈 문제 제목 텍스트 동적 바인딩
            if (m_titleText != null && m_viewModel.CurrentQuiz != null)
            {
                m_titleText.text = GetCategoryKoreanName(m_viewModel.CurrentQuiz.Category);
            }

            // 퀴즈 문제 텍스트 시안 스타일 포맷팅 및 동적 바인딩
            if (m_questionText != null && m_viewModel.CurrentQuiz != null)
            {
                string rawQuestion = m_viewModel.CurrentQuiz.Question;
                
                // [보완]: 유니티 YAML 직렬화에서 가져온 이중 백슬래시 개행 문자열을 진짜 개행 문자로 치환합니다.
                rawQuestion = rawQuestion.Replace("\\n", "\n");

                string[] lines = rawQuestion.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    
                    // 1. 첫 번째 라인 (질문 정의 타이틀)
                    sb.AppendLine(lines[0]);
                    
                    // 2. 점선 구분선 삽입
                    sb.AppendLine("<color=#bbbbbb>- - - - - - - - - - - - - - - - - - - - - - - - - - - - -</color>");
                    sb.AppendLine();

                    // 3. 두 번째 라인부터 ◆ 다이아몬드 지시자를 자동 부착하여 설명 리스트 구성
                    for (int i = 1; i < lines.Length; i++)
                    {
                        sb.AppendLine($"<color=#1a1a1a>◆</color> {lines[i]}");
                    }

                    m_questionText.text = sb.ToString();
                }
                else
                {
                    m_questionText.text = rawQuestion;
                    Debug.LogWarning($"[ClawGameQuizUI_View] 분할된 라인이 없어 원본 텍스트를 그대로 바인딩했습니다. 질문: {rawQuestion}");
                }
            }

            // 힌트 텍스트 단독 영역 할당
            if (m_hintText != null && m_viewModel.CurrentQuiz != null)
            {
                m_hintText.text = m_viewModel.CurrentQuiz.Hint;
            }

            if (m_quizPopupPanel != null)
            {
                m_quizPopupPanel.SetActive(false);
            }
        }
        #endregion

        #region 이벤트 (Events)
        /// <summary>
        /// [기능]: 퀴즈 문제 다시보기 팝업이 닫힐 때 발생하는 이벤트입니다.
        /// [작성자]: 윤승종
        /// </summary>
        public event System.Action OnQuizClosed;
        #endregion

        #region UI 이벤트 콜백 (Public Methods)
        /// <summary>
        /// [기능]: 퀴즈 문제 확인(시작) 버튼 클릭 시 호출되어 뷰모델의 퀴즈 종료 및 카운트다운을 트리거합니다.
        ///         (에디터 상에서 Button.OnClick 이벤트에 직접 등록하여 사용합니다.)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 퀴즈 팝업 닫힘 이벤트 트리거 추가
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

            if (OnQuizClosed != null)
            {
                OnQuizClosed.Invoke();
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

        /// <summary>
        /// [기능]: 퀴즈 카테고리 enum 값을 직관적인 한글 타이틀 이름으로 변환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private string GetCategoryKoreanName(QuizCategory category)
        {
            switch (category)
            {
                case QuizCategory.UIUX:
                    return "Q. UI/UX 디자인 문제";
                case QuizCategory.SoftwareEngineering:
                    return "Q. 소프트웨어 공학 문제";
                case QuizCategory.DesignPattern:
                    return "Q. 디자인 패턴 문제";
                case QuizCategory.UnityEngine:
                    return "Q. 유니티 엔진 문제";
                case QuizCategory.GeneralCS:
                    return "Q. 컴퓨터 사이언스 문제";
                default:
                    return "Q. 퀴즈 문제";
            }
        }
        #endregion
    }
}
