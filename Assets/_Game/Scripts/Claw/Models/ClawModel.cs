namespace GameArifiction.Claw
{
    /// <summary>
    /// [기능]: 집게의 속도, 물리 토크 및 작동 한계값을 소유하는 순수 C# 데이터 모델 (POCO)
    /// [작성자]: [Senior Client Developer]
    /// </summary>
    public class ClawModel
    {
        #region 내부 필드 (Private Fields)

        private float m_horizontalSpeed = 300.0f;
        private float m_descendSpeed = 300.0f;
        private float m_minXLimit = -903.0f;
        private float m_maxXLimit = 760.0f;
        private float m_defaultMaxTorque = 1500.0f;
        private float m_currentMaxTorque = 1500.0f;
        private float m_defaultMotorSpeed = 300.0f;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public float HorizontalSpeed
        {
            get => m_horizontalSpeed;
            set => m_horizontalSpeed = value;
        }

        public float DescendSpeed
        {
            get => m_descendSpeed;
            set => m_descendSpeed = value;
        }

        public float MinXLimit
        {
            get => m_minXLimit;
            set => m_minXLimit = value;
        }

        public float MaxXLimit
        {
            get => m_maxXLimit;
            set => m_maxXLimit = value;
        }

        public float DefaultMaxTorque
        {
            get => m_defaultMaxTorque;
            set => m_defaultMaxTorque = value;
        }

        public float CurrentMaxTorque
        {
            get => m_currentMaxTorque;
            set => m_currentMaxTorque = value;
        }

        public float DefaultMotorSpeed
        {
            get => m_defaultMotorSpeed;
            set => m_defaultMotorSpeed = value;
        }

        #endregion
    }
}
