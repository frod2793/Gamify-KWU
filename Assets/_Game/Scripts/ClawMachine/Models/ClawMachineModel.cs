using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 기계의 핵심 상태 및 위치 데이터를 관리하는 순수 Model
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawMachineModel
    {
        #region 내부 필드 (Private Fields)
        private Vector2 m_clawPosition;
        private int m_remainingPlayCount;
        private float m_remainingTime;
        private readonly float m_timeLimitPerPlay;
        #endregion

        #region 속성 (Properties)
        public Vector2 ClawPosition
        {
            get => m_clawPosition;
            set => m_clawPosition = value;
        }

        public int RemainingPlayCount
        {
            get => m_remainingPlayCount;
            set => m_remainingPlayCount = value;
        }

        public float RemainingTime
        {
            get => m_remainingTime;
            set => m_remainingTime = value;
        }

        public float TimeLimitPerPlay => m_timeLimitPerPlay;
        #endregion

        #region 초기화 (Initialization)
        public ClawMachineModel(int initialPlayCount, float timeLimitPerPlay)
        {
            m_remainingPlayCount = initialPlayCount;
            m_timeLimitPerPlay = timeLimitPerPlay;
            m_remainingTime = timeLimitPerPlay;
            m_clawPosition = Vector2.zero;
        }
        #endregion
    }
}
