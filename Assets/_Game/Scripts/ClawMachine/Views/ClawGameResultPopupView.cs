using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using GameArifiction.QuizClassic;
using GameArifiction.Player;
using GameArifiction.Core.Audio;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 게임의 최종 정답 성공 또는 오답/시간 초과 실패 결과를 출력하는 결과 패널 UI View
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-06
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: ISoundService를 주입받아 게임 성공 시 Sfx_claw_clear 재생 및 버튼 클릭 시 터치음 일괄 연동 적용
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

        [Header("결과 상태 아이콘 연출 (스프라이트 교체 방식)")]
        [SerializeField]
        [Tooltip("정/오답 상태 아이콘을 표시할 단일 Image 컴포넌트입니다.")]
        private Image m_statusIconImage;

        [SerializeField]
        [Tooltip("정답(성공) 판정 시 표시할 스프라이트 에셋입니다.")]
        private Sprite m_correctSprite;

        [SerializeField]
        [Tooltip("오답/실패 판정 시 표시할 스프라이트 에셋입니다.")]
        private Sprite m_incorrectSprite;
        #endregion

        #region 내부 필드 (Private Fields)
        [Inject]
        public PlayerSO PlayerSO { get; set; }

        [Inject]
        public QuizClassicFlowController QuizFlowController { get; set; }

        [Inject]
        public QuizClassicView QuizClassicViewInstance { get; set; }

        [Inject]
        public ClawGameView ClawGameViewInstance { get; set; }

        [Inject]
        public ClawSceneReferencesDTO SceneReferences { get; set; }

        [Inject]
        public ISoundService SoundService { get; set; }

        private IQuizGameViewModel m_viewModel;
        private TextMeshProUGUI m_confirmButtonText;
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

        public void func_HidePopup()
        {
            gameObject.SetActive(false);
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 성공과 실패 상태 분기에 부합하는 안내문 출력 및 버튼 컴포넌트의 가시성/워딩 동적 세팅을 수행합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-06
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 정오답 결과를 단일 Image의 Sprite 교체 방식으로 연출하도록 수정
        /// </summary>
        private void UpdatePanelContent(bool isSuccess)
        {
            if (m_viewModel == null || m_descriptionText == null)
            {
                return;
            }

            // 단일 Image 컴포넌트의 Sprite 교체 방식 적용 (Canvas Rebuild 오버헤드 원천 차단)
            if (m_statusIconImage != null)
            {
                Sprite targetSprite = isSuccess ? m_correctSprite : m_incorrectSprite;
                if (targetSprite != null)
                {
                    m_statusIconImage.sprite = targetSprite;
                    m_statusIconImage.gameObject.SetActive(true);
                }
                else
                {
                    m_statusIconImage.gameObject.SetActive(false);
                }
            }

            if (isSuccess)
            {
                // A. 인형뽑기 집게 퀴즈 성공 시 UI 세팅 및 성적 판정 계산
                float elapsedClawTime = Mathf.Max(0f, 120f - m_viewModel.TimeLeft);
                MinigameGrade calculatedGrade = MinigameGrade.D;

                // 성적 판정 기준 (120초 기준 소요 시간 기준):
                // A - 60초 내
                // B - 70~80초 사이 (60초 초과 80초 이하)
                // C - 90~100초 사이 (80초 초과 100초 이하)
                // D - 110~120초 사이 (100초 초과 120초 이하)
                // F - 시간 초과 (120초 초과)
                if (elapsedClawTime <= 60f)
                {
                    calculatedGrade = MinigameGrade.A;
                }
                else if (elapsedClawTime <= 80f)
                {
                    calculatedGrade = MinigameGrade.B;
                }
                else if (elapsedClawTime <= 100f)
                {
                    calculatedGrade = MinigameGrade.C;
                }
                else if (elapsedClawTime <= 120f)
                {
                    calculatedGrade = MinigameGrade.D;
                }
                else
                {
                    calculatedGrade = MinigameGrade.F;
                }

                if (PlayerSO != null)
                {
                    // PlayerSO 데이터에 저장 반영
                    PlayerSO.SetMinigameGrade("ClawMachineQuiz", calculatedGrade);
                }

                m_descriptionText.text = "정답\n정답이다";
                
                if (m_confirmButtonText != null)
                {
                    m_confirmButtonText.text = "다음으로";
                }
            }
            else
            {
                // B. 실패(오답 또는 제한시간 만료) 시 UI 세팅
                if (m_confirmButton != null)
                {
                    m_confirmButton.gameObject.SetActive(true);
                }

                m_descriptionText.text = "재수강\n재수강이다";

                if (m_confirmButtonText != null)
                {
                    m_confirmButtonText.text = "재수강";
                }
            }
        }

        private void HandleQuizSuccess()
        {
            Debug.Log("[ClawGameResultPopupView] 정답 성공 이벤트 수신 -> 결과 패널 성공 모드 오픈.");
            if (SoundService != null)
            {
                SoundService.PlaySFX(SoundDefine.Sfx_claw_clear);
            }
            func_ShowPopup(true);
        }

        private void HandleQuizFailed()
        {
            Debug.Log("[ClawGameResultPopupView] 오답 실패 이벤트 수신 -> 결과 패널 실패 모드 오픈.");
            func_ShowPopup(false);
        }

        private void HandleTimeOver()
        {
            Debug.Log("[ClawGameResultPopupView] 시간 초과 이벤트 수신 -> 결과 패널 실패(타임아웃) 모드 오픈.");
            func_ShowPopup(false);
        }

        private void func_OnConfirmButtonClick()
        {
            if (SoundService != null)
            {
                SoundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }

            if (m_viewModel != null)
            {
                if (m_isSuccessState)
                {
                    Debug.Log("[ClawGameResultPopupView] 플레이어가 '다음 단계로' 버튼을 선택하여 클래식 퀴즈 뷰를 활성화합니다.");
                    
                    // 1. 클래식 퀴즈 뷰 활성화
                    if (QuizClassicViewInstance != null)
                    {
                        QuizClassicViewInstance.gameObject.SetActive(true);
                        Debug.Log("[ClawGameResultPopupView] QuizClassicView 오브젝트를 성공적으로 활성화했습니다.");
                    }
                    else
                    {
                        Debug.LogError("[ClawGameResultPopupView] 주입받은 QuizClassicViewInstance가 null입니다.");
                    }

                    if (QuizFlowController != null)
                    {
                        QuizFlowController.StartClassicQuiz();
                    }
                    else
                    {
                        Debug.LogError("[ClawGameResultPopupView] QuizClassicFlowController 의존성이 주입되지 않았습니다.");
                    }

                    // 2. 현재 인형뽑기 메인 뷰 비활성화
                    if (ClawGameViewInstance != null)
                    {
                        ClawGameViewInstance.gameObject.SetActive(false);
                    }
                    else
                    {
                        Debug.LogWarning("[ClawGameResultPopupView] 주입받은 ClawGameViewInstance가 null입니다.");
                    }

                    // 3. 인형뽑기 3D 물리 공간 기기 오브젝트 비활성화
                    if (SceneReferences != null && SceneReferences.ClawMachineWorld != null)
                    {
                        SceneReferences.ClawMachineWorld.SetActive(false);
                    }
                    else
                    {
                        Debug.LogWarning("[ClawGameResultPopupView] 주입받은 SceneReferences 또는 ClawMachineWorld가 null입니다.");
                    }
                }
                else
                {
                    Debug.Log("[ClawGameResultPopupView] 플레이어가 재수강 버튼을 클릭하여 리플레이를 수행합니다.");
                    m_viewModel.AcceptReTake();
                }
            }
            func_HidePopup();
        }

        #endregion
    }
}
