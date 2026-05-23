using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
        [Tooltip("남은 플레이 횟수를 표시할 Text UI 컴포넌트입니다.")]
        private Text m_playCountText;

        [SerializeField]
        [Tooltip("제한 시간을 표시할 Text UI 컴포넌트입니다. (시간 제한 모드에서 사용)")]
        private Text m_timeText; // 시간 제한이 있을 경우 사용

        [SerializeField]
        [Tooltip("좌우 주행과 와이어 길이를 제어하는 천장 카트 View 객체입니다.")]
        private ClawView m_clawView;
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
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;
            
            // 이벤트 구독
            m_viewModel.OnPlayCountChanged += UpdatePlayCountUI;
            m_viewModel.OnTimeChanged += UpdateTimeUI;

            // 하위 View 초기화
            if (m_clawView != null)
            {
                m_clawView.Initialize(m_viewModel);
            }
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnPlayCountChanged -= UpdatePlayCountUI;
                m_viewModel.OnTimeChanged -= UpdateTimeUI;
                m_viewModel.Dispose();
            }
        }
        #endregion

        #region UI 업데이트 로직 (Private Methods)
        private void UpdatePlayCountUI(int count)
        {
            if (m_playCountText != null)
            {
                m_playCountText.text = $"남은 횟수: {count}";
            }
        }

        private void UpdateTimeUI(float time)
        {
            if (m_timeText != null)
            {
                m_timeText.text = $"남은 시간: {Mathf.CeilToInt(time)}";
            }
        }
        #endregion

        #region 키보드 입력 제어 (Private Methods)
        private void HandleKeyboardInput()
        {
            if (m_viewModel == null) return;

            // New Input System 키보드 인스턴스 획득 (안전 널 체크)
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // 1. 좌우 키보드 입력 감지 (A/D, 좌우 방향키)
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
            // 2. 스페이스바 입력 감지 (캐치 또는 릴리즈)
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
                    else if (state == ClawStateType.Idle)
                    {
                        // [규칙]: 캐치는 반드시 정지 상태(Idle)에서만 시작 가능
                        Debug.Log("[ClawGameView] 키보드 입력 감지: 캐치 개시 (스페이스바 하강)");
                        m_viewModel.DescendClaw();
                    }
                }
            }
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
    }
}
