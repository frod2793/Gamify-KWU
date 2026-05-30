using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 게임 전체의 UI(버튼 입력, 텍스트 출력)를 담당하는 View
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-22
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 스페이스바 연속 입력 시 집게 펴기(Release) 및 드랍 분기 로직 고도화 (IsClawClosed 검증 반영)
    /// </summary>
    public class ClawGameView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("좌우 주행과 와이어 길이를 제어하는 천장 카트 View 객체입니다.")]
        private ClawView m_clawView;



        [SerializeField]
        [Tooltip("최종 정답/실패 결과를 처리할 팝업 View 컴포넌트입니다.")]
        private ClawGameResultPopupView m_resultPopup;


        [Header("UI Controls (Inspector)")]
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
        #endregion


        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private float m_prevHorizontalInput;
        private bool m_isKeyboardControlling;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Update()
        {
            HandleKeyboardInput();
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel, ClawGameResultPopupView resultPopup)
        {
            m_viewModel = viewModel;
            m_resultPopup = resultPopup;
            
            // 이벤트 구독
            m_viewModel.OnRemoveDisagreeDollRequested += HandleRemoveDisagreeDoll;
            m_viewModel.OnStateChanged += UpdateButtonInteractions;

            // [신규]: UI 버튼 클릭 이벤트 코드 바인딩 주입
            if (m_descendButton != null)
            {
                m_descendButton.onClick.AddListener(func_OnDescendButtonClick);
            }
            if (m_dropButton != null)
            {
                m_dropButton.onClick.AddListener(func_OnDropButtonClick);
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

            // EventTrigger 동적 바인딩 해제 (메모리 누수 차단)
            UnregisterPointerEvents(m_leftButton);
            UnregisterPointerEvents(m_rightButton);

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
            if (m_viewModel != null)
            {
                m_viewModel.StartMoveRight();
            }
        }

        // Button의 OnClick 이벤트에 연결
        public void func_OnDescendButtonClick()
        {
            if (m_viewModel != null)
            {
                m_viewModel.DescendClaw();
            }
        }

        // Button의 OnClick 이벤트에 연결 (도중 놓기)
        public void func_OnDropButtonClick()
        {
            if (m_viewModel != null)
            {
                m_viewModel.DropDoll();
            }
        }
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        /// <summary>
        /// [기능]: 재수강 수락 시 뷰모델로부터 이벤트를 수신하여 집게 위치를 원복하고 오답 캡슐 1개를 무작위 제거합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleRemoveDisagreeDoll()
        {
            // [집게 위치 초기화]: 재시도가 트리거되었으므로 카트와 집게의 물리 상태를 원점 복원시킵니다.
            if (m_clawView != null)
            {
                m_clawView.func_ResetClawToInitialState();
            }

            ClawMachineDollView[] dolls = FindObjectsByType<ClawMachineDollView>(FindObjectsSortMode.None);
            if (dolls == null || dolls.Length == 0)
            {
                Debug.LogWarning("[ClawGameView] 씬에 인형이 존재하지 않아 난이도 완화 혜택을 적용할 수 없습니다.");
                return;
            }

            // [오답 캡슐 수집]: IsCorrect가 false인 방해 캡슐들만 모아 혜택 타겟으로 적용합니다.
            System.Collections.Generic.List<ClawMachineDollView> wrongDolls = new System.Collections.Generic.List<ClawMachineDollView>();
            for (int i = 0; i < dolls.Length; i++)
            {
                ClawMachineDollView doll = dolls[i];
                if (doll != null && !doll.IsCorrect)
                {
                    wrongDolls.Add(doll);
                }
            }

            if (wrongDolls.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, wrongDolls.Count);
                ClawMachineDollView targetDoll = wrongDolls[randomIndex];
                if (targetDoll != null)
                {
                    string targetId = targetDoll.DollId;
                    Destroy(targetDoll.gameObject);
                    Debug.Log($"[ClawGameView] 재수강 난이도 완화 적용 완료: 방해(오답) 캡슐({targetId}) 1개를 제거했습니다.");
                }
            }
            else
            {
                Debug.Log("[ClawGameView] 제거할 방해(오답) 캡슐이 씬에 더 이상 존재하지 않습니다.");
            }
        }
        #endregion
    }
}
