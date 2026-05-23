using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 물리 엔진을 배제하고 수식 기반 절차적 애니메이션으로 집게의 이동과 흔들림을 제어
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-24
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 모든 Physics2D 컴포넌트(Rigidbody, Joint)를 제거하고 수학적 진자 운동 시스템 도입
    /// </summary>
    public class ClawView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("좌우로 주행하는 천장 카트의 RectTransform 객체입니다.")]
        private RectTransform m_clawRoot;

        [SerializeField]
        [Tooltip("절차적 제어를 받는 실제 집게 헤드 View 객체입니다.")]
        private ClawBodyView m_clawBody;

        [SerializeField]
        [Tooltip("UI 와이어의 시각적 색상입니다.")]
        private Color m_wireColor = Color.white;

        [SerializeField]
        [Tooltip("상승 완료 후 밀착 상태의 최소 줄 길이입니다.")]
        private float m_minRopeDistance = 50f;

        [SerializeField]
        [Tooltip("집게 하강 시 와이어 줄이 늘어날 최대 길이입니다.")]
        private float m_maxRopeDistance = 500f;

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
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private bool m_isMoving;
        private float m_moveDirection; // -1: Left, 1: Right, 0: Idle
        private float m_moveSpeed = 300f;
        
        private Vector2 m_initialPosition;
        private RectTransform m_uiWireRect;
        private System.Threading.CancellationTokenSource m_animCts;

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
                m_initialPosition = m_clawRoot.anchoredPosition;
                m_prevCartX = m_clawRoot.anchoredPosition.x;
            }

            // 초기 줄 길이 및 각도 설정
            m_currentRopeLength = m_minRopeDistance;
            m_currentAngle = 0f;
            m_angularVelocity = 0f;

            if (m_clawBody != null)
            {
                m_clawBody.SetClawsOpenImmediately();
            }

            // UI 와이어 객체 동적 생성
            InitializeUIWire();
        }

        private void Update()
        {
            UpdateCartMovement();
            UpdatePendulumPhysics();
        }

        private void LateUpdate()
        {
            RenderClawAndWire();
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
                m_clawBody.Initialize(m_viewModel);
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

        private void InitializeUIWire()
        {
            GameObject wireObj = new GameObject("UI_Wire", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            wireObj.transform.SetParent(m_clawRoot, false);
            m_uiWireRect = wireObj.GetComponent<RectTransform>();
            m_uiWireRect.pivot = new Vector2(0.5f, 1f); 
            m_uiWireRect.anchorMin = new Vector2(0.5f, 0.5f);
            m_uiWireRect.anchorMax = new Vector2(0.5f, 0.5f);
            m_uiWireRect.localPosition = Vector3.zero;
            
            UnityEngine.UI.Image wireImg = wireObj.GetComponent<UnityEngine.UI.Image>();
            wireImg.color = m_wireColor;
            wireImg.raycastTarget = false;
        }
        #endregion

        #region 절차적 물리 엔진 (Procedural Physics Engine)
        private void UpdateCartMovement()
        {
            if (m_isMoving && m_clawRoot != null)
            {
                Vector2 pos = m_clawRoot.anchoredPosition;
                pos.x += m_moveDirection * m_moveSpeed * Time.deltaTime;
                m_clawRoot.anchoredPosition = pos;
            }
        }

        private void UpdatePendulumPhysics()
        {
            if (m_clawRoot == null) return;

            // 1. 카트의 가속도 계산 (프레임 간 좌표 차이)
            float currentCartX = m_clawRoot.anchoredPosition.x;
            float cartDeltaX = (currentCartX - m_prevCartX) / Time.deltaTime;
            
            // 가속도 변화량에 따른 관성 부여 (카트가 움직일 때 반대 방향으로 힘을 받음)
            // 실제 물리와 유사하게 현재 속도 자체가 아닌 가속도/속도 변화에 반응하도록 설계
            float inertiaForce = -cartDeltaX * m_swingSensitivity * 0.01f;

            // 2. 진자 운동 수식 (삼각함수 미분 근사)
            // 중력 복원력: -sin(각도) * gravity
            float gravityForce = -Mathf.Sin(m_currentAngle * Mathf.Deg2Rad) * m_swingGravity;
            
            // 각속도 갱신
            m_angularVelocity += (inertiaForce + gravityForce) * Time.deltaTime * 10f;
            m_angularVelocity *= m_swingDamping; // 공기 저항 감쇄

            // 각도 갱신
            m_currentAngle += m_angularVelocity * Time.deltaTime * 50f;
            
            // 최대 각도 제한
            m_currentAngle = Mathf.Clamp(m_currentAngle, -m_maxSwingAngle, m_maxSwingAngle);

            m_prevCartX = currentCartX;
        }

        private void RenderClawAndWire()
        {
            if (m_clawBody == null || m_clawRoot == null || m_uiWireRect == null) return;

            // 1. 집게 헤드 위치 계산 (진자 각도와 줄 길이를 이용한 원호 이동)
            // 하향 벡터를 현재 각도만큼 회전
            Quaternion rotation = Quaternion.Euler(0, 0, m_currentAngle);
            Vector3 offset = rotation * Vector3.down * m_currentRopeLength;
            
            // World Space 기준으로 변환 (Canvas 스케일 고려)
            m_clawBody.transform.position = m_clawRoot.position + (offset * m_clawRoot.lossyScale.y);
            
            // 2. 집게 헤드 회전 (줄의 각도와 일치시킴)
            m_clawBody.transform.rotation = m_clawRoot.rotation * rotation;

            // 3. UI 와이어 렌더링
            m_uiWireRect.sizeDelta = new Vector2(6f, m_currentRopeLength);
            m_uiWireRect.localRotation = rotation;
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
            CancelAnimations();
            m_animCts = new System.Threading.CancellationTokenSource();
            System.Threading.CancellationToken token = m_animCts.Token;

            switch (newState)
            {
                case ClawStateType.Descending:
                    PlayDescendAnimation(token).Forget();
                    break;
                case ClawStateType.Grabbing:
                    PlayGrabAnimation(token).Forget();
                    break;
                case ClawStateType.Ascending:
                    PlayAscendAnimation(token).Forget();
                    break;
                case ClawStateType.Returning:
                    PlayReturnAnimation(token).Forget();
                    break;
                case ClawStateType.Result:
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
            if (m_clawBody != null) m_clawBody.OpenClaws();

            float elapsed = 0f;
            float startLen = m_currentRopeLength;

            while (elapsed < m_descendDuration)
            {
                elapsed += Time.deltaTime;
                m_currentRopeLength = Mathf.Lerp(startLen, m_maxRopeDistance, elapsed / m_descendDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            m_currentRopeLength = m_maxRopeDistance;

            // 안착 대기 (절차적 방식이므로 속도 체크 대신 짧은 시간 대기)
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
            float startLen = m_currentRopeLength;

            while (elapsed < m_ascendDuration)
            {
                elapsed += Time.deltaTime;
                m_currentRopeLength = Mathf.Lerp(startLen, m_minRopeDistance, elapsed / m_ascendDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            m_currentRopeLength = m_minRopeDistance;

            if (m_viewModel != null) m_viewModel.NotifyAscendCompleted();
        }

        private async UniTaskVoid PlayReturnAnimation(System.Threading.CancellationToken token)
        {
            if (m_clawRoot == null) return;

            // [수정]: DOAnchoredMoveX -> DOAnchorPosX (DOTween RectTransform 전용 메서드)
            await m_clawRoot.DOAnchorPosX(m_initialPosition.x, 1.5f)
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
