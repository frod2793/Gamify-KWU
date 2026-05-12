using System;
using UnityEngine;

namespace GameArifiction.Player
{
    /// <summary>
    /// [기능]: 플레이어 이동 로직 및 상태 관리를 담당하는 뷰모델 클래스 (POCO)
    /// [작성자]: [성함/팀명]
    /// </summary>
    public class PlayerViewModel
    {
        #region 내부 필드 (Private Fields)

        private readonly PlayerModel m_model;
        
        private Vector2 m_currentPosition;
        private PlayerState m_currentState;
        private bool m_isFlipped;
        private float m_inputIntensity;

        #endregion

        #region 이벤트 (Events)

        public event Action<Vector2> OnPositionChanged;
        public event Action<PlayerState> OnStateChanged;
        public event Action<bool> OnFlipChanged;
        public event Action<float> OnIntensityChanged;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public Vector2 CurrentPosition
        {
            get => m_currentPosition;
            private set
            {
                if (m_currentPosition != value)
                {
                    m_currentPosition = value;
                    OnPositionChanged?.Invoke(m_currentPosition);
                }
            }
        }

        public PlayerState CurrentState
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

        public bool IsFlipped
        {
            get => m_isFlipped;
            private set
            {
                if (m_isFlipped != value)
                {
                    m_isFlipped = value;
                    OnFlipChanged?.Invoke(m_isFlipped);
                }
            }
        }

        public float InputIntensity
        {
            get => m_inputIntensity;
            private set
            {
                if (!Mathf.Approximately(m_inputIntensity, value))
                {
                    m_inputIntensity = value;
                    OnIntensityChanged?.Invoke(m_inputIntensity);
                }
            }
        }

        #endregion

        #region 초기화 (Initialization)

        public PlayerViewModel(PlayerModel model, Vector2 startPosition)
        {
            m_model = model;
            m_currentPosition = startPosition;
            m_currentState = PlayerState.IDLE;
            m_isFlipped = false;
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 조이스틱 입력을 처리하여 위치 및 상태 업데이트
        /// [작성자]: [성함/팀명]
        /// [수정 날짜]: 2026-05-11
        /// </summary>
        public void ProcessInput(Vector2 joystickInput, float deltaTime)
        {
            float intensity = joystickInput.magnitude;
            InputIntensity = intensity;

            if (intensity > 0.1f)
            {
                // 이동 처리
                Vector2 movement = joystickInput * m_model.MoveSpeed * deltaTime;
                CurrentPosition += movement;

                // 상태 업데이트
                CurrentState = PlayerState.MOVE;

                // 방향 업데이트 (좌/우)
                if (joystickInput.x > 0.1f)
                {
                    IsFlipped = false;
                }
                else if (joystickInput.x < -0.1f)
                {
                    IsFlipped = true;
                }
            }
            else
            {
                // 정지 상태 처리
                CurrentState = PlayerState.IDLE;
            }
        }

        #endregion
    }
}
