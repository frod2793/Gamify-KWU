using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using EasyTransition; // 이지 트랜지션 기능 수입
using VContainer;
using GameArifiction.Player;

namespace GameArifiction.QuizClassic
{
    /// <summary>
    /// [기능]: 클래식 퀴즈의 최종 정답 성공 또는 오답/시간 초과 실패 결과를 출력하는 결과 패널 UI View
    /// [작성자]: 윤승종
    /// </summary>
    public class QuizClassicResultPopupView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("결과 설명 및 재수강 패널티 정보를 보여줄 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_descriptionText;

        [SerializeField]
        [Tooltip("결과 확인(다음 문제 혹은 재수강 진행) 버튼입니다.")]
        private Button m_confirmButton;

        #endregion

        #region 내부 필드 (Private Fields)
        [Header("세션 데이터")]
        [SerializeField]
        [Tooltip("씬 간 플레이어 위치 상태 보존을 위한 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;

        [Header("이지 트랜지션 설정")]
        [SerializeField]
        [Tooltip("로비로 전환 시 화면 전환 연출을 위해 사용할 이지 트랜스 설정 자산입니다.")]
        private TransitionSettings m_transitionSettings;

        [SerializeField]
        [Tooltip("트랜스 효과가 진행되기 시작할 딜레이 시간(초)입니다.")]
        private float m_startDelay = 0f;

        private QuizClassicViewModel m_viewModel;
        private TextMeshProUGUI m_confirmButtonText;
        private bool m_isSuccessState;
        private bool m_isTimeOverState;
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: 뷰모델을 주입받아 초기화하고, 버튼 이벤트 및 뷰모델 이벤트를 바인딩합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Initialize(QuizClassicViewModel viewModel)
        {
            m_viewModel = viewModel;

            // TMProUGUI 하위 텍스트 컴포넌트 캐싱 및 리스너 바인딩
            if (m_confirmButton != null)
            {
                m_confirmButtonText = m_confirmButton.GetComponentInChildren<TextMeshProUGUI>();
                m_confirmButton.onClick.AddListener(func_OnConfirmButtonClick);
            }

            // 뷰모델 결과 및 제한 시간 초과 이벤트 다이렉트 구독
            m_viewModel.OnQuizSuccess += HandleQuizSuccess;
            m_viewModel.OnQuizFailed += HandleQuizFailed;
            m_viewModel.OnReTakeRequested += HandleTimeOver;

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (m_confirmButton != null)
            {
                m_confirmButton.onClick.RemoveListener(func_OnConfirmButtonClick);
            }

            if (m_viewModel != null)
            {
                m_viewModel.OnQuizSuccess -= HandleQuizSuccess;
                m_viewModel.OnQuizFailed -= HandleQuizFailed;
                m_viewModel.OnReTakeRequested -= HandleTimeOver;
            }
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 성공/실패 여부에 맞춤 워딩을 로드하고 결과 팝업 패널을 씬 상에 활성화 렌더링합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_ShowPopup(bool isSuccess)
        {
            m_isSuccessState = isSuccess;
            gameObject.SetActive(true);
            UpdatePanelContent(isSuccess);
        }

        /// <summary>
        /// [기능]: 결과 팝업 패널을 씬 상에서 비활성화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_HidePopup()
        {
            gameObject.SetActive(false);
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 성공과 실패 상태 분기에 부합하는 안내문 출력 및 버튼 컴포넌트의 가시성/워딩 동적 세팅을 수행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdatePanelContent(bool isSuccess)
        {
            if (m_viewModel == null || m_descriptionText == null)
            {
                return;
            }

            if (isSuccess)
            {
                if (m_viewModel.IsLastQuiz)
                {
                    // 클래식 퀴즈 최종 완료 성공 시 UI 세팅 및 성적 판정 계산
                    float totalPlayTime = 0f;
                    MinigameGrade calculatedGrade = MinigameGrade.D;

                    if (m_playerSO != null)
                    {
                        totalPlayTime = m_playerSO.TotalMinigamePlayTime;
                        
                        // 성적 판정 기준:
                        // A - 60초 내
                        // B - 70~80초 사이 (60초 초과 80초 이하)
                        // C - 90~100초 사이 (80초 초과 100초 이하)
                        // D - 110~120초 사이 (100초 초과 120초 이하)
                        // F - 시간 초과 (120초 초과)
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

                        // PlayerSO 데이터에 저장 반영
                        m_playerSO.SetMinigameGrade("ClawMachineQuiz", calculatedGrade);
                    }

                    m_descriptionText.text = "★ 최종 학습 평가 성적표 ★\n\n" +
                                             "축하합니다! 인형뽑기부터 클래식 퀴즈 코스까지 전체 수료하셨습니다.\n\n" +
                                             $"■ 총 소요 시간: {totalPlayTime:F1}초\n" +
                                             $"■ 최종 성적 등급: [{calculatedGrade} 등급]\n\n" +
                                             "배운 개념을 활용하여 실전에 응용해 보십시오!";
                    
                    if (m_confirmButtonText != null)
                    {
                        m_confirmButtonText.text = "메인 화면으로";
                    }
                }
                else
                {
                    // 클래식 퀴즈 중간 퀴즈 성공 시 정답 알림 팝업 UI 세팅
                    m_descriptionText.text = "★ 정답입니다! ★\n\n올바른 정답을 선택하셨습니다.\n다음 문제로 이동해 보십시오!";

                    if (m_confirmButtonText != null)
                    {
                        m_confirmButtonText.text = "다음 문제로";
                    }
                }
            }
            else
            {
                // 실패(오답 또는 제한시간 만료) 시 UI 세팅
                if (m_confirmButton != null)
                {
                    m_confirmButton.gameObject.SetActive(true);
                }

                if (m_isTimeOverState)
                {
                    m_descriptionText.text = "★ 제한 시간이 초과되었습니다! ★\n\n" +
                                             "제한 시간이 모두 경과하여 퀴즈에 실패하셨습니다.\n" +
                                             "재수강(리플레이)을 진행하여 다시 도전해 보십시오!";

                    if (m_confirmButtonText != null)
                    {
                        m_confirmButtonText.text = "재수강 진행";
                    }
                }
                else
                {
                    m_descriptionText.text = "★ 틀린 오답입니다! ★\n\n아쉽게도 틀렸습니다. 다시 한번 기회를 드릴 테니 올바른 정답을 골라 보세요!";

                    if (m_confirmButtonText != null)
                    {
                        m_confirmButtonText.text = "계속하기";
                    }
                }
            }
        }

        private void HandleQuizSuccess()
        {
            Debug.Log("[QuizClassicResultPopupView] 정답 성공 이벤트 수신 -> 결과 패널 성공 모드 오픈.");
            func_ShowPopup(true);
        }

        private void HandleQuizFailed()
        {
            Debug.Log("[QuizClassicResultPopupView] 오답 실패 이벤트 수신 -> 결과 패널 실패 모드 오픈.");
            m_isTimeOverState = false;
            func_ShowPopup(false);
        }

        private void HandleTimeOver()
        {
            Debug.Log("[QuizClassicResultPopupView] 시간 초과 이벤트 수신 -> 결과 패널 실패(타임아웃) 모드 오픈.");
            m_isTimeOverState = true;
            func_ShowPopup(false);
        }

        private void func_OnConfirmButtonClick()
        {
            if (m_viewModel != null)
            {
                if (m_isSuccessState)
                {
                    if (m_viewModel.IsLastQuiz)
                    {
                        Debug.Log("[QuizClassicResultPopupView] 플레이어가 클래식 퀴즈 최종 완료 성적표를 확인하고 메인(Lobby)으로 이동합니다.");
                        if (m_playerSO != null)
                        {
                            m_playerSO.HasSavedPosition = true;
                            Debug.Log($"[QuizClassicResultPopupView] 로비로 돌아갈 때 마지막 복귀 위치 복원을 위해 HasSavedPosition 플래그를 true로 활성화했습니다. 위치: {m_playerSO.LastPosition}");
                        }

                        // 이지 트랜지션이 설정되어 있고 씬에 매니저가 존재하는 경우 전환 연출 연동
                        if (m_transitionSettings != null)
                        {
                            TransitionManager manager = FindFirstObjectByType<TransitionManager>();
                            if (manager != null)
                            {
                                TransitionManager.Instance().Transition("Lobby", m_transitionSettings, m_startDelay);
                            }
                            else
                            {
                                SceneManager.LoadScene("Lobby");
                            }
                        }
                        else
                        {
                            SceneManager.LoadScene("Lobby");
                        }
                    }
                    else
                    {
                        Debug.Log("[QuizClassicResultPopupView] 플레이어가 중간 퀴즈 정답을 확인하고 다음 문제 출제를 계속합니다.");
                        m_viewModel.ContinueAfterCorrectAnswer();
                    }
                }
                else
                {
                    if (!m_isTimeOverState)
                    {
                        Debug.Log("[QuizClassicResultPopupView] 플레이어가 오답 확인 후 '계속하기' 버튼을 클릭했습니다. 게임을 이어서 진행합니다.");
                        m_viewModel.ContinueAfterWrongAnswer();
                    }
                    else
                    {
                        Debug.Log("[QuizClassicResultPopupView] 플레이어가 제한 시간 초과 후 '재수강 진행' 동의 버튼을 클릭하여 리플레이를 수행합니다.");
                        m_viewModel.AcceptReTake();
                    }
                }
            }
            func_HidePopup();
        }

        #endregion
    }
}
