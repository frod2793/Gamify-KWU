using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: UI Canvas를 벗어나 2D World Space 상에서 집게의 이동과 흔들림을 제어
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-31
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 와이어 탑(m_clawRoot) 기준 물리 진자 수식 일원화 및 집게 렌더링 끝점 이탈 방지용 오프셋 회전 교정 완료
    /// </summary>
    public class ClawView : MonoBehaviour
    {
        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("좌우로 주행 및 진자 물리/와이어의 최상단 축이 되는 와이어 탑(Wire Top)이자 카트의 Transform 객체입니다.")]
        private Transform m_clawRoot;

        [SerializeField]
        [Tooltip("절차적 제어를 받는 실제 집게 헤드 View 객체입니다.")]
        private ClawBodyView m_clawBody;

        [Header("와이어 다관절 설정 (Wire Segments)")]
        [SerializeField]
        [Tooltip("와이어의 마디로 사용할 스프라이트 텍스처입니다.")]
        private Sprite m_wireSegmentSprite;

        [SerializeField]
        [Tooltip("와이어 마디들의 간격입니다.")]
        private float m_segmentSpacing = 0.15f;

        [SerializeField]
        [Tooltip("와이어의 마디들의 시각적 크기(Scale)입니다.")]
        private Vector3 m_segmentScale = Vector3.one;

        [SerializeField]
        [Tooltip("와이어의 마디들의 색상입니다.")]
        private Color m_wireColor = Color.white;

        [SerializeField]
        [Tooltip("와이어 마디 스프라이트의 Sorting Layer 이름입니다.")]
        private string m_sortingLayerName = "Default";

        [SerializeField]
        [Tooltip("와이어 마디 스프라이트의 Sorting Order 값입니다.")]
        private int m_sortingOrder = 5;

        [Header("와이어 출렁임 및 처짐 설정 (Wire Sag & Wave)")]
        [SerializeField]
        [Tooltip("와이어의 처짐 강도 (중력에 의한 아래 처짐 정도)")]
        private float m_wireSagIntensity = 0.12f;

        [SerializeField]
        [Tooltip("와이어의 물결 출렁임 폭 (진자 흔들림 각속도 비례)")]
        private float m_waveAmplitude = 0.08f;

        [SerializeField]
        [Tooltip("와이어의 물결 파동 진행 속도")]
        private float m_waveSpeed = 12f;

        [SerializeField]
        [Tooltip("와이어의 물결 파동 주파수 (밀도)")]
        private float m_waveFrequency = 6f;

        [SerializeField]
        [Tooltip("상승 완료 후 밀착 상태의 최소 줄 길이입니다.")]
        private float m_minRopeDistance = 0.5f;


        [SerializeField]
        [Tooltip("집게 하강 시 와이어 줄이 늘어날 최대 길이입니다.")]
        private float m_maxRopeDistance = 5.0f;

        [SerializeField]
        [Tooltip("집게가 바닥/인형에 안착한 후, 실제로 오므리기까지 대기하는 시간(초)입니다.")]
        private float m_grabDelay = 0.2f;

        [Header("와이어 미세 위치 보정 설정 (Wire Offset)")]
        [SerializeField]
        [Tooltip("와이어의 월드 Z축 깊이 값입니다. 2D 표준은 0입니다.")]
        private float m_wireZDepth = 0f;

        [SerializeField]
        [Tooltip("천장 카트(ClawRoot) 기준 와이어 시작점의 로컬 미세 보정 좌표(Offset)입니다.")]
        private Vector3 m_wireStartOffset = Vector3.zero;

        [SerializeField]
        [Tooltip("집게 헤드(ClawBody) 기준 와이어 끝점의 로컬 미세 보정 좌표(Offset)입니다.")]
        private Vector3 m_wireEndOffset = Vector3.zero;

        [Header("밧줄 다관절 지연 시뮬레이터 (Rope Lag Settings)")]
        [SerializeField]
        [Tooltip("카트 주행 시 밧줄 마디가 상위 마디를 추종하는 지연 속도(유연도)입니다. 낮을수록 활처럼 크게 휩니다.")]
        private float m_ropeLagElasticity = 10.0f;

        [SerializeField]
        [Tooltip("카트 정지 시 밧줄이 다시 수직 일직선으로 신속하게 복원되는 강도입니다.")]
        private float m_ropeRestoration = 12.0f;

        private List<Vector3> m_segmentWorldPositions = new List<Vector3>();

        [Header("애니메이션 (Animation)")]
        [SerializeField] private float m_descendDuration = 0.8f;
        [SerializeField] private float m_ascendDuration = 0.8f;

        [Header("절차적 흔들림 설정 (Procedural Swing)")]
        [SerializeField][Tooltip("흔들림의 강도 (카트 가속도 영향력)")] private float m_swingSensitivity = 0.5f;
        [SerializeField][Tooltip("중력 복원력 (0으로 돌아오려는 힘)")] private float m_swingGravity = 9.8f;
        [SerializeField][Tooltip("흔들림 감쇄 저항")] private float m_swingDamping = 0.98f;
        [SerializeField][Tooltip("최대 흔들림 각도")] private float m_maxSwingAngle = 45f;

        [Header("주행 한계 설정 (Cart Boundaries)")]
        [SerializeField][Tooltip("카트가 좌측으로 갈 수 있는 최소 로컬 X 좌표입니다.")] private float m_minCartX = -4.0f;
        [SerializeField][Tooltip("카트가 우측으로 갈 수 있는 최대 로컬 X 좌표입니다.")] private float m_maxCartX = 4.0f;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private bool m_isMoving;
        private float m_moveDirection; // -1: Left, 1: Right, 0: Idle
        private float m_moveSpeed = 3.0f;

        private Vector3 m_initialPosition;
        private Transform m_wireContainer;
        private List<GameObject> m_wireSegments = new List<GameObject>();
        private List<Vector3> m_segmentPositions = new List<Vector3>();
        private System.Threading.CancellationTokenSource m_animCts;
        private bool m_isInAnimSequence; // 하강~상승 애니메이션 시퀀스 진행 중 플래그 (레이스 컨디션 방지)

        // [수식 필드]: 절차적 물리 연산용
        private float m_currentRopeLength;
        private float m_currentAngle;       // 현재 진자 각도 (degree)
        private float m_angularVelocity;   // 각속도
        private float m_prevCartX;         // 가속도 계산용 이전 프레임 좌표
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            // 하이어라키에서 의도치 않게 추가될 수 있는 Rigidbody2D 컴포넌트 강제 방어 제거
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) Destroy(rb);

            var joint = GetComponent<Joint2D>();
            if (joint != null) Destroy(joint);
        }

        private void Start()
        {
            if (m_clawRoot != null)
            {
                m_initialPosition = m_clawRoot.localPosition;
                m_prevCartX = m_clawRoot.position.x;
            }

            // 초기 줄 길이 및 각도 설정
            m_currentRopeLength = m_minRopeDistance;
            m_currentAngle = 0f;
            m_angularVelocity = 0f;

            if (m_clawBody != null)
            {
                m_clawBody.SetClawsOpenImmediately();
            }

            // 와이어 객체 동적 생성 (SpriteRenderer 기반)
            InitializeWire();
        }

        private void Update()
        {
            UpdateCartMovement();
            UpdatePendulumPhysics();
        }

        private void FixedUpdate()
        {
            UpdatePhysicsPosition();
        }

        private void LateUpdate()
        {
            RenderClawAndWire();
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 재수강(재시도) 시 카트(ClawRoot)의 위치를 시작 원점 좌표로 원복하고 줄 길이 및 집게 상태를 완전 리셋합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_ResetClawToInitialState()
        {
            CancelAnimations();

            if (m_clawRoot != null)
            {
                m_clawRoot.localPosition = m_initialPosition;
                m_prevCartX = m_clawRoot.position.x;
            }

            m_currentRopeLength = m_minRopeDistance;
            m_currentAngle = 0f;
            m_angularVelocity = 0f;
            m_moveDirection = 0f;
            m_isMoving = false;
            m_isInAnimSequence = false;

            if (m_clawBody != null)
            {
                m_clawBody.ResetPhysicsToKinematic();
                m_clawBody.SetClawsOpenImmediately();
                m_clawBody.ReleaseDoll();
            }

            Debug.Log("[ClawView] 재시도로 인한 집게 카트 및 로프 길이, 헤드 물리 상태 초기화 완료.");
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnMoveRequested += HandleMoveRequested;
            m_viewModel.OnStopRequested += HandleStopRequested;
            m_viewModel.OnStateChanged += HandleStateChanged;
            m_viewModel.OnDropRequested += HandleDropRequested;

            if (m_clawBody != null)
            {
                m_clawBody.Initialize(m_viewModel, m_clawRoot);
            }
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnMoveRequested -= HandleMoveRequested;
                m_viewModel.OnStopRequested -= HandleStopRequested;
                m_viewModel.OnStateChanged -= HandleStateChanged;
                m_viewModel.OnDropRequested -= HandleDropRequested;
            }
            CancelAnimations();
        }

        /// <summary>
        /// [기능]: 최대 필요한 와이어 마디 개수를 계산하여 SpriteRenderer 기반의 와이어 오브젝트들을 생성하고 풀링합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        private void InitializeWire()
        {
            if (m_wireContainer == null)
            {
                GameObject containerObj = new GameObject("Wire_Container");
                containerObj.transform.SetParent(m_clawRoot, false);
                containerObj.transform.localPosition = Vector3.zero;
                m_wireContainer = containerObj.transform;
            }

            // 최대 필요한 마디 수 계산 (여유 분으로 + 5개 추가 확보)
            int requiredCount = Mathf.CeilToInt(m_maxRopeDistance / m_segmentSpacing) + 5;
            for (int i = 0; i < requiredCount; i++)
            {
                GameObject segmentObj = new GameObject($"Wire_Segment_{i}", typeof(SpriteRenderer));
                segmentObj.transform.SetParent(m_wireContainer, false);
                segmentObj.transform.localScale = m_segmentScale;

                SpriteRenderer sr = segmentObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (m_wireSegmentSprite != null)
                    {
                        sr.sprite = m_wireSegmentSprite;
                    }
                    sr.color = m_wireColor;
                    sr.sortingLayerName = m_sortingLayerName;
                    sr.sortingOrder = m_sortingOrder;
                }

                segmentObj.SetActive(false);
                m_wireSegments.Add(segmentObj);
            }

            Debug.Log($"[ClawView] 와이어 다관절 풀 초기화 완료: {requiredCount}개 마디 생성됨.");
        }
        #endregion

        #region 절차적 물리 엔진 (Procedural Physics Engine)
        private void UpdateCartMovement()
        {
            if (m_isMoving && m_clawRoot != null)
            {
                Vector3 pos = m_clawRoot.localPosition;
                pos.x += m_moveDirection * m_moveSpeed * Time.deltaTime;
                pos.x = Mathf.Clamp(pos.x, m_minCartX, m_maxCartX);
                m_clawRoot.localPosition = pos;
            }
        }

        private void UpdatePendulumPhysics()
        {
            if (m_clawRoot == null) return;

            // 1. 카트의 가속도 계산 (프레임 간 월드 좌표 차이)
            float currentCartX = m_clawRoot.position.x;
            float cartDeltaX = (currentCartX - m_prevCartX) / Time.deltaTime;

            // 가속도 변화량에 따른 관성 부여
            float inertiaForce = -cartDeltaX * m_swingSensitivity;

            // 2. 진자 운동 수식
            float gravityForce = -Mathf.Sin(m_currentAngle * Mathf.Deg2Rad) * m_swingGravity;

            m_angularVelocity += (inertiaForce + gravityForce) * Time.deltaTime * 10f;
            m_angularVelocity *= m_swingDamping;

            m_currentAngle += m_angularVelocity * Time.deltaTime * 50f;
            m_currentAngle = Mathf.Clamp(m_currentAngle, -m_maxSwingAngle, m_maxSwingAngle);

            m_prevCartX = currentCartX;
        }

        /// <summary>
        /// [기능]: 물리 프레임마다 현재 줄 길이와 진자 각도에 따른 집게 헤드의 물리 목표 위치와 회전을 주입합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 물리 타겟 지연 필드(m_laggedPhysicsTarget)를 제거하여, 렌더링 끝점과 물리 강체 위치 불일치 유격을 완벽 방지하고 순수 진자각 타겟을 주입하도록 수정
        /// </summary>
        private void UpdatePhysicsPosition()
        {
            if (m_clawBody != null && m_clawRoot != null)
            {
                Quaternion rotation = Quaternion.Euler(0, 0, m_currentAngle);
                Vector3 idealOffset = rotation * Vector3.down * m_currentRopeLength;
                Vector3 idealTargetPosition = m_clawRoot.position + idealOffset;

                // 실시간 진자 운동 공식에 기초하여 목표 물리 회전(Tilt)을 주입
                Quaternion targetRotation = m_clawRoot.rotation * rotation;

                m_clawBody.UpdatePhysicsTarget(idealTargetPosition, targetRotation, m_currentRopeLength);
            }
        }

        /// <summary>
        /// [기능]: 다관절 스프라이트 마디들이 가속도에 반응하여 시간차 지연(Lag)을 겪으며 활처럼 휘어지는 렌더링을 처리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: m_laggedEndPos 지연 변수를 제거하고 N+1개 정점 구조를 통해 렌더-강체 밀착을 극대화했으며, endPos 오프셋에 가해지던 로컬 회전을 제거해 어긋나는 현상 교정 완료
        /// </summary>
        private void RenderClawAndWire()
        {
            if (m_clawBody == null || m_clawRoot == null)
            {
                return;
            }

            // 1. 보정된 시작/끝점 월드 좌표 산출
            Vector3 startPos = m_clawRoot.position + m_wireStartOffset;
            // 집게 헤드 위치는 실시간 물리 강체 좌표를 준수하며, 고정 오프셋을 더합니다.
            Vector3 endPos = m_clawBody.transform.position + m_wireEndOffset;

            // 2D 표준 평면 Z축 동기화 고정
            startPos.z = m_wireZDepth;
            endPos.z = m_wireZDepth;

            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;

            // 마디 간의 미세한 틈새 유격을 원천 차단하기 위해 올림(CeilToInt)으로 계산
            int neededSegments = Mathf.CeilToInt(distance / m_segmentSpacing);
            if (neededSegments <= 0)
            {
                for (int i = 0; i < m_wireSegments.Count; i++)
                {
                    if (m_wireSegments[i].activeSelf) m_wireSegments[i].SetActive(false);
                }
                return;
            }

            int poolCount = m_wireSegments.Count;

            // 동적 풀 팽창 안전 보증
            if (neededSegments > poolCount)
            {
                int addCount = neededSegments - poolCount + 5;
                for (int i = 0; i < addCount; i++)
                {
                    GameObject segmentObj = new GameObject($"Wire_Segment_Dynamic_{poolCount + i}", typeof(SpriteRenderer));
                    segmentObj.transform.SetParent(m_wireContainer, false);
                    segmentObj.transform.localScale = m_segmentScale;

                    SpriteRenderer sr = segmentObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        if (m_wireSegmentSprite != null)
                        {
                            sr.sprite = m_wireSegmentSprite;
                        }
                        sr.color = m_wireColor;
                        sr.sortingLayerName = m_sortingLayerName;
                        sr.sortingOrder = m_sortingOrder;
                    }

                    segmentObj.SetActive(false);
                    m_wireSegments.Add(segmentObj);
                }
                poolCount = m_wireSegments.Count;
            }

            // 마디(Sprite)들을 고정점과 끝점에 완전 연결하기 위해 N+1개의 정점을 생성
            int pointCount = neededSegments + 1;

            // 2. 가상 마디 점 버퍼 크기 맞추기 및 동기화 (Zero-Alloc)
            while (m_segmentWorldPositions.Count < pointCount)
            {
                m_segmentWorldPositions.Add(startPos);
            }
            if (m_segmentWorldPositions.Count > pointCount)
            {
                m_segmentWorldPositions.RemoveRange(pointCount, m_segmentWorldPositions.Count - pointCount);
            }

            // 3. 다관절 지연 체인 (Lag-Chain Physics) 월드 궤적 연산
            float currentElasticity = m_isMoving ? m_ropeLagElasticity : m_ropeRestoration;
            
            m_segmentPositions.Clear();
            Vector3 prevLeader = startPos;
            Vector3 normal = Vector3.Cross(direction.normalized, Vector3.forward).normalized;

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / neededSegments; // 0.0 (시작) ~ 1.0 (끝)
                
                // 이미 집게 헤드(endPos) 자체가 관성에 의해 지연된 물리 궤적을 띄고 있으므로, 직선 궤적은 양 끝단을 완벽히 잇습니다.
                Vector3 idealBasePos = Vector3.Lerp(startPos, endPos, t);

                Vector3 oldPos = m_segmentWorldPositions[i];
                oldPos.z = m_wireZDepth;

                Vector3 finalSegmentPos;
                if (i == 0)
                {
                    finalSegmentPos = startPos; // 최상단은 무조건 카트와 완벽 부착
                }
                else if (i == pointCount - 1)
                {
                    finalSegmentPos = endPos;   // 최하단은 무조건 집게 헤드 홀더와 완벽 부착
                }
                else
                {
                    Vector3 lagTarget = Vector3.Lerp(oldPos, prevLeader, Time.deltaTime * currentElasticity);
                    float dampWeight = m_isMoving ? Mathf.Lerp(0.85f, 0.0f, t) : Mathf.Lerp(0.95f, 0.0f, t);
                    finalSegmentPos = Vector3.Lerp(lagTarget, idealBasePos, 1.0f - dampWeight);
                }

                // 처짐 및 흔들림 파동 2차 결합
                float sagAmount = 4.0f * t * (1.0f - t);
                Vector3 sagOffset = Vector3.down * (m_wireSagIntensity * 0.5f * sagAmount);
                float waveValue = Mathf.Sin(Time.time * m_waveSpeed - t * m_waveFrequency) * (m_waveAmplitude * 0.375f) * sagAmount * Mathf.Clamp(m_angularVelocity, -2.5f, 2.5f);
                Vector3 waveOffset = normal * waveValue;

                Vector3 renderedPos = finalSegmentPos + sagOffset + waveOffset;
                
                // 파동 적용 후에도 시작점과 끝점은 100% 고정되도록 클램핑
                if (i == 0) renderedPos = startPos;
                else if (i == pointCount - 1) renderedPos = endPos;
                
                renderedPos.z = m_wireZDepth;

                m_segmentWorldPositions[i] = finalSegmentPos;
                m_segmentPositions.Add(renderedPos);
                
                prevLeader = finalSegmentPos;
            }

            // 4. 와이어 조각 오브젝트 최종 물리적 꺾임 회전 렌더링 배치
            // 각 마디(Sprite) i는 정점 P_i 에 위치하고, 바로 다음 정점 P_(i+1) 을 바라보도록 회전합니다.
            for (int i = 0; i < poolCount; i++)
            {
                GameObject segment = m_wireSegments[i];
                if (segment != null)
                {
                    if (i < neededSegments)
                    {
                        Vector3 targetPos = m_segmentPositions[i];
                        Vector3 nextPos = m_segmentPositions[i + 1];
                        Vector3 segmentDirection = nextPos - targetPos;

                        float angle = 0f;
                        if (segmentDirection.sqrMagnitude > 0.0001f)
                        {
                            angle = Mathf.Atan2(segmentDirection.y, segmentDirection.x) * Mathf.Rad2Deg + 90f;
                        }

                        Quaternion segmentRotation = Quaternion.Euler(0f, 0f, angle);

                        segment.transform.position = targetPos;
                        segment.transform.rotation = segmentRotation;

                        if (!segment.activeSelf)
                        {
                            segment.SetActive(true);
                        }
                    }
                    else
                    {
                        if (segment.activeSelf)
                        {
                            segment.SetActive(false);
                        }
                    }
                }
            }
        }
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        private void HandleMoveRequested(bool isRight)
        {
            m_moveDirection = isRight ? 1 : -1;
            m_isMoving = true;
        }

        private void HandleStopRequested()
        {
            m_isMoving = false;
            m_moveDirection = 0;
        }

        private void CancelAnimations()
        {
            if (m_animCts != null)
            {
                m_animCts.Cancel();
                m_animCts.Dispose();
                m_animCts = null;
            }
        }

        private void HandleStateChanged(ClawStateType newState)
        {
            // [핵심 보호]: 하강/그랩/상승 등 애니메이션 시퀀스가 진행 중일 때,
            // Idle/Moving 전환에 의한 불필요한 토큰 취소를 방지합니다.
            // 단, 새로운 애니메이션 시퀀스 상태로 진입할 때는 반드시 이전 시퀀스를 정리합니다.
            bool isAnimationState = (newState == ClawStateType.Descending ||
                                     newState == ClawStateType.Grabbing ||
                                     newState == ClawStateType.Ascending ||
                                     newState == ClawStateType.Returning);

            if (isAnimationState)
            {
                // 새 애니메이션 시퀀스 시작 → 이전 시퀀스 정리 후 새 토큰 발급
                CancelAnimations();
                m_animCts = new System.Threading.CancellationTokenSource();
            }
            else if (!m_isInAnimSequence)
            {
                // 애니메이션 시퀀스가 진행 중이 아닐 때만 잔존 토큰 정리 (레이스 컨디션 차단)
                CancelAnimations();
            }

            switch (newState)
            {
                case ClawStateType.Descending:
                    m_isInAnimSequence = true;
                    PlayDescendAnimation(m_animCts.Token).Forget();
                    break;
                case ClawStateType.Grabbing:
                    PlayGrabAnimation(m_animCts.Token).Forget();
                    break;
                case ClawStateType.Ascending:
                    PlayAscendAnimation(m_animCts.Token).Forget();
                    break;
                case ClawStateType.Returning:
                    PlayReturnAnimation(m_animCts.Token).Forget();
                    break;
                case ClawStateType.Idle:
                    m_isInAnimSequence = false;
                    break;
                case ClawStateType.Result:
                    m_isInAnimSequence = false;
                    CheckResult();
                    break;
            }
        }

        private void HandleDropRequested()
        {
            if (m_clawBody != null)
            {
                m_clawBody.ReleaseDoll();
            }
        }
        #endregion

        #region 절차적 애니메이션 시퀀스
        private async UniTaskVoid PlayDescendAnimation(System.Threading.CancellationToken token)
        {
            if (m_clawBody != null)
            {
                m_clawBody.ResetClawCollidersForNextPlay();
                m_clawBody.SetPhysicsToDynamic(); // 하강 시작 즉시 Dynamic으로 전환하여 밀침 충격 근절!
                m_clawBody.OpenClaws();
            }

            float elapsed = 0f;
            float startLen = m_currentRopeLength;

            while (elapsed < m_descendDuration)
            {
                elapsed += Time.deltaTime;
                m_currentRopeLength = Mathf.Lerp(startLen, m_maxRopeDistance, elapsed / m_descendDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            m_currentRopeLength = m_maxRopeDistance;

            await UniTask.Delay(System.TimeSpan.FromSeconds(m_grabDelay), cancellationToken: token);

            if (m_viewModel != null) m_viewModel.NotifyDescendCompleted();
        }

        private async UniTaskVoid PlayGrabAnimation(System.Threading.CancellationToken token)
        {
            if (m_clawBody != null)
            {
                await m_clawBody.PlayGrabSequenceAsync(token);
            }
            else
            {
                if (m_viewModel != null) m_viewModel.NotifyGrabCompleted(false);
            }
        }

        private async UniTaskVoid PlayAscendAnimation(System.Threading.CancellationToken token)
        {
            float elapsed = 0f;

            // 얹혀진 상태에 따른 실제 카트-집게간의 거리를 측정하여 줄의 감김 시작점으로 설정 (텔레포트/튕김 방지)
            float startLen = m_currentRopeLength;
            if (m_clawRoot != null && m_clawBody != null)
            {
                float actualDistance = Vector3.Distance(m_clawRoot.position, m_clawBody.transform.position);
                startLen = Mathf.Clamp(actualDistance, m_minRopeDistance, m_maxRopeDistance);
            }
            m_currentRopeLength = startLen;

            while (elapsed < m_ascendDuration)
            {
                elapsed += Time.deltaTime;
                m_currentRopeLength = Mathf.Lerp(startLen, m_minRopeDistance, elapsed / m_ascendDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            m_currentRopeLength = m_minRopeDistance;

            // [상승 완료 시점]: 상승이 완전히 끝나 밀착한 시점에 비로소 Kinematic으로 강체 원복 (실패 시의 Dynamic 유지 보증)
            if (m_clawBody != null)
            {
                m_clawBody.ResetPhysicsToKinematic();
            }

            if (m_viewModel != null)
            {
                m_viewModel.NotifyAscendCompleted();
            }
        }

        private async UniTaskVoid PlayReturnAnimation(System.Threading.CancellationToken token)
        {
            if (m_clawRoot == null) return;

            await m_clawRoot.DOLocalMoveX(m_initialPosition.x, 1.5f)
                .SetEase(Ease.OutQuad)
                .ToUniTask(cancellationToken: token);

            if (m_clawBody != null) m_clawBody.ReleaseDoll();

            await UniTask.Delay(500, cancellationToken: token);
            if (m_viewModel != null) m_viewModel.NotifyReturnCompleted();
        }

        private void CheckResult()
        {
            if (m_viewModel != null) m_viewModel.NotifyResultCompleted();
        }
        #endregion
    }
}
