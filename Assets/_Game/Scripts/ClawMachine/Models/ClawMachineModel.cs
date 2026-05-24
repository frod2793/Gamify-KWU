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
        private int m_reTakeCount; // [신규]: 누적 재수강 횟수
        #endregion

        #region 속성 (Properties)
        public Vector2 ClawPosition
        {
            get
            {
                return m_clawPosition;
            }
            set
            {
                m_clawPosition = value;
            }
        }

        public int RemainingPlayCount
        {
            get
            {
                return m_remainingPlayCount;
            }
            set
            {
                m_remainingPlayCount = value;
            }
        }

        public float RemainingTime
        {
            get
            {
                return m_remainingTime;
            }
            set
            {
                m_remainingTime = value;
            }
        }

        public float TimeLimitPerPlay
        {
            get
            {
                return m_timeLimitPerPlay;
            }
        }

        public int ReTakeCount
        {
            get
            {
                return m_reTakeCount;
            }
            set
            {
                m_reTakeCount = value;
            }
        }
        #endregion

        #region 초기화 (Initialization)
        public ClawMachineModel(int initialPlayCount, float timeLimitPerPlay)
        {
            m_remainingPlayCount = initialPlayCount;
            m_timeLimitPerPlay = timeLimitPerPlay;
            m_remainingTime = timeLimitPerPlay;
            m_clawPosition = Vector2.zero;
            m_reTakeCount = 0;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 현재 재수강 횟수 패널티(20초씩 영구 삭감)가 적용된 플레이 한계 시간을 계산하여 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public float GetTimeLimitForCurrentPlay()
        {
            float calculatedLimit = m_timeLimitPerPlay - (m_reTakeCount * 20f);
            if (calculatedLimit < 20f)
            {
                return 20f; // 극단적인 제한 시간 감축을 막기 위해 최소 20초 마진 확보
            }
            return calculatedLimit;
        }
        #endregion
    }
}
