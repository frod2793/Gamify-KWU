using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GameArifiction.ClawMachine;
using GamifyKWU.CraneGame.Data;
using EasyTransition;
using GameArifiction.Player;
using GameArifiction.UI.Common;

namespace GameArifiction.QuizClassic
{
    /// <summary>
    /// [기능]: 4지선다 객관식 버튼 조작과 문제 출제 시 시각 피드백 연출을 전담하는 클래식 퀴즈 뷰 컴포넌트
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-06
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 타이머 미사용 요구 사항에 따른 타이머 텍스트 비활성화 및 이벤트 바인딩 해제
    /// </summary>
    public class QuizClassicView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("텍스트 매쉬 프로 (TMPro)")]
        [SerializeField]
        [Tooltip("출제될 퀴즈 문제를 표시할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_questionText;

        [SerializeField]
        [Tooltip("남은 시간을 표시할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_timeText;

        [Header("선택지 조작 버튼군")]
        [SerializeField]
        [Tooltip("4지선다 객관식 선택지 버튼 배열입니다 (반드시 4개 할당 필요).")]
        private Button[] m_choiceButtons;

        [Header("결과 연출용 팝업")]
        [SerializeField]
        [Tooltip("정답 클리어 및 최종 오답 실패 결과를 연출할 공통 결과 팝업 뷰입니다.")]
        private CommonResultPopupView m_resultPopup;

        #endregion

        #region 내부 필드 (Private Fields)

        // [삭제]: View에서 Model인 PlayerSO를 직접 참조하여 수정하는 MVVM 위반 행위를 단절하기 위해 의존성 삭제

        [Header("이지 트랜지션 설정")]
        [SerializeField]
        [Tooltip("로비로 전환 시 화면 전환 연출을 위해 사용할 이지 트랜스 설정 자산입니다.")]
        private TransitionSettings m_transitionSettings;

        [SerializeField]
        [Tooltip("트랜스 효과가 진행되기 시작할 딜레이 시간(초)입니다.")]
        private float m_startDelay = 0f;

        private QuizClassicViewModel m_viewModel;
        private Color m_originalQuestionColor;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            if (m_questionText != null)
            {
                // [버그 수정]: 에셋 프리팹 결함으로 알파가 0인 상태를 완전히 복원하기 위해 강제 1.0f 불투명 적용
                Color color = m_questionText.color;
                color.a = 1f;
                m_questionText.color = color;
                m_originalQuestionColor = color;
            }
            else
            {
                m_originalQuestionColor = Color.white;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region 초기화 (Initialization)

        public void Initialize(QuizClassicViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 1. 뷰모델 데이터 이벤트 바인딩 (타이머 및 스코어 기능 연쇄 업데이트 재활성화)
            m_viewModel.OnNextQuizLoaded += HandleNextQuizLoaded;
            m_viewModel.OnStateChanged += HandleStateChanged;
            // 타이머 미사용에 따라 OnTimeChanged 구독 제거

            // [추가]: 타이머 미사용으로 텍스트 컴포넌트 비활성화
            if (m_timeText != null)
            {
                m_timeText.gameObject.SetActive(false);
            }

            // 2. 피드백 연쇄 시각 효과 연동
            m_viewModel.OnQuizSuccess += HandleQuizSuccess;
            m_viewModel.OnQuizFailed += HandleQuizFailed;
            m_viewModel.OnWrongAnswerSelected += HandleWrongAnswerSelected;

            // 3. 버튼 리스너 바인딩 (인덱스 캡처 방지용 내부 Scope 변수 사용)
            if (m_choiceButtons != null && m_choiceButtons.Length == 4)
            {
                for (int i = 0; i < m_choiceButtons.Length; i++)
                {
                    if (m_choiceButtons[i] != null)
                    {
                        int index = i; // Closure 복사
                        m_choiceButtons[i].onClick.AddListener(() => func_OnChoiceButtonClick(index));
                    }
                }
            }
            else
            {
                Debug.LogError("[QuizClassicView] m_choiceButtons 크기가 4가 아닙니다! 인스펙터를 확인하세요.");
            }

            // 4. 결과 연계 공통 패널 이벤트 바인딩
            m_viewModel.OnQuizSuccess += HandleQuizSuccessEvent;
            m_viewModel.OnQuizFailed += HandleQuizFailedEvent;
            m_viewModel.OnReTakeRequested += HandleTimeOverEvent;
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnNextQuizLoaded -= HandleNextQuizLoaded;
                m_viewModel.OnStateChanged -= HandleStateChanged;
                // 타이머 미사용에 따라 OnTimeChanged 해제 제거
                m_viewModel.OnQuizSuccess -= HandleQuizSuccess;
                m_viewModel.OnQuizFailed -= HandleQuizFailed;
                m_viewModel.OnWrongAnswerSelected -= HandleWrongAnswerSelected;

                m_viewModel.OnQuizSuccess -= HandleQuizSuccessEvent;
                m_viewModel.OnQuizFailed -= HandleQuizFailedEvent;
                m_viewModel.OnReTakeRequested -= HandleTimeOverEvent;

                m_viewModel.Dispose();
            }

            if (m_choiceButtons != null)
            {
                for (int i = 0; i < m_choiceButtons.Length; i++)
                {
                    if (m_choiceButtons[i] != null)
                    {
                        m_choiceButtons[i].onClick.RemoveAllListeners();
                    }
                }
            }

            // DOTween 정리
            DOTween.Kill(m_questionText);
        }

        #endregion



        #region 버튼 클릭 이벤트 핸들러 (Public Methods)

        /// <summary>
        /// [기능]: 사용자가 객관식 선택 버튼을 터치했을 때 뷰모델로 인덱스 데이터를 전달합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnChoiceButtonClick(int choiceIndex)
        {
            if (m_viewModel != null)
            {
                m_viewModel.func_SelectAnswer(choiceIndex);
            }
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        private void HandleNextQuizLoaded(QuizData quiz, List<string> choices)
        {
            // A. 질문 텍스트 출력 복구
            if (m_questionText != null)
            {
                m_questionText.text = quiz.Question;
                m_questionText.color = m_originalQuestionColor;
                m_questionText.transform.localScale = Vector3.one;
            }

            // B. 4개 선택지 버튼에 텍스트 주입 및 활성화
            if (m_choiceButtons != null && m_choiceButtons.Length == 4 && choices.Count == 4)
            {
                for (int i = 0; i < m_choiceButtons.Length; i++)
                {
                    if (m_choiceButtons[i] != null)
                    {
                        m_choiceButtons[i].interactable = true;
                        
                        TextMeshProUGUI btnTxt = m_choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (btnTxt != null)
                        {
                            btnTxt.text = choices[i];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// [기능]: 뷰모델의 상태 변화에 따라 객관식 버튼들의 상호작용 및 팝업 표시 여부를 제어합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 마지막 문제 정답 시 OnQuizSuccess와 Result 상태 진입에 의해 발생하던 이중 팝업 호출 방지를 위해 Result 진입 시의 중복 팝업 호출 로직 제거
        /// </summary>
        private void HandleStateChanged(QuizStateType state)
        {
            // 게임 가동 상태에 따라 버튼 인터랙션 제어
            bool isPlayable = (state == QuizStateType.Playing);
            
            if (m_choiceButtons != null)
            {
                for (int i = 0; i < m_choiceButtons.Length; i++)
                {
                    if (m_choiceButtons[i] != null)
                    {
                        m_choiceButtons[i].interactable = isPlayable;
                    }
                }
            }
        }

        private void HandleQuizSuccess()
        {
            if (m_questionText == null)
            {
                return;
            }

            DOTween.Kill(m_questionText);
            
            // 질문 텍스트 초록색 피드백 및 스케일 튕김(Punch) 효과 (지문 내용 보존)
            m_questionText.DOColor(new Color(0.2f, 0.9f, 0.2f, 1.0f), 0.4f).SetEase(Ease.OutQuad);
            m_questionText.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.5f, 8, 1f);
        }

        private void HandleQuizFailed()
        {
            if (m_questionText == null)
            {
                return;
            }

            DOTween.Kill(m_questionText);

            // 질문 텍스트 빨간색 피드백 및 좌우 흔들림(Shake) 효과 (지문 내용 보존)
            m_questionText.DOColor(new Color(0.9f, 0.2f, 0.2f, 1.0f), 0.4f).SetEase(Ease.OutQuad);
            m_questionText.transform.DOShakePosition(0.5f, new Vector3(8f, 0f, 0f), 12, 90f);
        }

        /// <summary>
        /// [기능]: 잘못된 오답 번호를 받았을 때 해당 버튼을 비활성화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleWrongAnswerSelected(int choiceIndex)
        {
            if (m_choiceButtons != null && choiceIndex >= 0 && choiceIndex < m_choiceButtons.Length)
            {
                if (m_choiceButtons[choiceIndex] != null)
                {
                    m_choiceButtons[choiceIndex].interactable = false;
                    Debug.Log($"[QuizClassicView] 오답 보기 버튼 비활성화 처리 완료: 선택지 인덱스 {choiceIndex}");
                }
            }
        }

        /// <summary>
        /// [기능]: 뷰모델로부터 실시간 남은 제한시간을 전달받아 클래식 퀴즈 UI 텍스트에 출력합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateTimeUI(float timeLeft)
        {
            if (m_timeText != null)
            {
                m_timeText.text = $"남은 시간: {Mathf.CeilToInt(timeLeft)}초";
            }
        }

        #endregion

        #region 결과 팝업 중개 로직 (Private Methods)

        /// <summary>
        /// [기능]: 퀴즈 성공 이벤트를 받아 DTO를 세팅하고 공통 팝업을 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleQuizSuccessEvent()
        {
            if (m_viewModel == null || m_resultPopup == null)
            {
                return;
            }

            Debug.Log("[QuizClassicView] 정답 성공 이벤트 수신 -> 공통 결과 팝업 세팅.");

            string titleText;
            string descriptionText = null;
            string confirmText;
            System.Action confirmCallback;

            MinigameGrade? targetGrade = null;

            if (m_viewModel.IsLastQuiz)
            {
                // [리팩토링]: 뷰모델의 프로퍼티를 통해 실제 총 누적 플레이 소요 시간 획득
                float totalPlayTime = m_viewModel.TotalPlayTime;
                MinigameGrade calculatedGrade = MinigameGrade.D;

                if (totalPlayTime <= 60f)
                {
                    calculatedGrade = MinigameGrade.A;
                }
                else if (totalPlayTime <= 80f)
                {
                    calculatedGrade = MinigameGrade.B;
                }
                else if (totalPlayTime <= 100f)
                {
                    calculatedGrade = MinigameGrade.C;
                }
                else if (totalPlayTime <= 120f)
                {
                    calculatedGrade = MinigameGrade.D;
                }
                else
                {
                    calculatedGrade = MinigameGrade.F;
                }

                // [리팩토링]: 뷰가 직접 저장하지 않고 뷰모델에 성적 기록 위임
                m_viewModel.SaveFinalGrade(calculatedGrade);

                targetGrade = calculatedGrade;

                titleText = "게임 결과";
                confirmText = "로비로 이동";
                confirmCallback = func_OnExitToLobby;

                CommonPopupDataDTO popupData = new CommonPopupDataDTO(
                    titleText,
                    $"소요시간: {totalPlayTime:F0}초",
                    "UX/UI개론",
                    targetGrade,
                    confirmText,
                    confirmCallback
                );

                m_resultPopup.Setup(popupData);
            }
            else
            {
                titleText = "★ 정답입니다! ★";
                descriptionText = "올바른 정답을 선택하셨습니다.\n다음 문제로 이동해 보십시오!";
                confirmText = "다음 문제로";
                confirmCallback = () => m_viewModel.ContinueAfterCorrectAnswer();

                CommonPopupDataDTO popupData = new CommonPopupDataDTO(
                    titleText,
                    descriptionText,
                    null,
                    targetGrade,
                    confirmText,
                    confirmCallback
                );

                m_resultPopup.Setup(popupData);
            }
        }

        /// <summary>
        /// [기능]: 퀴즈 실패(오답) 이벤트를 받아 DTO를 세팅하고 공통 팝업을 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleQuizFailedEvent()
        {
            if (m_viewModel == null || m_resultPopup == null)
            {
                return;
            }

            Debug.Log("[QuizClassicView] 오답 실패 이벤트 수신 -> 공통 결과 팝업 세팅.");

            CommonPopupDataDTO popupData = new CommonPopupDataDTO(
                "★ 틀린 오답입니다! ★",
                "아쉽게도 틀렸습니다. 다시 한번 기회를 드릴 테니 올바른 정답을 골라 보세요!",
                null,
                null,
                "계속하기",
                () => m_viewModel.ContinueAfterWrongAnswer()
            );

            m_resultPopup.Setup(popupData);
        }

        /// <summary>
        /// [기능]: 퀴즈 시간 초과 이벤트를 받아 DTO를 세팅하고 공통 팝업을 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleTimeOverEvent()
        {
            if (m_viewModel == null || m_resultPopup == null)
            {
                return;
            }

            Debug.Log("[QuizClassicView] 시간 초과 이벤트 수신 -> 공통 결과 팝업 세팅.");

            CommonPopupDataDTO popupData = new CommonPopupDataDTO(
                "★ 제한 시간이 초과되었습니다! ★",
                "제한 시간이 모두 경과하여 퀴즈에 실패하셨습니다.\n재수강(리플레이)을 진행하여 다시 도전해 보십시오!",
                null,
                null,
                "재수강 진행",
                () => m_viewModel.AcceptReTake()
            );

            m_resultPopup.Setup(popupData);
        }

        /// <summary>
        /// [기능]: 마지막 문제를 맞추고 메인 화면으로 돌아가는 버튼 콜백입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void func_OnExitToLobby()
        {
            Debug.Log("[QuizClassicView] 플레이어가 클래식 퀴즈 최종 완료 성적표를 확인하고 메인(Lobby)으로 이동합니다.");
            
            // [리팩토링]: 뷰가 직접 Model에 접근하여 플래그를 세팅하던 로직을 뷰모델에 위임
            if (m_viewModel != null)
            {
                m_viewModel.SaveExitLobbyPosition();
            }

            if (m_transitionSettings != null)
            {
                TransitionManager manager = FindFirstObjectByType<TransitionManager>();
                if (manager != null)
                {
                    TransitionManager.Instance().Transition("Lobby", m_transitionSettings, m_startDelay);
                    return;
                }
            }

            SceneManager.LoadScene("Lobby");
        }

        #endregion
    }
}
