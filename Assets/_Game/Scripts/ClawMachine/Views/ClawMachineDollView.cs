using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 개별 인형의 물리 및 시각적 표현을 담당하는 View
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-24
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: Kinematic 전환 대신 simulated 옵션 제어 방식으로 물리 snap 텔레포트 버그 완벽 패치
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class ClawMachineDollView : MonoBehaviour
    {
        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("캡슐 위에 선택지 텍스트를 출력할 3D TextMeshPro 컴포넌트입니다.")]
        private TMPro.TextMeshPro m_answerTextMesh;
        #endregion

        #region 내부 필드 (Private Fields)
        private DollModel m_model;
        private Rigidbody2D m_rigidbody;

        // 릴리즈 복원용 원본 상태 캐싱
        private Transform m_originalParent;
        private RigidbodyInterpolation2D m_originalInterpolation;
        private bool m_isGrabbed;

        // 다중 콜라이더 캐싱
        private Collider2D[] m_colliders;
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

        // [신규]: 방해 캡슐 여부를 뷰에서 조회하기 위한 래퍼 속성 (널 조건부 연산자 사용 차단)
        public bool IsDisagree
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.IsDisagree;
                }
                return false;
            }
        }

        /// <summary>
        /// [기능]: 해당 캡슐의 정답 여부를 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public bool IsCorrect
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.IsCorrect;
                }
                return false;
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

            // 다중 콜라이더 캐싱 (일시적 물리 비활성화 기능용)
            m_colliders = GetComponentsInChildren<Collider2D>();
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(DollModel model)
        {
            m_model = model;

            if (m_model != null && m_answerTextMesh != null)
            {
                m_answerTextMesh.text = m_model.AnswerText;
            }
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 집게에 잡힐 때 Rigidbody2D.simulated = false로 물리 시뮬레이션을 일시 중지하고 콜라이더를 끕니다.
        ///         릴리즈 시 simulated = true로 복원하고 보간(Interpolation)을 제어하여 부드러운 중력 낙하를 유도합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// </summary>
        public void SetGrabbed(bool isGrabbed, Transform grabPoint = null)
        {
            if (m_rigidbody == null)
            {
                return;
            }

            if (isGrabbed)
            {
                // 원본 부모 및 보간 상태 캐싱
                m_originalParent = transform.parent;
                m_originalInterpolation = m_rigidbody.interpolation;

                // [수정]: bodyType = Kinematic 전환 대신, simulated = false로 물리 엔진 계산만 꺼둡니다.
                // 뚝 떨어지는 물리 snap 현상을 원천 방지하는 핵심 설계입니다.
                m_rigidbody.linearVelocity = Vector2.zero;
                m_rigidbody.angularVelocity = 0f;
                m_rigidbody.simulated = false;

                // grabPoint가 제공되면 그 자식으로 편입 (위치 자동 동기화)
                if (grabPoint != null)
                {
                    transform.SetParent(grabPoint, true);
                }

                // 물건을 집어 고정된 직후 콜라이더를 완전히 비활성화(끄기)합니다.
                SetCollidersEnabled(false);

                m_isGrabbed = true;
                Debug.Log($"[ClawMachineDollView] 인형이 집게에 잡혔습니다. (시뮬레이션 정지 및 콜라이더 비활성화 완료) DollId: {DollId}");
            }
            else
            {
                // 인형을 씬 최상단(Root)으로 분리 (이미 콜라이더가 완전히 꺼져 있는 상태이므로 튕김 리스크가 절대 없음!)
                transform.SetParent(null, true);

                // [물리 동기화]: SetParent 직후 변경된 월드 계층 구조 정보를 물리 엔진 내부 시뮬레이터에 강제로 반영합니다.
                Physics2D.SyncTransforms();

                // [중요]: 보간 버퍼에 쌓여있던 과거 틱의 잔여 정보 때문에 텔레포트되는 유니티 Snap 버그를 차단하기 위해
                // 물리 보간(Interpolation)을 강제로 완전히 끕니다!
                m_rigidbody.interpolation = RigidbodyInterpolation2D.None;

                // [수정]: 캡슐의 Dynamic 상태를 상시 유지하되, 릴리즈 순간 시뮬레이션을 다시 재개(simulated = true)합니다.
                m_rigidbody.simulated = true;

                // [물리 좌표 덮어쓰기 후행 보장]: 물리 복원이 완전히 적용된 직후에 월드 렌더 좌표를 물리 엔진 강체 좌표 버퍼에 주입합니다.
                m_rigidbody.position = transform.position;
                m_rigidbody.rotation = transform.rotation.eulerAngles.z;

                // Dynamic 물리 상태 전이 직전 속도 정보 완전 리셋 (잔여 관성 튕김 차단)
                m_rigidbody.linearVelocity = Vector2.zero;
                m_rigidbody.angularVelocity = 0f;
                m_rigidbody.WakeUp();

                // 0.2초간 안전 낙하 유도 후 콜라이더 및 보간 복원 비동기 루틴 구동
                RestoreCollidersAndInterpolationAsync(this.GetCancellationTokenOnDestroy()).Forget();

                m_isGrabbed = false;
            }
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 인형 하위의 모든 콜라이더 활성화 상태를 안전하게 제어합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// </summary>
        private void SetCollidersEnabled(bool isEnabled)
        {
            if (m_colliders == null)
            {
                return;
            }

            for (int i = 0; i < m_colliders.Length; i++)
            {
                if (m_colliders[i] != null)
                {
                    m_colliders[i].enabled = isEnabled;
                }
            }
        }

        /// <summary>
        /// [기능]: 집게발이 벌어지고 겹침 이탈이 일어나기에 충분한 시간(0.2초) 동안 물리 차단을 유지한 뒤 콜라이더와 보간 상태를 복원시킵니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// </summary>
        private async UniTaskVoid RestoreCollidersAndInterpolationAsync(System.Threading.CancellationToken token)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f), cancellationToken: token);

            SetCollidersEnabled(true);
            
            if (m_rigidbody != null)
            {
                m_rigidbody.interpolation = m_originalInterpolation;
            }
        }
        #endregion
    }
}
