using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameArifiction.QuizClassic;

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
        private IQuizGameViewModel m_viewModel;
        private TextMeshProUGUI m_confirmButtonText;
        private TextMeshProUGUI m_cancelButtonText;
        private bool m_isSuccessState;
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
                if (activeSceneName == "CardGame")
                {
                    // A-1. 클래식 퀴즈 성공(성적표 확인) 시 UI 세팅
                    m_descriptionText.text = "★ 최종 학습 평가 성적표 ★\n\n축하합니다! 클래식 객관식 퀴즈 코스를 우수하게 수료하셨습니다.\n\n최종 평가 결과: [이수 완료]\n배운 개념을 활용하여 실전에 응용해 보십시오!";
                    
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
            func_ShowPopup(false);
        }

        private void HandleTimeOver()
        {
            Debug.Log("[ClawGameResultPopupView] 시간 초과 이벤트 수신 -> 결과 패널 실패 모드 오픈.");
            func_ShowPopup(false);
        }

        private void func_OnConfirmButtonClick()
        {
            if (m_viewModel != null)
            {
                if (m_isSuccessState)
                {
                    string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (activeSceneName == "CardGame")
                    {
                        Debug.Log("[ClawGameResultPopupView] 플레이어가 클래식 퀴즈 성적표를 확인하고 메인(Lobby)으로 이동합니다.");
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
