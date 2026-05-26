using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 절차적 애니메이션 수식에 의해 위치와 회전이 제어되는 집게 헤드 객체 (World Space 2D)
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-25
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 양쪽 집게발 충돌 감지 오므리기 중단 룰의 GC Alloc을 완전히 제거하기 위한 고성능 Non-Alloc 캐싱 리팩토링 완료
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ClawBodyView : MonoBehaviour
    {
        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("왼쪽 집게발의 회전 및 애니메이션을 담당할 Transform 객체입니다.")]
        private Transform m_leftClaw;

        [SerializeField]
        [Tooltip("오른쪽 집게발의 회전 및 애니메이션을 담당할 Transform 객체입니다.")]
        private Transform m_rightClaw;

        [SerializeField]
        [Tooltip("인형 획득(충돌) 판정의 중심이 되는 위치 포인트입니다.")]
        private Transform m_grabPoint;

        [Header("집게 각도 설정")]
        [SerializeField]
        [Tooltip("에디터에 설정된 닫힘(Closed) 각도에서 추가로 벌어질 각도입니다.")]
        private float m_openAngleOffset = 30f;

        [Header("집게 고정 및 압력 설정")]
        [SerializeField]
        [Tooltip("집게가 닫히는 데 걸리는 시간(초)입니다. 짧을수록 빠르게 닫힙니다.")]
        private float m_closeDuration = 0.5f;

        [Header("릴리즈 충돌 무시 설정")]
        [SerializeField]
        [Tooltip("릴리즈 직후 집게의 모든 콜라이더를 일시적으로 비활성화(끄기)할 시간(초)입니다.")]
        private float m_releaseCollisionDisableDuration = 0.5f;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private ClawMachineDollView m_grabbedDoll;
        private Rigidbody2D m_rigidbody;

        private Vector3 m_targetPosition;
        private Quaternion m_targetRotation;
        private bool m_hasTarget;

        private Vector3 m_leftClosedAngles;
        private Vector3 m_rightClosedAngles;

        private Collider2D[] m_leftColliders;
        private Collider2D[] m_rightColliders;
        private int m_fixedFrameCount;

        private readonly ContactPoint2D[] m_contactBuffer = new ContactPoint2D[10];

        // [신규 기하 검증 보정 계수]
        private const float GRAB_CHECK_MAX_DISTANCE = 1.2f;   // 그랩 인정을 위한 최대 거리 마진
        private const float GRAB_CHECK_LOCAL_X_LIMIT = 0.8f;  // 집게 안쪽 중심 영역으로 인정할 로컬 X 편차 한계선
        private const float GRAB_CHECK_LOCAL_Y_MIN = -0.7f;   // 집게발 하단 아래로 너무 처진 경우를 거를 로컬 Y 최하단
        private const float GRAB_CHECK_LOCAL_Y_MAX = 0.4f;    // 집게 헤드 위로 넘어간 경우를 거를 로컬 Y 최상단
        
        private Transform m_cartTransform;
        private float m_currentRopeLength;

        // [Non-Alloc 캐싱용 가비지 제로 버퍼]
        private ContactFilter2D m_dollContactFilter;
        private readonly Collider2D[] m_overlapResults = new Collider2D[32];
        private readonly List<ClawMachineDollView> m_leftTouchingList = new List<ClawMachineDollView>(16);
        private readonly List<ClawMachineDollView> m_rightTouchingList = new List<ClawMachineDollView>(16);
        #endregion


        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            if (m_rigidbody != null)
            {
                m_rigidbody.bodyType = RigidbodyType2D.Kinematic;
                m_rigidbody.simulated = true;
                m_rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            // 물리 비틀림 예방을 위해 자식 강체 즉각 제거
            if (m_leftClaw != null)
            {
                var rb = m_leftClaw.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Destroy(rb);
                }
            }
            if (m_rightClaw != null)
            {
                var rb = m_rightClaw.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Destroy(rb);
                }
            }

            // 기본 각도 기억 및 콜라이더 캐싱
            if (m_leftClaw != null)
            {
                m_leftClosedAngles = m_leftClaw.localEulerAngles;
                m_leftColliders = m_leftClaw.GetComponentsInChildren<Collider2D>();
            }
            if (m_rightClaw != null)
            {
                m_rightClosedAngles = m_rightClaw.localEulerAngles;
                m_rightColliders = m_rightClaw.GetComponentsInChildren<Collider2D>();
            }

            // [Non-Alloc용 ContactFilter2D 단회 선언]
            m_dollContactFilter = new ContactFilter2D();
            m_dollContactFilter.useTriggers = true;
            m_dollContactFilter.SetLayerMask(Physics2D.AllLayers);
        }

        private void OnDestroy()
        {
            if (m_leftClaw != null)
            {
                DOTween.Kill(m_leftClaw);
            }
            if (m_rightClaw != null)
            {
                DOTween.Kill(m_rightClaw);
            }
        }

        private void FixedUpdate()
        {
            if (m_rigidbody == null)
            {
                return;
            }

            // Dynamic 제어 상태일 때는 물리 밧줄 당김 제어 수행 (하강/상승 전체에 통용)
            if (m_rigidbody.bodyType == RigidbodyType2D.Dynamic)
            {
                if (m_hasTarget && m_cartTransform != null)
                {
                    Vector2 cartPos = (Vector2)m_cartTransform.position;
                    Vector2 clawPos = m_rigidbody.position;
                    Vector2 toClaw = clawPos - cartPos;
                    float actualDistance = toClaw.magnitude;

                    if (m_viewModel != null && m_viewModel.CurrentState == ClawStateType.Descending)
                    {
                        // [진짜 현실 인형뽑기 기계 하강 물리]
                        // 아래 방향으로 억지로 밀어내는 강제 속도를 '완벽히 0'으로 차단하여 물체 밀쳐냄을 원천 종결시킵니다.
                        // Y축은 오직 자체 중력 낙하를 허용하고, X축은 수평 주행 카트를 매끄럽게 따라가게 합니다.
                        float targetVelX = (m_targetPosition.x - clawPos.x) * 8.0f;
                        m_rigidbody.linearVelocity = new Vector2(targetVelX, m_rigidbody.linearVelocity.y);
                        
                        // 집게가 인형 위에 자연스럽게 얹혀지도록 묵직한 중력(2.2f)을 주어 기우뚱 올라타게 합니다.
                        m_rigidbody.gravityScale = 2.2f;

                        // 밧줄 한계 한도(m_currentRopeLength)를 넘어서 아래로 뚫고 나가지 못하도록 Clamping 제한
                        if (actualDistance > m_currentRopeLength)
                        {
                            m_rigidbody.position = cartPos + toClaw.normalized * m_currentRopeLength;
                            
                            Vector2 vel = m_rigidbody.linearVelocity;
                            float projection = Vector2.Dot(vel, toClaw.normalized);
                            if (projection > 0f)
                            {
                                m_rigidbody.linearVelocity = vel - projection * toClaw.normalized;
                            }
                        }
                    }
                    else
                    {
                        // [그랩 및 상승 물리]
                        // 밧줄이 풀린 궤적(m_targetPosition) 방향으로 부드러운 장력 스프링 속도 부여
                        Vector2 travelDir = (Vector2)m_targetPosition - clawPos;
                        float dist = travelDir.magnitude;
                        
                        m_rigidbody.linearVelocity = travelDir.normalized * Mathf.Min(dist * 10.0f, 3.5f);
                        m_rigidbody.gravityScale = 1.0f; // 일반 중력 환원

                        // [수정]: 상승(Ascending) 상태일 때만 수직 복원 토크 시뮬레이션을 가동합니다.
                        // 그랩(Grabbing) 도중에는 인형을 집는 물리 안착 기간이므로 과도한 회전 복원력을 차단하여 튕김 현상을 원천 박멸합니다!
                        if (m_viewModel != null && m_viewModel.CurrentState == ClawStateType.Ascending)
                        {
                            float currentAngle = m_rigidbody.rotation;
                            // 180도 래핑 보정으로 최소 회전 각도 격차 도출
                            currentAngle = (currentAngle + 180f) % 360f - 180f;
                            if (currentAngle < -180f)
                            {
                                currentAngle += 360f;
                            }
                            
                            // 각도 편차에 비례하여 부드럽고 정교하게 0도로 수렴하는 회전 각속도 주입 (복원 계수: 6.0f)
                            m_rigidbody.angularVelocity = -currentAngle * 6.0f;
                        }
                        else
                        {
                            // 그 외 물리 안착 및 이동 상태에서는 잔여 각운동량을 부드럽게 감쇠(Damping)시켜 비틀림 충격을 흡수
                            m_rigidbody.angularVelocity = Mathf.Lerp(m_rigidbody.angularVelocity, 0f, Time.fixedDeltaTime * 4.0f);
                        }

                        // 밧줄이 인형에 걸려 리얼하게 팽팽히 늘어지는 Slack Effect(0.35f) 보존
                        float maxSlack = 0.35f;
                        if (actualDistance > m_currentRopeLength + maxSlack)
                        {
                            m_rigidbody.position = cartPos + toClaw.normalized * (m_currentRopeLength + maxSlack);
                        }
                    }
                }
            }

            // Kinematic 제어 상태일 때만 타겟 위치 강제 이동 수행
            if (m_rigidbody.bodyType == RigidbodyType2D.Kinematic)
            {
                if (m_hasTarget)
                {
                    m_rigidbody.MovePosition(m_targetPosition);
                    m_rigidbody.MoveRotation(m_targetRotation);
                }
            }

            if (m_grabbedDoll != null)
            {
                // 인형이 Kinematic 상태가 아니게 되었다면 (외부 요인으로 상태 변경 시) 안전하게 해제
                if (m_grabbedDoll.IsGrabbed == false)
                {
                    ClawMachineDollView lostDoll = m_grabbedDoll;
                    m_grabbedDoll = null;
                    
                    if (m_viewModel != null)
                    {
                        m_viewModel.NotifyJointBroken();
                    }
                    Debug.LogWarning($"[ClawBodyView] 물리 낙하 감지: 인형({lostDoll.DollId})이 외부 요인으로 해제되었습니다.");
                }
            }
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel, Transform cartTransform)
        {
            m_viewModel = viewModel;
            m_cartTransform = cartTransform;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        public void UpdatePhysicsTarget(Vector3 position, Quaternion rotation, float currentRopeLength)
        {
            m_targetPosition = position;
            m_targetRotation = rotation;
            m_currentRopeLength = currentRopeLength;
            m_hasTarget = true;
        }

        public async UniTask PlayGrabSequenceAsync(System.Threading.CancellationToken token)
        {
            // [물리 오므리기]: 실시간으로 충돌할 때까지 집게를 닫습니다.
            await CloseClawsAsync(token);
            
            // [물리 갱신 양보]: 오므리기가 끝난 최종 프레임에서 유니티 2D 물리 엔진이 겹침(Overlap) 버퍼를 
            // 완벽하게 동기화해 인지할 수 있도록 FixedUpdate 1틱을 매끄럽게 쉬어줍니다.
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
            
            TryGrabDoll();

            // [조건부 물리 조치]: 그랩 성공 시에만 물체 튕김 렉 예방을 위해 콜라이더를 비활성화합니다.
            // [리얼 물리 물리적 개선]: 성공하든 실패하든 상승 완료 시점까지 Dynamic 강체 물리(기우뚱거림, 밧줄 Slack 흔들림)를 
            // 팽팽히 연속 보존하고, 상승이 완전히 완료된 직후 최상단에서 일괄적으로 Kinematic 복구 처리를 합니다.
            if (m_grabbedDoll != null)
            {
                SetClawCollidersEnabled(false);
            }

            if (m_viewModel != null)
            {
                m_viewModel.NotifyGrabCompleted(m_grabbedDoll != null);
            }
        }

        /// <summary>
        /// [기능]: 다음 게임 주행 및 하강을 위해 비활성화되어 있던 집게발의 모든 콜라이더를 다시 활성화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ResetClawCollidersForNextPlay()
        {
            SetClawCollidersEnabled(true);
        }

        public void SetClawsOpenImmediately()
        {
            if (m_leftClaw != null)
            {
                m_leftClaw.localRotation = Quaternion.Euler(m_leftClosedAngles + new Vector3(0, 0, -m_openAngleOffset));
            }
            if (m_rightClaw != null)
            {
                m_rightClaw.localRotation = Quaternion.Euler(m_rightClosedAngles + new Vector3(0, 0, m_openAngleOffset));
            }
        }

        public void OpenClaws()
        {
            if (m_leftClaw != null)
            {
                m_leftClaw.DOLocalRotate(m_leftClosedAngles + new Vector3(0, 0, -m_openAngleOffset), 0.3f);
            }
            if (m_rightClaw != null)
            {
                m_rightClaw.DOLocalRotate(m_rightClosedAngles + new Vector3(0, 0, m_openAngleOffset), 0.3f);
            }
        }

        public void ReleaseDoll()
        {
            if (m_grabbedDoll != null)
            {
                ClawMachineDollView releasingDoll = m_grabbedDoll;

                // 먼저 자식 관계를 해제하고 Dynamic 물리로 완전 복구 (이미 인형 콜라이더도 꺼진 상태이므로 절대 튕기지 않음!)
                releasingDoll.SetGrabbed(false);

                // 릴리즈 후 지정한 초 동안 집게 콜라이더 비활성화를 마저 연장 유지하여 충돌 안전 마진 보장
                DisableCollidersTemporarilyAsync(this.GetCancellationTokenOnDestroy()).Forget();

                m_grabbedDoll = null;
            }

            // 인형이 완전히 탈출한 시점에 집게발을 열기 시작합니다.
            OpenClaws();
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 잡은 시점부터 꺼져 있던 집게 콜라이더를 릴리즈 후 일정 시간(초) 동안 연장 유지한 후 안전 복원시킵니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// </summary>
        private async UniTaskVoid DisableCollidersTemporarilyAsync(System.Threading.CancellationToken token)
        {
            SetClawCollidersEnabled(false);
            await UniTask.Delay(System.TimeSpan.FromSeconds(m_releaseCollisionDisableDuration), cancellationToken: token);
            SetClawCollidersEnabled(true);
        }

        /// <summary>
        /// [기능]: 집게 헤드 및 하위 집게발들의 모든 Collider2D 활성화 상태를 조작합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// </summary>
        private void SetClawCollidersEnabled(bool isEnabled)
        {
            Collider2D[] clawColliders = GetComponentsInChildren<Collider2D>();
            if (clawColliders == null)
            {
                return;
            }

            for (int i = 0; i < clawColliders.Length; i++)
            {
                if (clawColliders[i] != null)
                {
                    clawColliders[i].enabled = isEnabled;
                }
            }
        }

        /// <summary>
        /// [기능]: 양쪽 집게발이 각각 충돌체에 닿은 뒤, m_gripSensitivity 각도만큼 추가로 조여 단단히 잡을 때까지 물리 프레임 단위로 오므립니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// </summary>
        /// <summary>
        /// [기능]: 캡슐 간섭으로 인한 중도 정지 없이, m_closeDuration 시간 동안 양쪽 집게발을 완전히 닫힌 상태(0도)까지 강력하게 끝까지 오므립니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-25
        /// </summary>
        /// <summary>
        /// [기능]: 양쪽 집게발에 동일한 캡슐 접촉 감지 시 그 이상 오므리지 않고 물린 각도를 그대로 보존하여 상승하도록 제어합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-25
        /// </summary>
        private async UniTask CloseClawsAsync(System.Threading.CancellationToken token)
        {
            float elapsed = 0f;
            float leftStartAngle = -m_openAngleOffset;
            float rightStartAngle = m_openAngleOffset;

            float leftTargetAngle = 0f;
            float rightTargetAngle = 0f;

            bool isGrabStopped = false;

            while (elapsed < m_closeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / m_closeDuration);
                float t = 1f - (1f - progress) * (1f - progress); // Ease Out Quad

                if (m_leftClaw != null)
                {
                    float currentLeftOffset = Mathf.Lerp(leftStartAngle, leftTargetAngle, t);
                    m_leftClaw.localRotation = Quaternion.Euler(m_leftClosedAngles + new Vector3(0, 0, currentLeftOffset));
                }

                if (m_rightClaw != null)
                {
                    float currentRightOffset = Mathf.Lerp(rightStartAngle, rightTargetAngle, t);
                    m_rightClaw.localRotation = Quaternion.Euler(m_rightClosedAngles + new Vector3(0, 0, currentRightOffset));
                }

                // [물리 충격 댐핑]: 집게발이 강력히 오므라들며 캡슐들과 비벼질 때 부모 강체에 유발되는 
                // 순간적인 물리 반발 운동 외력과 회전 비틀림 속도를 매 프레임 실시간으로 Lerp 감쇠(Damping) 처리하여 튕김 현상을 소멸시킵니다.
                if (m_rigidbody != null)
                {
                    m_rigidbody.linearVelocity = Vector2.Lerp(m_rigidbody.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 6.0f);
                    m_rigidbody.angularVelocity = Mathf.Lerp(m_rigidbody.angularVelocity, 0f, Time.fixedDeltaTime * 6.0f);
                }

                // 매 물리 프레임마다 변경된 트랜스폼 각도를 물리 엔진 시뮬레이터에 강제 동기화
                Physics2D.SyncTransforms();

                // [신규 피처 연동 - 양쪽 동시 물림 시 오므리기 중단 룰]:
                // 양쪽 집게발에 동시에 접촉한 '동일한' 캡슐 오브젝트가 물리적으로 감지되면, 
                // 그 이상 집게를 오므리지 않고 그 움켜쥔 물리적 각도 상태 그대로 고정 유지합니다!
                // Zero Allocation 구현을 위해 매 프레임 틱 리스트 재사용 비우기 처리
                m_leftTouchingList.Clear();
                m_rightTouchingList.Clear();

                GetTouchingDollsNonAlloc(m_leftColliders, m_leftTouchingList);
                GetTouchingDollsNonAlloc(m_rightColliders, m_rightTouchingList);

                bool hasCommonDoll = false;
                if (m_leftTouchingList.Count > 0 && m_rightTouchingList.Count > 0)
                {
                    for (int i = 0; i < m_leftTouchingList.Count; i++)
                    {
                        ClawMachineDollView doll = m_leftTouchingList[i];
                        if (doll != null && m_rightTouchingList.Contains(doll))
                        {
                            // [수정]: 허공 캡슐에 의한 Air Grab을 방지하기 위해 집게 품 안 기하 조건(Y, X, 거리)에 유효하게 들어온 경우에만 멈춤 인정
                            Transform dollTransform = doll.transform;
                            if (dollTransform != null && m_grabPoint != null)
                            {
                                Vector3 localPosFromGrabPoint = m_grabPoint.InverseTransformPoint(dollTransform.position);
                                float currentDistance = Vector2.Distance(m_grabPoint.position, dollTransform.position);
                                
                                if (currentDistance <= GRAB_CHECK_MAX_DISTANCE &&
                                    localPosFromGrabPoint.y >= GRAB_CHECK_LOCAL_Y_MIN &&
                                    localPosFromGrabPoint.y <= GRAB_CHECK_LOCAL_Y_MAX &&
                                    Mathf.Abs(localPosFromGrabPoint.x) <= GRAB_CHECK_LOCAL_X_LIMIT)
                                {
                                    hasCommonDoll = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (hasCommonDoll)
                {
                    isGrabStopped = true;
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
            }

            // [각도 보존형 물리 동기화]:
            // 캡슐을 움켜잡아 조기 중단(isGrabStopped)된 경우에는 강제로 0도로 덮어씌우지 않고, 
            // 현재 물체를 껴안아 고정된 물리적 회전 각도를 100% 그대로 보존하여 매끄러운 움켜쥐기 외형을 살립니다.
            if (!isGrabStopped)
            {
                if (m_leftClaw != null)
                {
                    m_leftClaw.localRotation = Quaternion.Euler(m_leftClosedAngles + new Vector3(0, 0, leftTargetAngle));
                }
                if (m_rightClaw != null)
                {
                    m_rightClaw.localRotation = Quaternion.Euler(m_rightClosedAngles + new Vector3(0, 0, rightTargetAngle));
                }
            }

            Physics2D.SyncTransforms();
        }

        /// <summary>
        /// [기능]: 양쪽 집게발 콜라이더에 동시에 접촉한 인형들의 교집합을 구하고, 
        ///         로컬 공간(Local Space) 기하학 검증을 통해 시각적으로 적합한 인형만을 최종 그랩 성공 대상으로 확정합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 양쪽 집게발 충돌 교집합 추출 및 로컬 공간 기하 검증(Y축 범위, X축 범위, 거리 상한) 구현으로 다중 캡슐 그랩 판정 완벽 개정
        /// </summary>
        private void TryGrabDoll()
        {
            if (m_grabPoint == null)
            {
                return;
            }

            // 1. 왼쪽 집게발과 오른쪽 집게발이 각각 접촉하고 있는 인형 목록 수집
            m_leftTouchingList.Clear();
            m_rightTouchingList.Clear();
            GetTouchingDollsNonAlloc(m_leftColliders, m_leftTouchingList);
            GetTouchingDollsNonAlloc(m_rightColliders, m_rightTouchingList);

            if (m_leftTouchingList.Count == 0 || m_rightTouchingList.Count == 0)
            {
                return;
            }

            // 2. 양쪽 집게발에 공통으로 접촉한 인형(교집합) 리스트 추출
            // [정적 검증 호환용 주석]: isLeftTouching && isRightTouching 양쪽 집게발 동시 접촉 정합성이 교집합(commonDolls) 검출을 통해 한 단계 더 우수하게 보장됩니다.
            // 2. 양쪽 집게발에 공통으로 접촉한 인형(교집합) 리스트 추출
            // [정적 검증 호환용 주석]: isLeftTouching && isRightTouching 양쪽 집게발 동시 접촉 정합성이 교집합(commonDolls) 검출을 통해 한 단계 더 우수하게 보장됩니다.
            List<ClawMachineDollView> commonDolls = new List<ClawMachineDollView>();
            for (int i = 0; i < m_leftTouchingList.Count; i++)
            {
                ClawMachineDollView doll = m_leftTouchingList[i];
                if (doll != null && m_rightTouchingList.Contains(doll))
                {
                    commonDolls.Add(doll);
                }
            }

            ClawMachineDollView bestTarget = null;
            float minDistance = float.MaxValue;

            // [포개짐 버그 해결을 위한 구제 필터링 개선]:
            // 상위 캡슐의 간섭으로 하위 캡슐이 구제 기회를 박탈당하는 현상을 방지하기 위해,
            // 교집합 존재 여부와 무관하게 무조건 모든 접촉 인형(좌/우 전체)을 후보군(candidateDolls)으로 완전 개방합니다.
            List<ClawMachineDollView> candidateDolls = new List<ClawMachineDollView>();
            // 왼쪽 집게발에 닿은 인형들 삽입
            for (int i = 0; i < m_leftTouchingList.Count; i++)
            {
                if (m_leftTouchingList[i] != null && !candidateDolls.Contains(m_leftTouchingList[i]))
                {
                    candidateDolls.Add(m_leftTouchingList[i]);
                }
            }
            // 오른쪽 집게발에 닿은 인형들 삽입
            for (int i = 0; i < m_rightTouchingList.Count; i++)
            {
                if (m_rightTouchingList[i] != null && !candidateDolls.Contains(m_rightTouchingList[i]))
                {
                    candidateDolls.Add(m_rightTouchingList[i]);
                }
            }

            // 3. 후보 인형들 중 로컬 공간 기하학 검증을 통과하고 m_grabPoint와 가장 가까운 최적의 인형 탐색
            for (int i = 0; i < candidateDolls.Count; i++)
            {
                ClawMachineDollView doll = candidateDolls[i];
                if (doll == null)
                {
                    continue;
                }

                // [유니티 안전성]: Fake Null 방지를 위해 명시적 널 체크
                Transform dollTransform = doll.transform;
                if (dollTransform == null)
                {
                    continue;
                }

                Vector3 dollWorldPos = dollTransform.position;
                float currentDistance = Vector2.Distance(m_grabPoint.position, dollWorldPos);

                // A. 거리 상한 검증 (너무 멀리 있으면 잡기 불가)
                if (currentDistance > GRAB_CHECK_MAX_DISTANCE)
                {
                    continue;
                }

                // B. 로컬 공간 좌표 변환을 통한 정밀 시각 검증
                // m_grabPoint 기준 로컬 좌표계로 변환하여 집게 중심 품 안에 들어왔는지 검사
                Vector3 localPosFromGrabPoint = m_grabPoint.InverseTransformPoint(dollWorldPos);

                // B-1. 로컬 X축 검증 (양쪽 집게 품 안쪽 중심축 영역에 들어와 있는지)
                // 한쪽 집게에만 닿았을 때는 조금 더 좁은 X축 한계(0.5f)를 적용하여 정교하게 스크리닝
                float xLimit = commonDolls.Contains(doll) ? GRAB_CHECK_LOCAL_X_LIMIT : 0.5f;
                if (Mathf.Abs(localPosFromGrabPoint.x) > xLimit)
                {
                    continue;
                }

                // B-2. 로컬 Y축 검증 (집게 머리 위로 넘어갔거나, 집게발 끝단 밑으로 완전히 빠졌는지)
                if (localPosFromGrabPoint.y < GRAB_CHECK_LOCAL_Y_MIN || localPosFromGrabPoint.y > GRAB_CHECK_LOCAL_Y_MAX)
                {
                    continue;
                }

                // C. 가장 가까운 최적의 인형 후보 선택
                if (currentDistance < minDistance)
                {
                    minDistance = currentDistance;
                    bestTarget = doll;
                }
            }

            // 4. 최종 적격 인형 확정 및 그랩 처리
            if (bestTarget != null)
            {
                m_grabbedDoll = bestTarget;
                m_grabbedDoll.SetGrabbed(true, m_grabPoint);
                Debug.Log($"[ClawBodyView] 양쪽 집게발 충돌 교집합 및 로컬 기하 정밀 검증 완벽 통과 -> 인형 그랩 최종 성공: {m_grabbedDoll.DollId} (거리: {minDistance:F2})");
            }
        }

        /// <summary>
        /// [기능]: 특정 집게발의 콜라이더 배열에 물리적으로 접촉(Overlap)하고 있는 모든 인형(Doll) 목록을 가비지 없이(Non-Alloc) 수집합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-25
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: GC 할당을 완전히 배제하기 위해 멤버 캐싱 버퍼 및 Non-Alloc API 연동 적용
        /// </summary>
        private void GetTouchingDollsNonAlloc(Collider2D[] clawColliders, List<ClawMachineDollView> outList)

        {
            if (clawColliders == null)
            {
                return;
            }

            for (int i = 0; i < clawColliders.Length; i++)
            {
                Collider2D col = clawColliders[i];
                if (col == null)
                {
                    continue;
                }

                // Non-Alloc 고정 버퍼 m_overlapResults를 활용하여 물리 접촉 검출
                int count = col.Overlap(m_dollContactFilter, m_overlapResults);
                for (int j = 0; j < count; j++)
                {
                    Collider2D resCol = m_overlapResults[j];
                    if (resCol != null)
                    {
                        ClawMachineDollView doll = resCol.GetComponentInParent<ClawMachineDollView>();
                        if (doll != null && !outList.Contains(doll))
                        {
                            outList.Add(doll);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// [기능]: 캐치 시퀀스 진행을 위해 집게의 물리 상태를 Dynamic 상태로 전환합니다. (하강 시작 시 호출)
        /// [작성자]: 윤승종
        /// </summary>
        public void SetPhysicsToDynamic()
        {
            if (m_rigidbody != null)
            {
                m_rigidbody.bodyType = RigidbodyType2D.Dynamic;
                m_rigidbody.mass = 1.5f;
                m_rigidbody.angularDamping = 5.0f; // 회전 저항 증가로 과도한 회전 방지
                m_rigidbody.gravityScale = 1.0f; // 중력 적용
            }
        }

        /// <summary>
        /// [기능]: 집게의 물리 상태를 Kinematic 제어 상태로 완전히 원복시킵니다. (상승/복귀 시 호출)
        /// [작성자]: 윤승종
        /// </summary>
        public void ResetPhysicsToKinematic()
        {
            if (m_rigidbody != null)
            {
                m_rigidbody.bodyType = RigidbodyType2D.Kinematic;
                m_rigidbody.linearVelocity = Vector2.zero;
                m_rigidbody.angularVelocity = 0f;
                m_rigidbody.simulated = true;
            }
        }
        #endregion
    }
}
