using UnityEngine;
using TMPro;

namespace GameArifiction.Claw
{
    /// <summary>
    /// [기능]: 퀴즈 뽑기 타겟이 되는 일정한 규격의 물리 캡슐 제어 컴포넌트
    /// [작성자]: [Senior Client Developer]
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class CapsuleView : MonoBehaviour
    {
        #region Inspector Settings

        [Header("Physical Attributes")]
        [SerializeField] private float m_defaultMass = 1.0f;
        [SerializeField] private float m_dragValue = 0.5f;

        [Header("Quiz Core Data")]
        [SerializeField] private string m_capsuleValue = "";

        [Header("UI Reference")]
        [SerializeField] private TextMeshProUGUI m_answerText;

        #endregion

        #region 내부 필드 (Private Fields)

        private Rigidbody2D m_rigidbody;
        private CircleCollider2D m_circleCollider;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        /// <summary>
        /// [기능]: 캡슐에 부여된 고유 식별값(텍스트 정답 비교용)
        /// </summary>
        public string Value
        {
            get => m_capsuleValue;
            set => m_capsuleValue = value;
        }

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            InitializePhysics();
        }

        private void Start()
        {
            // 캡슐의 외경(크기)을 엄격히 고정하기 위해 초기화 검증
            ValidateCapsuleScale();
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 캡슐 텍스트 데이터를 주입하고 UI 텍스트에 표기합니다.
        /// </summary>
        public void Setup(string text)
        {
            m_capsuleValue = text;
            if (m_answerText != null)
            {
                m_answerText.text = text;
            }
        }

        /// <summary>
        /// [기능]: 캡슐 게임 오브젝트를 파괴합니다.
        /// </summary>
        public void DestroySelf()
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region 내부 로직 (Private Methods)

        private void InitializePhysics()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            m_circleCollider = GetComponent<CircleCollider2D>();

            // Rigidbody 설정 강제 적용
            if (m_rigidbody != null)
            {
                m_rigidbody.mass = m_defaultMass;
                m_rigidbody.linearDamping = m_dragValue;
                m_rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void ValidateCapsuleScale()
        {
            // 크기는 일정하게 강제 통일 (예: 1.0f 스케일)
            Vector3 uniformScale = Vector3.one;
            transform.localScale = uniformScale;
        }

        #endregion
    }
}

