using TMPro;
using GamifyKWU.CraneGame.Data;
using UnityEngine;
using UnityEngine.UI;
using GameArifiction.Core.Audio;
using VContainer;
using static UnityEngine.Mathf;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: UI Canvas 상단에 제한시간 타이머를 제어하고 게임 시작 전 퀴즈 문제 팝업 및 문제 다시보기 기능을 연동하는 UI View (클로게임 전용).
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-21
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 실시간 렌더링 질문 타이틀 줄 수 기반 구분선 Y축 동적 보정(1줄: Y=257f, 2줄: Y=210f) 적용, 폰트 오염 방지(fontSize 36.25f 강제 고정) 및 우측 여백(40f) 고정 복원, using static UnityEngine.Mathf 활용을 통한 코드 최적화, UniTask 1프레임 대기 정렬 기법 연동.
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
        /// [수정 내용]: 우측 마진 40f 고정 복원, 폰트 사이즈 36.25f 강제 고정 및 실시간 렌더링 질문 타이틀 줄 수 기반 구분선 Y축 동적 보정 적용
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
                // [보완]: Auto Sizing에 의해 텍스트 크기가 강제 축소되는 것을 전면 차단합니다.
                m_questionText.enableAutoSizing = false;

                // [보완]: 에디터 시뮬레이션 등에 의해 폰트 크기가 오염 및 강제 축소되는 현상을 방지하기 위해 폰트 사이즈를 강제 고정합니다.
                m_questionText.fontSize = 36.25f;

                // [보완]: 자동 줄바꿈(Word Wrapping) 기능이 꺼져 있어 마진이 씹히는 현상을 방지합니다.
                m_questionText.textWrappingMode = TextWrappingModes.Normal;

                // [보완]: 텍스트 줄바꿈 시 타이틀과 리스트 행간이 찌그러지며 겹치지 않게 행간을 안전 확보합니다.
                m_questionText.lineSpacing = 15f;

                // [보완]: 텍스트 사이즈를 조절하지 않고 물음표 아이콘과 겹치지 않게 우측 마진을 40f 고정으로 확보합니다.
                Vector4 currentMargin = m_questionText.margin;
                m_questionText.margin = new Vector4(currentMargin.x, currentMargin.y, 40f, currentMargin.w);

                string rawQuestion = m_viewModel.CurrentQuiz.Question;

                // [보완]: 질문 텍스트 내에 포함된 중복 힌트 문구(예: (힌트: ...))를 동적으로 감지하여 도려냅니다.
                int hintStartIndex = rawQuestion.IndexOf("(힌트");
                if (hintStartIndex == -1)
                {
                    hintStartIndex = rawQuestion.IndexOf("<b>(힌트");
                }
                if (hintStartIndex == -1)
                {
                    hintStartIndex = rawQuestion.IndexOf("(Hint");
                }

                if (hintStartIndex != -1)
                {
                    // 힌트 시작점 이전의 순수 질문 본문 텍스트만 추출하고 트림 처리
                    rawQuestion = rawQuestion.Substring(0, hintStartIndex).Trim();
                }
                
                // [보완]: 유니티 YAML 직렬화에서 가져온 이중 백슬래시 개행 문자열을 진짜 개행 문자로 치환합니다.
                rawQuestion = rawQuestion.Replace("\\n", "\n");

                string[] lines = rawQuestion.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    
                    // 1. 첫 번째 라인 (질문 정의 타이틀)을 볼드체로 렌더링
                    sb.AppendLine($"<b>{lines[0]}</b>");
                    
                    // [수정]: 텍스트로 중복 삽입하던 점선 구분선을 전면 제거하고 씬의 Line 이미지로 단독 대체합니다.
                    // 타이틀과 리스트 간의 구분을 위한 빈 줄 개행만 남깁니다.
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

            // [보완]: 1프레임 대기 후 실제 렌더링된 텍스트 줄바꿈 정보를 기반으로 라인 위치 정밀 동적 보정
            AdjustLinePositionAsync(this.GetCancellationTokenOnDestroy()).Forget();
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
        /// [수정 날짜]: 2026-06-21
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: using static UnityEngine.Mathf 도입으로 함수 호출 간소화
        /// </summary>
        private void UpdateTimeUI(float timeLeft)
        {
            if (m_timeText != null)
            {
                int seconds = Max(0, CeilToInt(timeLeft));
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

        /// <summary>
        /// [기능]: 1프레임 대기 후 렌더링된 텍스트 메쉬 데이터를 분석하여 질문 타이틀의 실질 줄 수에 비례하도록 구분선의 Y축 위치를 정밀 보정합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-21
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        private async UniTaskVoid AdjustLinePositionAsync(CancellationToken cancellationToken)
        {
            // Canvas 및 TMP 레이아웃이 갱신되는 1프레임을 대기합니다.
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            if (m_questionText == null || m_quizPopupPanel == null)
            {
                return;
            }

            // 실시간 텍스트 레이아웃 정보 강제 갱신
            m_questionText.ForceMeshUpdate(true);

            Transform lineTransform = m_quizPopupPanel.transform.Find("Line");
            if (lineTransform != null)
            {
                RectTransform lineRect = lineTransform.GetComponent<RectTransform>();
                if (lineRect != null)
                {
                    // 첫 번째 줄바꿈('\n') 문자의 실제 lineNumber를 조회하여 타이틀의 줄 수를 동적 산출합니다.
                    int titleLineCount = 1;
                    for (int i = 0; i < m_questionText.textInfo.characterCount; i++)
                    {
                        if (m_questionText.textInfo.characterInfo[i].character == '\n')
                        {
                            if (i > 0)
                            {
                                titleLineCount = m_questionText.textInfo.characterInfo[i - 1].lineNumber + 1;
                            }
                            break;
                        }
                    }

                    // 1줄일 때 257f, 2줄일 때 210f, 그 이상(3줄)일 때도 비례하여 안전 마진 Y좌표 연산
                    float targetY = 257f - (titleLineCount - 1) * 47f;
                    lineRect.anchoredPosition = new Vector2(lineRect.anchoredPosition.x, targetY);
                    
                    Debug.Log($"[ClawGameQuizUI_View] 실제 렌더링된 질문 타이틀 줄 수: {titleLineCount}, Line Y축 동적 보정 좌표 적용: {targetY}");
                }
            }
        }
        #endregion
    }
}
