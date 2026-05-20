using System;
using UnityEngine;
using GameArifiction.DTO;

namespace GameArifiction.Claw
{
    /// <summary>
    /// [기능]: 집게 기계장치의 물리 속도 갱신, 수평 이동 연산 및 상태 머신(FSM) 제어를 담당하는 뷰모델 (POCO)
    /// [작성자]: [Senior Client Developer]
    /// </summary>
    public class ClawViewModel
    {
        #region 내부 필드 (Private Fields)

        private readonly ClawModel m_model;
        private ClawState m_currentState;
        private Vector2 m_position;
        private float m_currentInputDirection;
        private bool m_isHolding;

        #endregion

        #region 이벤트 (Events)

        public event Action<ClawState> OnStateChanged;
        public event Action<Vector2> OnPositionChanged;
        public event Action<float, float, bool> OnMotorTriggered; // (속도, 최대토크, useMotor 여부)

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public ClawState CurrentState
        {
            get => m_currentState;
            private set
            {
                if (m_currentState != value)
                {
                    m_currentState = value;
                    OnStateChanged?.Invoke(m_currentState);
                }
            }
        }

        /// <summary>
        /// [기능]: 현재 집게가 캡슐을 잡아서 들고 있는지 여부를 판별합니다.
        /// </summary>
        public bool IsHolding
        {
            get => m_isHolding;
            private set => m_isHolding = value;
        }

        /// <summary>
        /// [기능]: 현재 집게 상태에 따라 플레이어의 좌우 조작권 잠금 여부를 판별합니다.
        /// (수동 조종 중 대기 상태일 때는 조작이 가능하도록 하강/집기/상승/놓기 연출 및 진입 연출 중에만 잠급니다)
        /// </summary>
        public bool IsControlLocked
        {
            get
            {
                return m_currentState == ClawState.DESCENDING ||
                       m_currentState == ClawState.GRABBING ||
                       m_currentState == ClawState.ASCENDING ||
                       m_currentState == ClawState.RELEASING ||
                       m_currentState == ClawState.ENTRANCE_SEQUENCE;
            }
        }

        public float DescendSpeed => m_model.DescendSpeed;

        #endregion

        #region 초기화 (Initialization)

        public ClawViewModel(ClawModel model, Vector2 startPosition)
        {
            m_model = model;
            m_position = startPosition;
            m_currentState = ClawState.IDLE;
            m_currentInputDirection = 0f;
            m_isHolding = false;
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 퀴즈 결과를 기반으로 집게 다리 아귀 토크 강도를 동적으로 갱신합니다.
        /// </summary>
        public void ApplyQuizStats(QuizStatsDTO stats)
        {
            if (stats != null)
            {
                m_model.CurrentMaxTorque = m_model.DefaultMaxTorque * stats.TorqueMultiplier;
            }
        }

        /// <summary>
        /// [기능]: UI 방향키 입력 값에 따라 이동 방향 상태를 분기 제어합니다.
        /// </summary>
        public void SetInputDirection(float direction)
        {
            if (IsControlLocked)
            {
                return;
            }

            m_currentInputDirection = direction;

            if (Mathf.Approximately(direction, 0f))
            {
                CurrentState = ClawState.IDLE;
            }
            else
            {
                CurrentState = direction < 0f ? ClawState.MOVING_LEFT : ClawState.MOVING_RIGHT;
            }
        }

        /// <summary>
        /// [기능]: 'Grab' 집기 UI 버튼 신호 수신 시 수평 제어 멈춤 및 하강 프로세스 전이를 개시합니다.
        /// </summary>
        public void ExecuteGrab()
        {
            if (IsControlLocked || m_isHolding)
            {
                return;
            }

            // 아귀력 초기화 (이전 퀴즈 정오답 판정으로 인해 아귀력이 약화된 잔재를 리셋합니다)
            if (m_model != null)
            {
                m_model.CurrentMaxTorque = m_model.DefaultMaxTorque;
            }

            m_currentInputDirection = 0f;
            CurrentState = ClawState.DESCENDING;
        }

        /// <summary>
        /// [기능]: 'Release' 놓기 UI 버튼 신호 수신 시 수평 제어 멈춤 및 투하 프로세스 전이를 개시합니다.
        /// </summary>
        public void ExecuteRelease()
        {
            if (IsControlLocked || !m_isHolding)
            {
                return;
            }

            m_currentInputDirection = 0f;
            CurrentState = ClawState.RELEASING;
        }

        /// <summary>
        /// [기능]: 캡슐 보유 상태(IsHolding)를 안전하게 변경합니다.
        /// </summary>
        public void SetHolding(bool holding)
        {
            m_isHolding = holding;
        }

        /// <summary>
        /// [기능]: 집게 오므리기(Grab) 및 벌리기(Release) 물리 모터 매개변수를 연산하고 이벤트를 발생시킵니다.
        /// </summary>
        public void ControlClawPhysics(bool grab)
        {
            float speed = grab ? m_model.DefaultMotorSpeed : -m_model.DefaultMotorSpeed;
            float torque = m_model.CurrentMaxTorque;

            // grab이 true(오므릴 때)일 때만 HingeJoint2D의 useMotor를 참으로 켜서 모터의 강력한 힘을 부가하고,
            // grab이 false(놓기/대기)일 때는 모터를 해제하여(useMotor = false) 중력이나 본래의 물리 장력에 의해 부드럽게 늘어지며 벌어지게 제어합니다.
            OnMotorTriggered?.Invoke(speed, torque, grab);
        }

        /// <summary>
        /// [기능]: 실시간 Update 프레임 내 수평 좌표 이동 연산 및 한계 범위 검사
        /// </summary>
        public void UpdateMovement(float deltaTime)
        {
            if (IsControlLocked)
            {
                return;
            }

            if (!Mathf.Approximately(m_currentInputDirection, 0f))
            {
                float nextX = m_position.x + (m_currentInputDirection * m_model.HorizontalSpeed * deltaTime);
                nextX = Mathf.Clamp(nextX, m_model.MinXLimit, m_model.MaxXLimit);

                m_position.x = nextX;
                OnPositionChanged?.Invoke(m_position);
            }
        }

        public void SetPositionDirectly(Vector2 newPos)
        {
            m_position = newPos;
            OnPositionChanged?.Invoke(m_position);
        }

        public void ChangeState(ClawState state)
        {
            CurrentState = state;
        }

        #endregion
    }
}
