using System;

namespace GameArifiction.Player
{
    /// <summary>
    /// [기능]: 플레이어의 순수 데이터 정보를 담는 모델 클래스 (POCO)
    /// [작성자]: [성함/팀명]
    /// </summary>
    public class PlayerModel
    {
        #region 내부 필드 (Private Fields)

        private float m_moveSpeed;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public float MoveSpeed
        {
            get => m_moveSpeed;
            set => m_moveSpeed = value;
        }

        #endregion

        #region 초기화 (Initialization)

        public PlayerModel(float moveSpeed)
        {
            m_moveSpeed = moveSpeed;
        }

        #endregion
    }
}
