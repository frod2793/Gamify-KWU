using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: UI Canvas를 벗어나 2D World Space 상에서 집게의 이동과 흔들림을 제어
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-24
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: RectTransform 의존성을 제거하고 Transform 및 SpriteRenderer 기반으로 전환
    /// </summary>
    public class ClawView : MonoBehaviour
    {
        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("좌우로 주행하는 천장 카트의 Transform 객체입니다.")]
        private Transform m_clawRoot;

        [SerializeField]
        [Tooltip("절차적 제어를 받는 실제 집게 헤드 View 객체입니다.")]
        private ClawBodyView m_clawBody;

        [SerializeField]
        [Tooltip("와이어의 시각적 색상입니다.")]
        private Color m_wireColor = Color.white;

        [SerializeField]
        [Tooltip("와이어의 굵기(너비)입니다.")]
        private float m_wireWidth = 0.04f;

        [SerializeField]
        [Tooltip("상승 완료 후 밀착 상태의 최소 줄 길이입니다.")]
        private float m_minRopeDistance = 0.5f;


        [SerializeField]
        [Tooltip("집게 하강 시 와이어 줄이 늘어날 최대 길이입니다.")]
        private float m_maxRopeDistance = 5.0f;

        [SerializeField]
        [Tooltip("집게가 바닥/인형에 안착한 후, 실제로 오므리기까지 대기하는 시간(초)입니다.")]
        private float m_grabDelay = 0.2f;

        [Header("애니메이션 (Animation)")]
        [SerializeField] private float m_descendDuration = 0.8f;
        [SerializeField] private float m_ascendDuration = 0.8f;

        [Header("절차적 흔들림 설정 (Procedural Swing)")]
        [SerializeField] [Tooltip("흔들림의 강도 (카트 가속도 영향력)")] private float m_swingSensitivity = 0.5f;
        [SerializeField] [Tooltip("중력 복원력 (0으로 돌아오려는 힘)")] private float m_swingGravity = 9.8f;
        [SerializeField] [Tooltip("흔들림 감쇄 저항")] private float m_swingDamping = 0.98f;
        [SerializeField] [Tooltip("최대 흔들림 각도")] private float m_maxSwingAngle = 45f;

        [Header("주행 한계 설정 (Cart Boundaries)")]
        [SerializeField] [Tooltip("카트가 좌측으로 갈 수 있는 최소 로컬 X 좌표입니다.")] private float m_minCartX = -4.0f;
        [SerializeField] [Tooltip("카트가 우측으로 갈 수 있는 최대 로컬 X 좌표입니다.")] private float m_maxCartX = 4.0f;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private bool m_isMoving;
        private float m_moveDirection; // -1: Left, 1: Right, 0: Idle
        private float m_moveSpeed = 3.0f;
        
        private Vector3 m_initialPosition;
        private LineRenderer m_wireLineRenderer;
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
            // [전면 재설계]: 물리 컴포넌트 자동 제거 (있을 경우 대비)
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

            Debug.Log("[ClawView] [ClawView] 재시도로 인한 집게 카트 및 로프 길이, 헤드 물리 상태 초기화 완료.");
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

        private void InitializeWire()
        {
            GameObject wireObj = new GameObject("Wire_Renderer", typeof(LineRenderer));
            wireObj.transform.SetParent(m_clawRoot, false);
            m_wireLineRenderer = wireObj.GetComponent<LineRenderer>();
            
            if (m_wireLineRenderer != null)
            {
                Shader defaultShader = Shader.Find("Sprites/Default");
                if (defaultShader != null)
                {
                    m_wireLineRenderer.material = new Material(defaultShader);
                }
                
                m_wireLineRenderer.startColor = m_wireColor;
                m_wireLineRenderer.endColor = m_wireColor;
                m_wireLineRenderer.startWidth = m_wireWidth;
                m_wireLineRenderer.endWidth = m_wireWidth;

                m_wireLineRenderer.positionCount = 2;
                m_wireLineRenderer.sortingLayerName = "Default";
                m_wireLineRenderer.sortingOrder = 5;
            }
            
            wireObj.transform.localPosition = Vector3.zero;
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
        /// [수정 날짜]: 2026-05-24
        /// </summary>
        private void UpdatePhysicsPosition()
        {
            if (m_clawBody != null && m_clawRoot != null)
            {
                Quaternion rotation = Quaternion.Euler(0, 0, m_currentAngle);
                Vector3 offset = rotation * Vector3.down * m_currentRopeLength;
                
                Vector3 targetPosition = m_clawRoot.position + offset;
                Quaternion targetRotation = m_clawRoot.rotation * rotation;

                m_clawBody.UpdatePhysicsTarget(targetPosition, targetRotation, m_currentRopeLength);
            }
        }

        /// <summary>
        /// [기능]: 줄 스프라이트와 집게의 시각적 형태를 와이어 길이와 회전에 맞추어 렌더링합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 물리 타겟 갱신(UpdatePhysicsTarget) 로직을 FixedUpdate로 이관하고 시각적 연출만 담당
        /// </summary>
        private void RenderClawAndWire()
        {
            if (m_clawBody == null || m_clawRoot == null || m_wireLineRenderer == null)
            {
                return;
            }

            // 카트 루트와 실제 집게 바디의 월드 좌표를 이어 밧줄 렌더링
            m_wireLineRenderer.SetPosition(0, m_clawRoot.position);
            m_wireLineRenderer.SetPosition(1, m_clawBody.transform.position);
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
