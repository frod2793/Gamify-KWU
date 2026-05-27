using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameArifiction.QuizClassic;
using GameArifiction.Player;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 게임의 최종 정답 성공 또는 오답/시간 초과 실패 결과를 출력하는 결과 패널 UI View
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-26
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 클래스명을 ClawGameResultPopupView로 격상하고 버튼 변수명을 m_confirmButton / m_cancelButton으로 역할 맞춤 리팩토링 완료
    /// </summary>
    public class ClawGameResultPopupView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("결과 설명 및 재수강 패널티 정보를 보여줄 TextMeshProUGUI 컴포넌트입니다.")]
        private TextMeshProUGUI m_descriptionText;

        [SerializeField]
        [Tooltip("결과 확인(다음 단계 혹은 재수강 진행) 버튼입니다.")]
        private Button m_confirmButton;

        [SerializeField]
        [Tooltip("결과 취소(게임 종료) 버튼입니다.")]
        private Button m_cancelButton;
        #endregion

        #region 내부 필드 (Private Fields)
        [Header("세션 데이터")]
        [SerializeField]
        [Tooltip("씬 간 플레이어 위치 상태 보존을 위한 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;

        private IQuizGameViewModel m_viewModel;
        private TextMeshProUGUI m_confirmButtonText;
        private TextMeshProUGUI m_cancelButtonText;
        private bool m_isSuccessState;
        private bool m_isTimeOverState;
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(IQuizGameViewModel viewModel)
        {
            m_viewModel = viewModel;
            
            // TMProUGUI 하위 텍스트 컴포넌트 캐싱
            if (m_confirmButton != null)
            {
                m_confirmButtonText = m_confirmButton.GetComponentInChildren<TextMeshProUGUI>();
                m_confirmButton.onClick.AddListener(func_OnConfirmButtonClick);
            }
            if (m_cancelButton != null)
            {
                m_cancelButtonText = m_cancelButton.GetComponentInChildren<TextMeshProUGUI>();
                m_cancelButton.onClick.AddListener(func_OnCancelButtonClick);
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
            if (m_cancelButton != null)
            {
                m_cancelButton.onClick.RemoveListener(func_OnCancelButtonClick);
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
                string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (m_viewModel is QuizClassicViewModel)
                {
                    // A-1. 클래식 퀴즈 성공(성적표 확인) 시 UI 세팅 및 성적 판정 계산
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
                    // A-2. 인형뽑기 집게 퀴즈 성공 시 UI 세팅
                    m_descriptionText.text = "★ 축하합니다! 정답입니다 ★\n\n지정된 퀴즈의 정답 캡슐을 골인시켰습니다.\n성공적으로 과정을 완료하였습니다!";
                    
                    if (m_confirmButtonText != null)
                    {
                        m_confirmButtonText.text = "다음 단계로";
                    }
                }
                
                // 성공 상태에서는 거절 버튼 은폐
                if (m_cancelButton != null)
                {
                    m_cancelButton.gameObject.SetActive(false);
                }
            }
            else
            {
                // B. 실패(오답 또는 제한시간 만료) 시 UI 세팅
                if (m_viewModel is QuizClassicViewModel)
                {
                    // 클래식 퀴즈 실패 UI (오답 또는 시간 초과 판정 구체화)
                    if (m_isTimeOverState)
                    {
                        m_descriptionText.text = "제한 시간이 초과되었습니다!\n\n제한 시간 마진 내에 문제를 해결하지 못해 실패하셨습니다.\n다시 한번 도전하여 학습 평가 코스를 수료해 보십시오!";
                    }
                    else
                    {
                        m_descriptionText.text = "틀린 오답을 선택하셨습니다!\n\n오답으로 인해 스테이지 수료에 실패하셨습니다.\n다시 한번 개념을 곱씹으며 재도전해 보십시오!";
                    }

                    if (m_confirmButtonText != null)
                    {
                        m_confirmButtonText.text = "재수강 진행";
                    }
                    if (m_cancelButtonText != null)
                    {
                        m_cancelButtonText.text = "학습 종료";
                    }
                }
                else
                {
                    // 인형뽑기 집게 퀴즈 실패 UI
                    int currentPenaltySeconds = (m_viewModel.ReTakeCount + 1) * 20;
                    int nextTimeLimit = 120 - currentPenaltySeconds;
                    if (nextTimeLimit < 20)
                    {
                        nextTimeLimit = 20;
                    }

                    m_descriptionText.text = "오답 혹은 제한 시간이 초과되었습니다!\n재수강(리플레이)을 신청하시겠습니까?\n\n" +
                                             $"[혜택] 방해 캡슐 '동의 안 함' 1개 제거\n" +
                                             $"[패널티] 제한 시간 {currentPenaltySeconds}초 차감 (다음 판: {nextTimeLimit}초)";

                    if (m_confirmButtonText != null)
                    {
                        m_confirmButtonText.text = "재수강 진행";
                    }
                    if (m_cancelButtonText != null)
                    {
                        m_cancelButtonText.text = "동의 안 함 (종료)";
                    }
                }

                if (m_cancelButton != null)
                {
                    m_cancelButton.gameObject.SetActive(true);
                }
            }
        }

        private void HandleQuizSuccess()
        {
            Debug.Log("[ClawGameResultPopupView] 정답 성공 이벤트 수신 -> 결과 패널 성공 모드 오픈.");
            func_ShowPopup(true);
        }

        private void HandleQuizFailed()
        {
            Debug.Log("[ClawGameResultPopupView] 오답 실패 이벤트 수신 -> 결과 패널 실패 모드 오픈.");
            m_isTimeOverState = false;
            func_ShowPopup(false);
        }

        private void HandleTimeOver()
        {
            Debug.Log("[ClawGameResultPopupView] 시간 초과 이벤트 수신 -> 결과 패널 실패(타임아웃) 모드 오픈.");
            m_isTimeOverState = true;
            func_ShowPopup(false);
        }

        private void func_OnConfirmButtonClick()
        {
            if (m_viewModel != null)
            {
                if (m_isSuccessState)
                {
                    string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (m_viewModel is QuizClassicViewModel)
                    {
                        Debug.Log("[ClawGameResultPopupView] 플레이어가 클래식 퀴즈 성적표를 확인하고 메인(Lobby)으로 이동합니다.");
                        if (m_playerSO != null)
                        {
                            m_playerSO.HasSavedPosition = true;
                            Debug.Log($"[ClawGameResultPopupView] 로비로 돌아갈 때 마지막 복귀 위치 복원을 위해 HasSavedPosition 플래그를 true로 활성화했습니다. 위치: {m_playerSO.LastPosition}");
                        }
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
                    }
                    else
                    {
                        Debug.Log("[ClawGameResultPopupView] 플레이어가 '다음 단계로' 버튼을 선택하여 동일 씬 내의 클래식 퀴즈 팝업/뷰를 활성화합니다.");
                        
                        // 1. 클래식 퀴즈 이니셜라이저 활성화 (비활성화 상태 탐색 포함)
                        QuizClassicInitializer classicInitializer = FindFirstObjectByType<QuizClassicInitializer>(FindObjectsInactive.Include);
                        if (classicInitializer != null)
                        {
                            classicInitializer.gameObject.SetActive(true);
                            
                            // [수정]: 유령 타이머 방지를 위해 m_initializeOnStart가 false로 되어있으므로, 명시적으로 수동 개시 호출
                            classicInitializer.InitializeClassicQuiz();
                            
                            // 이니셜라이저 산하의 클래식 뷰도 활성화 보장
                            QuizClassicView classicView = FindFirstObjectByType<QuizClassicView>(FindObjectsInactive.Include);
                            if (classicView != null)
                            {
                                classicView.gameObject.SetActive(true);
                            }
                        }
                        else
                        {
                            // 폴백: 이니셜라이저 유실 대비 뷰 다이렉트 활성화
                            QuizClassicView classicView = FindFirstObjectByType<QuizClassicView>(FindObjectsInactive.Include);
                            if (classicView != null)
                            {
                                classicView.gameObject.SetActive(true);
                            }
                        }

                        // 2. 현재 인형뽑기 메인 뷰 비활성화
                        ClawGameView clawGameView = FindFirstObjectByType<ClawGameView>(FindObjectsInactive.Include);
                        if (clawGameView != null)
                        {
                            clawGameView.gameObject.SetActive(false);
                        }

                        // 3. 인형뽑기 3D 물리 공간 기기 오브젝트 비활성화
                        GameObject clawWorld = GameObject.Find("ClawMachine_World");
                        if (clawWorld != null)
                        {
                            clawWorld.SetActive(false);
                        }
                    }
                }
                else
                {
                    Debug.Log("[ClawGameResultPopupView] 플레이어가 '재수강 진행' 동의 버튼을 클릭하여 리플레이를 수행합니다.");
                    m_viewModel.AcceptReTake();
                }
            }
            func_HidePopup();
        }

        private void func_OnCancelButtonClick()
        {
            if (m_viewModel != null && !m_isSuccessState)
            {
                Debug.Log("[ClawGameResultPopupView] 플레이어가 재수강 거절 버튼을 클릭했습니다.");
                m_viewModel.RejectReTake();
            }
            func_HidePopup();
        }
        #endregion
    }
}
