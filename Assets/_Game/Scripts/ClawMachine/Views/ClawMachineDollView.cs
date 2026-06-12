using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 개별 인형의 물리 및 시각적 표현을 담당하는 View
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-12
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 인스펙터에 지정된 AnswerSpriteConfigs 데이터 노출을 위한 읽기 전용 프로퍼티 추가
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class ClawMachineDollView : MonoBehaviour
    {
        #region 구조체 정의 (Structures)
        /// <summary>
        /// [기능]: 퀴즈 답안지 텍스트(예: "A", "B", "C" 등)에 따라 개별 캡슐에 매핑될 스프라이트 정보를 관리하는 구조체
        /// [작성자]: 윤승종
        /// </summary>
        [System.Serializable]
        public struct AnswerSpriteConfig
        {
            [Tooltip("스프라이트를 매핑할 답안지 텍스트 내용 (예: '아이콘', '폰트' 혹은 퀴즈 선택지 텍스트와 일치해야 합니다)")]
            public string AnswerTextKey;

            [Tooltip("적용할 캡슐 스프라이트 리소스")]
            public Sprite CapsuleSprite;

            [Tooltip("적용할 캡슐 스프라이트의 색상 보정값 (기본값 White)")]
            public Color SpriteColor;
        }
        #endregion

        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("캡슐 위에 선택지 텍스트를 출력할 3D TextMeshPro 컴포넌트입니다.")]
        private TextMeshPro m_answerTextMesh;

        [SerializeField]
        [Tooltip("답안지 텍스트 내용별로 매핑 연출될 캡슐 스프라이트 구성 목록입니다.")]
        private List<AnswerSpriteConfig> m_answerSpriteConfigs = new List<AnswerSpriteConfig>();
        #endregion

        #region 내부 필드 (Private Fields)
        private DollStateDTO m_state;
        private Rigidbody2D m_rigidbody;
        private SpriteRenderer m_spriteRenderer;

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
                return m_state.DollId;
            }
        }

        // 방해 캡슐 여부를 뷰에서 조회하기 위한 래퍼 속성
        public bool IsDisagree
        {
            get
            {
                return m_state.IsDisagree;
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
                return m_state.IsCorrect;
            }
        }

        public bool IsGrabbed
        {
            get { return m_isGrabbed; }
        }

        /// <summary>
        /// [기능]: 인스펙터에 지정된 답안지 텍스트별 캡슐 연출 구성 목록을 읽기 전용으로 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public IReadOnlyList<AnswerSpriteConfig> AnswerSpriteConfigs
        {
            get
            {
                return m_answerSpriteConfigs;
            }
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

            // 동일 게임 오브젝트에 부착된 SpriteRenderer 컴포넌트 자동 탐색 및 캐싱
            m_spriteRenderer = GetComponent<SpriteRenderer>();

            // 다중 콜라이더 캐싱 (일시적 물리 비활성화 기능용)
            m_colliders = GetComponentsInChildren<Collider2D>();
        }
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: 인형의 데이터 상태를 설정하고, 출제된 퀴즈 답안 정보(텍스트 및 정답 여부)에 따라 텍스트 및 스프라이트 리소스를 연계 초기화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-09
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: DollModel 직접 참조를 제거하고 DollStateDTO 바인딩으로 변경하여 MVVM 규칙 준수
        /// </summary>
        public void Initialize(DollStateDTO state)
        {
            m_state = state;

            if (m_answerTextMesh != null)
            {
                m_answerTextMesh.text = m_state.AnswerText;
            }

            // 퀴즈 답안지 텍스트와 직렬화 설정 리스트를 비교하여 맞춤형 캡슐 스프라이트 적용
            if (m_spriteRenderer != null && m_answerSpriteConfigs != null && m_answerSpriteConfigs.Count > 0)
            {
                string answerText = m_state.AnswerText;
                for (int i = 0; i < m_answerSpriteConfigs.Count; i++)
                {
                    AnswerSpriteConfig config = m_answerSpriteConfigs[i];
                    if (config.AnswerTextKey == answerText)
                    {
                        if (config.CapsuleSprite != null)
                        {
                            m_spriteRenderer.sprite = config.CapsuleSprite;
                            
                            // 투명색이 아닌 유효한 색상일 경우에만 색상 보정치를 입힙니다.
                            if (config.SpriteColor.a > 0.001f)
                            {
                                m_spriteRenderer.color = config.SpriteColor;
                            }

                            Debug.Log($"[ClawMachineDollView] 답안 키워드 매칭에 의해 캡슐 스프라이트 교체 적용됨. (답안: {answerText}, Sprite: {config.CapsuleSprite.name})");
                            break;
                        }
                    }
                }
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

                // 집게 하강 안착 시 강체의 물리 동역학 계산(simulated)만 일시 차단하여 유격 및 스냅 렉 차단
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
                // 인형을 씬 최상단(Root)으로 분리
                transform.SetParent(null, true);

                // 계층 구조 변경 정보 반영
                Physics2D.SyncTransforms();

                // 물리 보간(Interpolation) 초기화로 급격한 위치 이동 시 튕김 방지
                m_rigidbody.interpolation = RigidbodyInterpolation2D.None;

                // 캡슐 릴리즈 순간 물리 엔진 시뮬레이션 복구
                m_rigidbody.simulated = true;

                // 월드 렌더 좌표를 물리 엔진 강체 좌표 버퍼에 직접 주입
                m_rigidbody.position = transform.position;
                m_rigidbody.rotation = transform.rotation.eulerAngles.z;

                // 물리 상태 전이 직전 속도 값 완전 리셋
                m_rigidbody.linearVelocity = Vector2.zero;
                m_rigidbody.angularVelocity = 0f;
                m_rigidbody.WakeUp();

                // 일정 거리 낙하 안전 확보 후 콜라이더 및 보간 상태 복원 구동
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
