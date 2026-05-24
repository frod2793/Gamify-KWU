using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 개별 인형의 물리 및 시각적 표현을 담당하는 View
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-24
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: Kinematic 전환 + Transform 페어런팅 방식으로 전면 리팩토링 (FixedJoint2D 폐기)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class ClawMachineDollView : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)
        [SerializeField]
        [Tooltip("인형에 적용할 물리 재질입니다. 마찰력을 높이려면 Friction이 높은 재질을 할당하세요.")]
        private PhysicsMaterial2D m_physicsMaterial;

        private DollModel m_model;
        private Rigidbody2D m_rigidbody;

        // 잡히기 전 원본 상태 캐싱
        private Transform m_originalParent;
        private float m_originalGravityScale;
        private float m_originalMass;
        private float m_originalDrag;
        private float m_originalAngularDrag;
        private bool m_isGrabbed;
        #endregion

        #region 속성 (Properties)
        public string DollId
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.DollId;
                }
                return string.Empty;
            }
        }

        public bool IsGrabbed
        {
            get { return m_isGrabbed; }
        }
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            if (m_rigidbody != null)
            {
                m_rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            
            // 물리 재질 적용
            if (m_physicsMaterial != null)
            {
                Collider2D col = GetComponent<Collider2D>();
                if (col != null)
                {
                    col.sharedMaterial = m_physicsMaterial;
                }
            }
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(DollModel model)
        {
            m_model = model;
            // 모델의 무게 등의 데이터로 강체 설정
            if (m_rigidbody != null && m_model != null)
            {
                m_rigidbody.mass = m_model.Weight;
            }
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 집게에 잡힐 때 Kinematic으로 전환하고 grabPoint의 자식으로 편입시켜 완벽한 위치 동기화를 보장합니다.
        ///         놓일 때 원래 부모와 Dynamic 물리 상태로 완전 복원합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Kinematic 전환 + Transform 페어런팅 방식으로 전면 리팩토링
        /// </summary>
        public void SetGrabbed(bool isGrabbed, Transform grabPoint = null)
        {
            if (m_rigidbody == null)
            {
                return;
            }

            if (isGrabbed)
            {
                // 원본 물리 상태 및 부모 캐싱
                m_originalParent = transform.parent;
                m_originalGravityScale = m_rigidbody.gravityScale;
                m_originalMass = m_rigidbody.mass;
                m_originalDrag = m_rigidbody.linearDamping;
                m_originalAngularDrag = m_rigidbody.angularDamping;

                // Kinematic 전환: 물리 시뮬레이션에서 완전히 분리
                m_rigidbody.linearVelocity = Vector2.zero;
                m_rigidbody.angularVelocity = 0f;
                m_rigidbody.bodyType = RigidbodyType2D.Kinematic;

                // grabPoint가 제공되면 그 자식으로 편입 (위치 자동 동기화)
                if (grabPoint != null)
                {
                    transform.SetParent(grabPoint, true);
                }

                m_isGrabbed = true;
                Debug.Log($"[ClawMachineDollView] 인형이 집게에 잡혔습니다. (Kinematic 전환 완료) DollId: {DollId}");
            }
            else
            {
                // 부모 관계 원복
                transform.SetParent(m_originalParent, true);

                // Dynamic 물리 상태 완전 복원
                m_rigidbody.bodyType = RigidbodyType2D.Dynamic;
                m_rigidbody.gravityScale = m_originalGravityScale;
                m_rigidbody.mass = m_originalMass;
                m_rigidbody.linearDamping = m_originalDrag;
                m_rigidbody.angularDamping = m_originalAngularDrag;
                m_rigidbody.WakeUp();

                m_isGrabbed = false;
                Debug.Log($"[ClawMachineDollView] 인형이 집게에서 해제되었습니다. (Dynamic 복원 완료) DollId: {DollId}");
            }
        }
        #endregion
    }
}
