using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using DG.Tweening;
using GameArifiction.Core.Audio;
using GameArifiction.UI.Common;
using VContainer;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 게임 전체의 UI(버튼 입력, 텍스트 출력, 튜토리얼 다시보기 버튼 연동)를 담당하는 View
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-06
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: ISoundService를 주입받아 조작 버튼 클릭 시 Sfx_claw_touch 터치음 일괄 연동 적용
    /// </summary>
    public class ClawGameView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [Header("UI 제어 (Inspector)")]
        [SerializeField]
        [Tooltip("좌측 주행 제어 UI 버튼입니다.")]
        private Button m_leftButton;

        [SerializeField]
        [Tooltip("우측 주행 제어 UI 버튼입니다.")]
        private Button m_rightButton;

        [SerializeField]
        [Tooltip("집게 캐치(하강) UI 버튼입니다.")]
        private Button m_descendButton;

        [SerializeField]
        [Tooltip("집게 릴리즈(드랍) UI 버튼입니다.")]
        private Button m_dropButton;

        [SerializeField]
        [Tooltip("튜토리얼 팝업을 다시 볼 수 있는 버튼입니다.")]
        private Button m_showTutorialButton;

        [SerializeField]
        [Tooltip("공용 설정 팝업을 띄우는 UI 버튼입니다.")]
        private Button m_settingsButton;

        [SerializeField]
        [Tooltip("게임 일시정지를 제어하는 UI 버튼입니다.")]
        private Button m_pauseButton;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawView m_clawView;
        private ClawGameResultPopupView m_resultPopup;
        private CommonPausePopupView m_pausePopupView;

        private ClawGameViewModel m_viewModel;
        private ClawGameTutorialPopupView m_tutorialPopupView;
        private ClawGameQuizUI_View m_quizUIView;
        private CommonSettingsPopupView m_settingsPopupView;
        private ISoundService m_soundService;
        private float m_prevHorizontalInput;
        private bool m_isKeyboardControlling;
        #endregion

        #region 의존성 주입 (Dependency Injection)
        /// <summary>
        /// [기능]: VContainer를 통해 공통 사운드 서비스 및 씬 내 주요 하이어라키 뷰 의존성들을 일괄 주입받습니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 하이어라키 자동 등록 기법을 적용하여 인스펙터 결합도 제거 및 로직 간소화
        /// </summary>
        [Inject]
        public void Construct(
            ISoundService soundService, 
            CommonSettingsPopupView settingsPopupView,
            CommonPausePopupView pausePopupView,
            ClawView clawView,
            ClawGameResultPopupView resultPopup,
            ClawGameTutorialPopupView tutorialPopup,
            ClawGameQuizUI_View quizUIView)
        {
            m_soundService = soundService;
            m_settingsPopupView = settingsPopupView;
            m_pausePopupView = pausePopupView;
            m_clawView = clawView;
            m_resultPopup = resultPopup;
            m_tutorialPopupView = tutorialPopup;
            m_quizUIView = quizUIView;
        }
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Update()
        {
            HandleKeyboardInput();
        }
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: 뷰모델을 초기화하고 조작 버튼과 일시정지 팝업 상태 및 이벤트를 제어합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 의존성들이 VContainer에 의해 자동 주입되므로 뷰모델 매개변수만 전달받도록 최적화
        /// </summary>
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;
            
            // 이벤트 구독
            m_viewModel.OnRemoveDisagreeDollRequested += HandleRemoveDisagreeDoll;
            m_viewModel.OnStateChanged += UpdateButtonInteractions;

            if (m_tutorialPopupView != null)
            {
                m_tutorialPopupView.OnTutorialClosed += HandleTutorialClosed;
            }
            if (m_quizUIView != null)
            {
                m_quizUIView.OnQuizClosed += HandleQuizClosed;
            }
            if (m_resultPopup != null)
            {
                m_resultPopup.OnConfirmClicked += HandleConfirmClicked;
            }

            // [신규]: UI 버튼 클릭 이벤트 코드 바인딩 주입
            if (m_descendButton != null)
            {
                m_descendButton.onClick.AddListener(func_OnDescendButtonClick);
            }
            if (m_dropButton != null)
            {
                m_dropButton.onClick.AddListener(func_OnDropButtonClick);
            }
            if (m_showTutorialButton != null)
            {
                m_showTutorialButton.onClick.AddListener(func_OnShowTutorialButtonClick);
            }
            if (m_pauseButton != null)
            {
                m_pauseButton.onClick.AddListener(func_OnPauseButtonClick);
            }
            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.AddListener(func_OnSettingsButtonClick);
            }

            // [신규]: UI 좌우 이동 버튼 EventTrigger 기반 PointerDown/Up 동적 바인딩 주입 (타입 세이프 가동 보장)
            RegisterPointerEvent(m_leftButton, EventTriggerType.PointerDown, (data) => func_OnLeftButtonDown());
            RegisterPointerEvent(m_leftButton, EventTriggerType.PointerUp, (data) => func_OnMoveButtonUp());
            RegisterPointerEvent(m_rightButton, EventTriggerType.PointerDown, (data) => func_OnRightButtonDown());
            RegisterPointerEvent(m_rightButton, EventTriggerType.PointerUp, (data) => func_OnMoveButtonUp());

            // 하위 View 초기화
            if (m_clawView != null)
            {
                m_clawView.Initialize(m_viewModel);
            }

            // 초기 일시정지 팝업 강제 비활성화 (Late Awake 방지)
            if (m_pausePopupView != null)
            {
                m_pausePopupView.gameObject.SetActive(false);
            }

            // 초기 설정 팝업 강제 비활성화 및 닫기 이벤트 구독
            if (m_settingsPopupView != null)
            {
                m_settingsPopupView.OnClosePopup += func_OnSettingsClose;
                m_settingsPopupView.gameObject.SetActive(false);
            }

            // 초기 버튼 상호작용 상태 동기화
            UpdateButtonInteractions(m_viewModel.CurrentState);
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제 및 리스너 해제 (메모리 누수 방지 방어 코드)
            if (m_descendButton != null)
            {
                m_descendButton.onClick.RemoveListener(func_OnDescendButtonClick);
            }
            if (m_dropButton != null)
            {
                m_dropButton.onClick.RemoveListener(func_OnDropButtonClick);
            }
            if (m_showTutorialButton != null)
            {
                m_showTutorialButton.onClick.RemoveListener(func_OnShowTutorialButtonClick);
            }
            if (m_pauseButton != null)
            {
                m_pauseButton.onClick.RemoveListener(func_OnPauseButtonClick);
            }
            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.RemoveListener(func_OnSettingsButtonClick);
            }
            if (m_settingsPopupView != null)
            {
                m_settingsPopupView.OnClosePopup -= func_OnSettingsClose;
            }

            // EventTrigger 동적 바인딩 해제 (메모리 누수 차단)
            UnregisterPointerEvents(m_leftButton);
            UnregisterPointerEvents(m_rightButton);

            if (m_tutorialPopupView != null)
            {
                m_tutorialPopupView.OnTutorialClosed -= HandleTutorialClosed;
            }
            if (m_quizUIView != null)
            {
                m_quizUIView.OnQuizClosed -= HandleQuizClosed;
            }
            if (m_resultPopup != null)
            {
                m_resultPopup.OnConfirmClicked -= HandleConfirmClicked;
            }

            if (m_viewModel != null)
            {
                m_viewModel.OnRemoveDisagreeDollRequested -= HandleRemoveDisagreeDoll;
                m_viewModel.OnStateChanged -= UpdateButtonInteractions;
                m_viewModel.Dispose();
            }

        }

        private void RegisterPointerEvent(Button button, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            if (button == null)
            {
                return;
            }

            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void UnregisterPointerEvents(Button button)
        {
            if (button != null)
            {
                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    trigger.triggers.Clear();
                }
            }
        }




        #endregion

        #region UI 업데이트 로직 (Private Methods)

        /// <summary>
        /// [기능]: 뷰모델 상태 변화에 맞춰 모든 주행 및 조작 버튼들의 활성/비활성 인터랙션 상태를 실시간 제어합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateButtonInteractions(ClawStateType state)
        {
            if (m_viewModel == null)
            {
                return;
            }

            // A. 크레인이 Idle/Moving 등의 가동 준비 상태일 때만 조작 버튼 활성화
            bool isPlayable = state == ClawStateType.Idle || 
                              state == ClawStateType.MovingLeft || 
                              state == ClawStateType.MovingRight;

            if (m_leftButton != null)
            {
                m_leftButton.interactable = isPlayable;
            }
            if (m_rightButton != null)
            {
                m_rightButton.interactable = isPlayable;
            }
            if (m_descendButton != null)
            {
                m_descendButton.interactable = isPlayable;
            }

            // B. 릴리즈(놓기) 버튼은 오직 가동 대기 중이면서 집게가 닫혀있을 때만 특별 활성화 허용
            if (m_dropButton != null)
            {
                m_dropButton.interactable = isPlayable && m_viewModel.IsClawClosed;
            }
        }
        #endregion



        #region 키보드 입력 제어 (Private Methods)
        private void HandleKeyboardInput()
        {
            if (m_viewModel == null)
            {
                return;
            }

            // 일시정지 중(Time.timeScale == 0)일 때는 키보드 입력을 차단하여 오작동을 방지합니다.
            if (Mathf.Approximately(Time.timeScale, 0f))
            {
                return;
            }

            // New Input System 키보드 인스턴스 획득 (안전 널 체크)
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // [우선 처리]: 스페이스바 입력을 좌우 이동보다 먼저 평가하여 같은 프레임 내 상태 충돌 방지
            bool descendedThisFrame = false;
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                ClawStateType state = m_viewModel.CurrentState;

                // [규칙]: 캐치(하강/상승) 진행 중에는 전체 버튼 비활성화 (Idle 또는 이동 중일 때만 반응)
                if (state == ClawStateType.Idle ||
                    state == ClawStateType.MovingLeft ||
                    state == ClawStateType.MovingRight)
                {
                    // [규칙]: 집게 오므려짐(IsClawClosed) 상태에 따른 토글 (릴리즈 vs 캐치)
                    if (m_viewModel.IsClawClosed)
                    {
                        Debug.Log("[ClawGameView] 키보드 입력 감지: 릴리즈 실행 (스페이스바 놓기)");
                        if (m_soundService != null)
                        {
                            m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
                        }
                        m_viewModel.DropDoll();
                    }
                    else
                    {
                        // [수정]: 이동 중이든 정지 중이든 Idle/Moving 상태면 이동을 즉시 정지 후 하강 개시
                        if (state == ClawStateType.MovingLeft || state == ClawStateType.MovingRight)
                        {
                            m_viewModel.StopMove();
                            m_isKeyboardControlling = false;
                        }
                        Debug.Log("[ClawGameView] 키보드 입력 감지: 캐치 개시 (스페이스바 하강)");
                        if (m_soundService != null)
                        {
                            m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
                        }
                        m_viewModel.DescendClaw();
                        descendedThisFrame = true;
                    }
                }
            }

            // [하강 확정 프레임 가드]: 방금 하강이 확정된 프레임에서는 좌우 입력을 완전히 차단하여
            // 같은 프레임 내 MovingLeft/Right 전환에 의한 하강 토큰 취소 레이스 컨디션을 방지
            if (descendedThisFrame)
            {
                m_prevHorizontalInput = 0f;
                return;
            }

            // 1. 좌우 키보드 입력 감지 (A/D, 좌우 방향키)
            // [가드]: 하강/그랩/상승 등 비조작 상태에서는 이동 입력 차단
            ClawStateType currentState = m_viewModel.CurrentState;
            if (currentState != ClawStateType.Idle &&
                currentState != ClawStateType.MovingLeft &&
                currentState != ClawStateType.MovingRight)
            {
                m_prevHorizontalInput = 0f;
                return;
            }

            float horizontal = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal = -1f;
            }
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal = 1f;
            }

            if (Mathf.Abs(horizontal) > 0.1f)
            {
                m_isKeyboardControlling = true;

                // 입력 방향의 부호가 달라질 때만 1회 호출 (오버헤드 방지)
                if (!Mathf.Approximately(Mathf.Sign(horizontal), Mathf.Sign(m_prevHorizontalInput)) || Mathf.Abs(m_prevHorizontalInput) < 0.1f)
                {
                    if (horizontal > 0f)
                    {
                        Debug.Log("[ClawGameView] 키보드 입력 감지: 우측 이동");
                        m_viewModel.StartMoveRight();
                    }
                    else
                    {
                        Debug.Log("[ClawGameView] 키보드 입력 감지: 좌측 이동");
                        m_viewModel.StartMoveLeft();
                    }
                }
            }
            else
            {
                // 키보드를 떼어 입력이 0이 되는 순간 1회만 Stop
                if (m_isKeyboardControlling && Mathf.Abs(m_prevHorizontalInput) > 0.1f)
                {
                    Debug.Log("[ClawGameView] 키보드 입력 감지: 이동 정지");
                    m_viewModel.StopMove();
                    m_isKeyboardControlling = false;
                }
            }

            m_prevHorizontalInput = horizontal;
        }
        #endregion

        #region UI 이벤트 콜백 (Public Methods)
        // Event Trigger 컴포넌트의 PointerDown 이벤트에 연결
        public void func_OnLeftButtonDown()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }
            if (m_viewModel != null)
            {
                m_viewModel.StartMoveLeft();
            }
        }

        // Event Trigger 컴포넌트의 PointerUp 이벤트에 연결
        public void func_OnMoveButtonUp()
        {
            if (m_viewModel != null)
            {
                m_viewModel.StopMove();
            }
        }

        // Event Trigger 컴포넌트의 PointerDown 이벤트에 연결
        public void func_OnRightButtonDown()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }
            if (m_viewModel != null)
            {
                m_viewModel.StartMoveRight();
            }
        }

        // Button의 OnClick 이벤트에 연결
        public void func_OnDescendButtonClick()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }
            if (m_viewModel != null)
            {
                m_viewModel.DescendClaw();
            }
        }

        // Button의 OnClick 이벤트에 연결 (도중 놓기)
        public void func_OnDropButtonClick()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }
            if (m_viewModel != null)
            {
                m_viewModel.DropDoll();
            }
        }

        // Button의 OnClick 이벤트에 연결 (튜토리얼 다시보기)
        public void func_OnShowTutorialButtonClick()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }
            if (m_tutorialPopupView != null)
            {
                m_tutorialPopupView.func_ShowTutorial();
            }
        }

        /// <summary>
        /// [기능]: 일시정지 버튼 클릭 시 호출되는 콜백으로, 게임을 정지하고 일시정지 공통 팝업을 표시합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-11
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 비활성화된 부모 UI 계층 강제 활성화 가드 코드 추가 및 타임스케일 대입 순서 최적화
        /// </summary>
        public void func_OnPauseButtonClick()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }

            if (m_pausePopupView != null)
            {
                // [부모 레이어 렌더 가드 최적화]: 비활성 상태인 최상위 부모 노드 단 1개만 활성화하여 렉 방지
                Transform parentNode = m_pausePopupView.transform.parent;
                Transform deepestInactiveParent = null;

                while (parentNode != null)
                {
                    if (!parentNode.gameObject.activeSelf)
                    {
                        deepestInactiveParent = parentNode;
                    }
                    parentNode = parentNode.parent;
                }

                if (deepestInactiveParent != null)
                {
                    deepestInactiveParent.gameObject.SetActive(true);
                    Debug.Log($"[ClawGameView] 가장 상위의 비활성 부모 UI 오브젝트를 활성화하여 계층 구조를 켰습니다: {deepestInactiveParent.name}");
                }

                CommonPausePopupDataDTO pauseData = new CommonPausePopupDataDTO
                {
                    OnResume = () =>
                    {
                        Time.timeScale = 1f;
                    },
                    OnReplayTutorial = () =>
                    {
                        if (m_tutorialPopupView != null)
                        {
                            m_tutorialPopupView.func_ShowTutorial();
                        }
                    },
                    OnReplayQuiz = () =>
                    {
                        if (m_quizUIView != null)
                        {
                            m_quizUIView.func_OnShowQuizButtonClick();
                        }
                    }
                };

                m_pausePopupView.Setup(pauseData);
                m_pausePopupView.ShowPopup();

                // [핵심 해결책]: 팝업이 활성화된 즉시 UI 레이아웃과 캔버스 버퍼의 강제 업데이트 동기화 수행
                Canvas.ForceUpdateCanvases();

                Debug.Log("[ClawGameView] 게임을 일시정지하고 일시정지 팝업을 즉시 동기화하여 활성화했습니다.");
            }
            else
            {
                Debug.LogWarning("[ClawGameView] CommonPausePopupView 의존성이 주입되지 않아 일시정지 팝업을 표시할 수 없습니다.");
            }

            // 타임 스케일을 최종 정지
            Time.timeScale = 0f;
        }

        /// <summary>
        /// [기능]: 설정 버튼 클릭 시 호출되는 콜백으로, 게임을 정지하고 공통 설정 팝업을 표시합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        public void func_OnSettingsButtonClick()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }

            if (m_settingsPopupView != null)
            {
                // [부모 레이어 렌더 가드 최적화]: 비활성 상태인 최상위 부모 노드 단 1개만 활성화하여 렉 방지
                Transform parentNode = m_settingsPopupView.transform.parent;
                Transform deepestInactiveParent = null;

                while (parentNode != null)
                {
                    if (!parentNode.gameObject.activeSelf)
                    {
                        deepestInactiveParent = parentNode;
                    }
                    parentNode = parentNode.parent;
                }

                if (deepestInactiveParent != null)
                {
                    deepestInactiveParent.gameObject.SetActive(true);
                    Debug.Log($"[ClawGameView] 가장 상위의 비활성 부모 UI 오브젝트를 활성화하여 계층 구조를 켰습니다: {deepestInactiveParent.name}");
                }

                m_settingsPopupView.ShowPopup();

                // 팝업이 활성화된 즉시 UI 레이아웃과 캔버스 버퍼의 강제 업데이트 동기화 수행
                Canvas.ForceUpdateCanvases();

                Debug.Log("[ClawGameView] 게임을 일시정지하고 공통 설정 팝업을 즉시 동기화하여 활성화했습니다.");
            }
            else
            {
                Debug.LogWarning("[ClawGameView] CommonSettingsPopupView 의존성이 주입되지 않아 설정 팝업을 표시할 수 없습니다.");
            }

            // 타임 스케일을 최종 정지
            Time.timeScale = 0f;
        }

        /// <summary>
        /// [기능]: 설정 팝업이 닫힐 때 호출되며, 게임 일시정지를 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        private void func_OnSettingsClose()
        {
            Time.timeScale = 1f;
            Debug.Log("[ClawGameView] 공통 설정 팝업이 닫혀 게임 일시정지를 해제했습니다.");
        }
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        /// <summary>
        /// [기능]: 결과 팝업에서 확인 완료가 감지되었을 때 호출되며, 인형뽑기 뷰 화면을 스스로 비활성화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleConfirmClicked()
        {
            gameObject.SetActive(false);
            Debug.Log("[ClawGameView] 결과 확인 완료 이벤트를 수신하여 화면을 자가 비활성화했습니다.");
        }

        /// <summary>
        /// [기능]: 튜토리얼 팝업이 닫힐 때 호출되며, 게임 상태를 체크하여 일시정지를 해제합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleTutorialClosed()
        {
            if (m_viewModel == null)
            {
                return;
            }

            if (m_viewModel.CurrentState != ClawStateType.Tutorial && m_viewModel.CurrentState != ClawStateType.QuizReveal)
            {
                Time.timeScale = 1f;
                Debug.Log("[ClawGameView] 튜토리얼 다시보기가 닫혀 게임 일시정지를 해제했습니다.");
            }
        }

        /// <summary>
        /// [기능]: 퀴즈 팝업이 닫힐 때 호출되며, 게임 상태를 체크하여 일시정지를 해제합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleQuizClosed()
        {
            if (m_viewModel == null)
            {
                return;
            }

            if (m_viewModel.CurrentState != ClawStateType.Tutorial && m_viewModel.CurrentState != ClawStateType.QuizReveal)
            {
                Time.timeScale = 1f;
                Debug.Log("[ClawGameView] 퀴즈 다시보기가 닫혀 게임 일시정지를 해제했습니다.");
            }
        }

        /// <summary>
        /// [기능]: 재수강 수락 시 뷰모델로부터 이벤트를 수신하여 집게 위치를 원복하고 오답 캡슐 1개를 무작위 제거합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleRemoveDisagreeDoll()
        {
            // [집게 위치 초기화]: 재시도가 트리거되었으므로 카트와 집게의 물리 상태를 원점 복원시킵니다.
            if (m_clawView != null)
            {
                m_clawView.ResetClawToInitialState();
            }

            // [수정]: 기존의 재수강 시 오답 캡슐 1개 무작위 삭제(난이도 완화) 로직을 전면 제거합니다.
            Debug.Log("[ClawGameView] 재수강 난이도 완화 캡슐 제거 기능이 생략되었습니다.");
        }


        #endregion
    }
}
