using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)의 학점 점수, 타이머 등 순수 인게임 비즈니스 상태 데이터를 담는 POCO 모델 클래스
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class GradeRunnerModel
    {
        #region 내부 필드 (Private Fields)

        private float m_currentGradePoint;
        private float m_remainingTime;
        private readonly float m_gameDuration;
        private readonly float m_maxGradePoint;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        /// <summary>
        /// 현재 플레이어의 학점 점수 (0.0 ~ MaxGradePoint 범위 보장)
        /// </summary>
        public float CurrentGradePoint
        {
            get => m_currentGradePoint;
            set => m_currentGradePoint = Mathf.Clamp(value, 0f, m_maxGradePoint);
        }

        /// <summary>
        /// 남은 제한 시간 (초 단위, 0 이하 클램프)
        /// </summary>
        public float RemainingTime
        {
            get => m_remainingTime;
            set => m_remainingTime = Mathf.Max(0f, value);
        }

        public float GameDuration => m_gameDuration;
        public float MaxGradePoint => m_maxGradePoint;

        #endregion

        #region 초기화 (Initialization)

        public GradeRunnerModel(float startGradePoint, float maxGradePoint, float gameDuration)
        {
            m_maxGradePoint = maxGradePoint;
            m_gameDuration = gameDuration;
            
            m_currentGradePoint = Mathf.Clamp(startGradePoint, 0f, maxGradePoint);
            m_remainingTime = gameDuration;
        }

        #endregion
    }
}
