using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 개별 인형의 물리 및 시각적 표현을 담당하는 View (타입 충돌 우회 적용)
    /// [작성자]: 윤승종
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class ClawMachineDollView : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)
        private DollModel m_model;
        private Rigidbody2D m_rigidbody;
        #endregion

        #region 속성 (Properties)
        public string DollId => m_model?.DollId;
        #endregion

        #region 초기화 (Initialization)
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
        }

        public void Initialize(DollModel model)
        {
            m_model = model;
            // 모델의 무게 등의 데이터로 강체 설정
            if (m_rigidbody != null)
            {
                m_rigidbody.mass = m_model.Weight;
            }
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        public void SetGrabbed(bool isGrabbed, Transform grabPoint = null)
        {
            if (m_rigidbody == null)
            {
                return;
            }

            // Kinematic 강제 변환 및 부모 지정을 제거하여, 순수 Dynamic 물리 및 FixedJoint2D 결합 연산이 올바르게 시뮬레이션되도록 지원합니다.
            m_rigidbody.bodyType = RigidbodyType2D.Dynamic;
        }
        #endregion
    }
}
