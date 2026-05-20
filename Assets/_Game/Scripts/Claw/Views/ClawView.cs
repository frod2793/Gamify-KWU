using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using GameArifiction.DTO;
using GamifyKWU.CraneGame.View;

namespace GameArifiction.Claw
{


    /// <summary>
    /// [기능]: 3버튼 UI 이벤트 및 New Input System 입력을 뷰모델에 위임하고, HingeJoint2D 물리 모터 및 Rigidbody2D를 제어하는 WebGL 최적화 유니티 뷰 컴포넌트
    /// [작성자]: [Senior Client Developer]
    /// </summary>
    public class ClawView : MonoBehaviour
    {
        #region UI 및 컴포넌트 참조 (Inspector)

        [Header("집게 물리 리지드 컴포넌트 참조")]
        [SerializeField] 
        [Tooltip("천장 캐리지 및 집게 장치의 최상위 물리 몸체인 Rigidbody2D 컴포넌트입니다.")]
        private Rigidbody2D m_baseRigidbody;

        [SerializeField] 
        [Tooltip("왼쪽 집게다리의 회전 관절 모터 제어를 담당하는 HingeJoint2D 컴포넌트입니다.")]
        private HingeJoint2D m_leftArmJoint;

        [SerializeField] 
        [Tooltip("오른쪽 집게다리의 회전 관절 모터 제어를 담당하는 HingeJoint2D 컴포넌트입니다.")]
        private HingeJoint2D m_rightArmJoint;

        [Header("수평 이동 로컬 경계 제한")]
        [SerializeField] 
        [Tooltip("로컬 좌표 기준 집게가 좌측으로 이동할 수 있는 최대 한계 값입니다.")]
        private float m_minX = -903.0f;

        [SerializeField] 
        [Tooltip("로컬 좌표 기준 집게가 우측으로 이동할 수 있는 최대 한계 값입니다.")]
        private float m_maxX = 760.0f;

        [SerializeField]
        [Tooltip("집게의 수평 이동 속도 조절 매개변수입니다.")]
        private float m_moveSpeed = 300.0f;

        [Header("수동 투하 설정")]
        [SerializeField] 
        [Tooltip("투하지점에서 집게를 열고 대기하는 연출 대기 시간(초 단위)입니다.")]
        private float m_dropWaitTime = 1.0f;

        [Header("하강 및 충돌 센싱 설정")]
        [SerializeField]
        [Tooltip("최대 하강 허용 시간(초)입니다. 바닥/오브젝트 미충돌 시 이 시간이 지나면 상승으로 자동 전환됩니다.")]
        private float m_maxDescendDuration = 4.0f;

        [SerializeField]
        [Tooltip("아래쪽 장애물(바닥/캡슐)을 감지할 레이캐스트 최대 사거리입니다.")]
        private float m_raycastDistance = 60.0f;

        [SerializeField]
        [Tooltip("레이캐스트 및 물리 센싱이 감지할 레이어 마스크입니다.")]
        private LayerMask m_collisionLayers;

        [Header("신규 입력 시스템 인풋 액션")]
        [SerializeField]
        [Tooltip("집게의 좌우 이동을 제어하는 인풋 액션 레퍼런스입니다.")]
        private InputActionReference m_moveAction;

        [SerializeField]
        [Tooltip("집게를 강하 및 집기 작동시키는 인풋 액션 레퍼런스입니다.")]
        private InputActionReference m_grabAction;

        [Header("좌우 개별 물리 모터 반전 안전장치")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_invertMotorDirection")]
        [Tooltip("왼쪽 집게의 오므라드는 모터 방향을 반전시킵니다.")]
        private bool m_invertLeftMotor = false;

        [SerializeField]
        [Tooltip("오른쪽 집게의 오므라드는 모터 방향을 반전시킵니다.")]
        private bool m_invertRightMotor = false;

        [Header("집게 배속 속도 제어")]
        [SerializeField]
        [Range(0.5f, 10.0f)]
        [Tooltip("집게가 오므라들 때(집을 때) 기본 속도에 곱해질 배율입니다.")]
        private float m_grabSpeedMultiplier = 2.0f;

        [SerializeField]
        [Range(0.5f, 10.0f)]
        [Tooltip("집게가 벌어질 때(필 때) 기본 속도에 곱해질 배율입니다.")]
        private float m_releaseSpeedMultiplier = 3.0f;

        #endregion

        #region 내부 필드 (Private Fields)

        private ClawViewModel m_viewModel;
        private CancellationTokenSource m_cts;
        private float m_lastInputX = 0f;
        private bool m_hasHitObstacle = false;
        private bool m_testGrabState = false;

        private Vector3 m_leftOriginalRotation;
        private Vector3 m_rightOriginalRotation;
        private Vector3 m_leftGrabbedRotation;
        private Vector3 m_rightGrabbedRotation;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            InitializeMVVM();
            PerformSelfDiagnosisAndCorrection();
        }

        private void OnEnable()
        {
            RegisterInputActions();
        }

        private void Start()
        {
            // 초기 벌려진 기본 로컬 회전 각도를 디자이너 세팅 프리팹 기준으로 저장
            if (m_leftArmJoint != null)
            {
                m_leftOriginalRotation = m_leftArmJoint.transform.localEulerAngles;
            }

            if (m_rightArmJoint != null)
            {
                m_rightOriginalRotation = m_rightArmJoint.transform.localEulerAngles;
            }

            // 초기 물리 고정을 Kinematic 벌림 상태로 기동
            if (m_viewModel != null)
            {
                m_viewModel.ControlClawPhysics(grab: false);
                UpdateClawPhysicsMode(m_viewModel.CurrentState);
            }
        }

        private void Update()
        {
            if (m_viewModel != null)
            {
                PollInputSystemValues();
                m_viewModel.UpdateMovement(Time.deltaTime);
            }

            // H 키 디버그 수동 오므리기 테스트 핫키 감지 (뷰모델의 FSM 잠금 상태와 무관하게 작동)
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                func_TestToggleGrab();
            }
        }

        private void OnDisable()
        {
            UnregisterInputActions();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            CancelCurrentSequence();
        }

        #endregion

        #region 초기화 (Initialization)

        private void InitializeMVVM()
        {
            var model = new ClawModel
            {
                MinXLimit = m_minX,
                MaxXLimit = m_maxX,
                HorizontalSpeed = m_moveSpeed
            };

            // 뷰모델의 초기 좌표 상태를 현재 로컬 좌표 기준으로 대입
            m_viewModel = new ClawViewModel(model, transform.localPosition);

            // HingeJoint2D의 connectedBody를 베이스 Rigidbody에 강제 연결하여 부모 트랜스폼 이동 종속성 확보
            if (m_baseRigidbody != null)
            {
                if (m_leftArmJoint != null)
                {
                    m_leftArmJoint.connectedBody = m_baseRigidbody;
                }

                if (m_rightArmJoint != null)
                {
                    m_rightArmJoint.connectedBody = m_baseRigidbody;
                }
            }

            m_viewModel.OnPositionChanged += UpdateBasePosition;
            m_viewModel.OnStateChanged += HandleStateChanged;
            m_viewModel.OnMotorTriggered += HandleMotorAction;
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnPositionChanged -= UpdateBasePosition;
                m_viewModel.OnStateChanged -= HandleStateChanged;
                m_viewModel.OnMotorTriggered -= HandleMotorAction;
            }
        }

        /// <summary>
        /// [기능]: 집게 물리 컴포넌트(Rigidbody2D, HingeJoint2D)의 세팅 무결성을 실시간 자가진단하고 오세팅을 자동 강제 교정합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-21
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 좌우 집게다리의 회전 한계 각도(Limits), 질량(Mass), 댐핑(Damping), 중력 배율 등을 강제로 완벽하게 좌우 대칭으로 일치시켜 시각적 속도 편차 문제를 근본적으로 해결했습니다.
        /// </summary>
        private void PerformSelfDiagnosisAndCorrection()
        {
            const float targetMass = 1.0f;
            const float targetLinearDamping = 5.0f;
            const float targetAngularDamping = 15.0f;
            const float targetGravityScale = 1.0f;
            const float targetLimitMin = -50.0f;
            const float targetLimitMax = 50.0f;

            // 1. 왼쪽 집게 물리 진단 및 자동 교정
            if (m_leftArmJoint != null)
            {
                if (m_leftArmJoint.enableCollision)
                {
                    m_leftArmJoint.enableCollision = false;
                    Debug.Log("[ClawView] 왼쪽 HingeJoint2D의 Enable Collision이 활성화되어 있어, 부품 간 충돌 끼임 예방을 위해 자동으로 OFF 처리했습니다.");
                }

                // 좌우 비대칭 각도로 인해 시각적 펄럭임 및 속도 편차가 발생하는 것을 방지하기 위해 Limits 각도 강제 통일
                if (m_leftArmJoint.useLimits)
                {
                    JointAngleLimits2D limits = m_leftArmJoint.limits;
                    limits.min = targetLimitMin;
                    limits.max = targetLimitMax;
                    m_leftArmJoint.limits = limits;
                    Debug.Log($"[ClawView] 왼쪽 HingeJoint2D의 Limits 회전 한계를 좌우 대칭 정합성을 위해 {targetLimitMin}도 ~ {targetLimitMax}도로 강제 조정했습니다.");
                }

                Rigidbody2D armRb = m_leftArmJoint.GetComponent<Rigidbody2D>();
                if (armRb != null)
                {
                    if (armRb.bodyType != RigidbodyType2D.Dynamic)
                    {
                        armRb.bodyType = RigidbodyType2D.Dynamic;
                        Debug.LogWarning("[ClawView] 왼쪽 집게다리의 Rigidbody2D BodyType이 Dynamic이 아니어서 런타임에 자동으로 Dynamic으로 교정했습니다.");
                    }

                    if ((armRb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0)
                    {
                        armRb.constraints = RigidbodyConstraints2D.None;
                        Debug.LogWarning("[ClawView] 왼쪽 집게다리의 Rigidbody2D Constraints에 FreezeRotation이 켜져 있어, 회전 활성화를 위해 자동으로 락을 해제했습니다.");
                    }

                    // 무게, 드래그, 중력 배율을 좌우 완벽히 통일하여 운동 방정식의 오차 극복
                    armRb.mass = targetMass;
                    armRb.linearDamping = targetLinearDamping;
                    armRb.angularDamping = targetAngularDamping;
                    armRb.gravityScale = targetGravityScale;
                }
            }

            // 2. 오른쪽 집게 물리 진단 및 자동 교정
            if (m_rightArmJoint != null)
            {
                if (m_rightArmJoint.enableCollision)
                {
                    m_rightArmJoint.enableCollision = false;
                    Debug.Log("[ClawView] 오른쪽 HingeJoint2D의 Enable Collision이 활성화되어 있어, 부품 간 충돌 끼임 예방을 위해 자동으로 OFF 처리했습니다.");
                }

                // 좌우 비대칭 각도로 인해 시각적 펄럭임 및 속도 편차가 발생하는 것을 방지하기 위해 Limits 각도 강제 통일
                if (m_rightArmJoint.useLimits)
                {
                    JointAngleLimits2D limits = m_rightArmJoint.limits;
                    limits.min = targetLimitMin;
                    limits.max = targetLimitMax;
                    m_rightArmJoint.limits = limits;
                    Debug.Log($"[ClawView] 오른쪽 HingeJoint2D의 Limits 회전 한계를 좌우 대칭 정합성을 위해 {targetLimitMin}도 ~ {targetLimitMax}도로 강제 조정했습니다.");
                }

                Rigidbody2D armRb = m_rightArmJoint.GetComponent<Rigidbody2D>();
                if (armRb != null)
                {
                    if (armRb.bodyType != RigidbodyType2D.Dynamic)
                    {
                        armRb.bodyType = RigidbodyType2D.Dynamic;
                        Debug.LogWarning("[ClawView] 오른쪽 집게다리의 Rigidbody2D BodyType이 Dynamic이 아니어서 런타임에 자동으로 Dynamic으로 교정했습니다.");
                    }

                    if ((armRb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0)
                    {
                        armRb.constraints = RigidbodyConstraints2D.None;
                        Debug.LogWarning("[ClawView] 오른쪽 집게다리의 Rigidbody2D Constraints에 FreezeRotation이 켜져 있어, 회전 활성화를 위해 자동으로 락을 해제했습니다.");
                    }

                    // 무게, 드래그, 중력 배율을 좌우 완벽히 통일하여 운동 방정식의 오차 극복
                    armRb.mass = targetMass;
                    armRb.linearDamping = targetLinearDamping;
                    armRb.angularDamping = targetAngularDamping;
                    armRb.gravityScale = targetGravityScale;
                }
            }
        }

        #endregion

        #region New Input System 바인딩 처리

        private void RegisterInputActions()
        {
            if (m_moveAction != null && m_moveAction.action != null)
            {
                m_moveAction.action.Enable();
            }

            if (m_grabAction != null && m_grabAction.action != null)
            {
                m_grabAction.action.Enable();
                m_grabAction.action.performed += OnGrabActionExecuted;
            }
        }

        private void UnregisterInputActions()
        {
            if (m_moveAction != null && m_moveAction.action != null)
            {
                m_moveAction.action.Disable();
            }

            if (m_grabAction != null && m_grabAction.action != null)
            {
                if (m_grabAction.action != null)
                {
                    m_grabAction.action.performed -= OnGrabActionExecuted;
                }
                m_grabAction.action.Disable();
            }
        }

        private void PollInputSystemValues()
        {
            if (m_moveAction != null && m_moveAction.action != null)
            {
                var rawValue = m_moveAction.action.ReadValue<Vector2>();
                float inputX = rawValue.x;

                // 유효한 입력 값이 들어올 때만 방향 상태 갱신
                if (!Mathf.Approximately(inputX, 0f))
                {
                    m_viewModel.SetInputDirection(inputX);
                    m_lastInputX = inputX;
                }
                // 입력이 0이 되는 시점(키 해제)을 감지하여 1회 정지 신호 송신 및 상태 초기화
                else if (!Mathf.Approximately(m_lastInputX, 0f))
                {
                    m_viewModel.SetInputDirection(0f);
                    m_lastInputX = 0f;
                }
            }
        }

        private void OnGrabActionExecuted(InputAction.CallbackContext context)
        {
            if (m_viewModel != null)
            {
                if (m_viewModel.IsHolding)
                {
                    func_OnReleaseButtonClicked();
                }
                else
                {
                    func_OnGrabButtonClicked();
                }
            }
        }

        #endregion

        #region 3버튼 UI용 이벤트 콜백 (UI 3-Button Callbacks)

        public void func_OnLeftButtonDown()
        {
            if (m_viewModel != null)
            {
                m_viewModel.SetInputDirection(-1f);
            }
        }

        public void func_OnLeftButtonUp()
        {
            if (m_viewModel != null)
            {
                m_viewModel.SetInputDirection(0f);
            }
        }

        public void func_OnRightButtonDown()
        {
            if (m_viewModel != null)
            {
                m_viewModel.SetInputDirection(1f);
            }
        }

        public void func_OnRightButtonUp()
        {
            if (m_viewModel != null)
            {
                m_viewModel.SetInputDirection(0f);
            }
        }

        public void func_OnGrabButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.ExecuteGrab();
            }
        }

        public void func_OnReleaseButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.ExecuteRelease();
            }
        }

        /// <summary>
        /// [기능]: H 키 입력을 수신하여 집게 오므리기/벌리기를 강제로 테스트하고 상세 물리 정보(각도,Constraints,모터속도)를 100% 한글 디버그 로그로 콘솔에 실시간 로깅합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-21
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Kinematic 고정 락이 도입됨에 따라 H 키 디버그 시 강제로 Rigidbody2D BodyType을 Dynamic으로 강제 복구하고 Constraints를 완전히 해제하여 독립적 물리 수동 제어 테스트 환경이 무결하게 기동하도록 수정했습니다.
        /// </summary>
        public void func_TestToggleGrab()
        {
            m_testGrabState = !m_testGrabState;
            Debug.Log($"[ClawView] [H 키 디버그] 집게 수동 테스트 가동 - 목표 오므리기 작동 상태: {m_testGrabState}");

            // H 키 디버그 작동 시 일시적으로 집게발 Rigidbody2D를 Dynamic으로 복구하여 회전이 가능하게 함
            if (m_leftArmJoint != null)
            {
                Rigidbody2D leftRb = m_leftArmJoint.GetComponent<Rigidbody2D>();
                if (leftRb != null)
                {
                    leftRb.bodyType = RigidbodyType2D.Dynamic;
                    leftRb.constraints = RigidbodyConstraints2D.None;
                }
            }

            if (m_rightArmJoint != null)
            {
                Rigidbody2D rightRb = m_rightArmJoint.GetComponent<Rigidbody2D>();
                if (rightRb != null)
                {
                    rightRb.bodyType = RigidbodyType2D.Dynamic;
                    rightRb.constraints = RigidbodyConstraints2D.None;
                }
            }

            // 뷰모델을 거치지 않고 직접 모터 작동 트리거
            // 오므릴 때는 300f의 디폴트 물리 스피드와 최대 10000f의 강력한 max torque를 적용하여 댐핑 항력을 격파합니다.
            float speed = m_testGrabState ? 300.0f : -300.0f;
            float torque = m_testGrabState ? 10000.0f : 1500.0f; 

            // 실제 물리 모터 강제 대입
            HandleMotorAction(speed, torque, useMotor: true);

            // 실시간 물리 상태 콘솔 디버깅
            if (m_leftArmJoint != null)
            {
                Rigidbody2D leftRb = m_leftArmJoint.GetComponent<Rigidbody2D>();
                float jointAngle = m_leftArmJoint.jointAngle;
                string constraintsStr = leftRb != null ? leftRb.constraints.ToString() : "없음";
                string bodyTypeStr = leftRb != null ? leftRb.bodyType.ToString() : "없음";
                
                Debug.Log($"[ClawView] [H 키 디버그] 왼쪽다리 - 조인트각도: {jointAngle:F2}도, 트랜스폼각도: {m_leftArmJoint.transform.localEulerAngles.z:F2}도, useMotor: {m_leftArmJoint.useMotor}, motorSpeed: {m_leftArmJoint.motor.motorSpeed}, Constraints: {constraintsStr}, BodyType: {bodyTypeStr}");
            }
            else
            {
                Debug.LogWarning("[ClawView] [H 키 디버그] 왼쪽 HingeJoint2D가 인스펙터에 없습니다.");
            }

            if (m_rightArmJoint != null)
            {
                Rigidbody2D rightRb = m_rightArmJoint.GetComponent<Rigidbody2D>();
                float jointAngle = m_rightArmJoint.jointAngle;
                string constraintsStr = rightRb != null ? rightRb.constraints.ToString() : "없음";
                string bodyTypeStr = rightRb != null ? rightRb.bodyType.ToString() : "없음";
                
                Debug.Log($"[ClawView] [H 키 디버그] 오른쪽다리 - 조인트각도: {jointAngle:F2}도, 트랜스폼각도: {m_rightArmJoint.transform.localEulerAngles.z:F2}도, useMotor: {m_rightArmJoint.useMotor}, motorSpeed: {m_rightArmJoint.motor.motorSpeed}, Constraints: {constraintsStr}, BodyType: {bodyTypeStr}");
            }
            else
            {
                Debug.LogWarning("[ClawView] [H 키 디버그] 오른쪽 HingeJoint2D가 인스펙터에 없습니다.");
            }
        }

        #endregion

        #region 외부 의존성 주입 (External Dependency Injection)

        public void InjectQuizResult(QuizStatsDTO stats)
        {
            if (m_viewModel != null && stats != null)
            {
                m_viewModel.ApplyQuizStats(stats);
            }
        }

        #endregion

        #region 내부 물리 작동 및 널 체크 가드 (Private Physics Operations)

        private void UpdateBasePosition(Vector2 newLocalPosition)
        {
            Vector3 targetLocalPos = new Vector3(newLocalPosition.x, newLocalPosition.y, transform.localPosition.z);
            Vector3 targetWorldPos = targetLocalPos;

            if (transform.parent != null)
            {
                targetWorldPos = transform.parent.TransformPoint(targetLocalPos);
            }

            if (m_baseRigidbody != null)
            {
                m_baseRigidbody.MovePosition(targetWorldPos);
            }
            else
            {
                transform.localPosition = targetLocalPos;
            }
        }

        /// <summary>
        /// [기능]: 뷰모델의 그랩 상태 변경 이벤트를 수신하여 HingeJoint2D 물리 모터 속도 및 한계 토크를 제어합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-21
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Kinematic 동적 스위칭 고정 락이 도입됨에 따라 불필요해진 모터 상시 벌림 강제 제어 코드를 폐지하고, 순수한 속도 증폭 스케일러와 물리 대칭 대입으로 완벽히 리팩토링했습니다.
        /// </summary>
        private void HandleMotorAction(float speed, float torque, bool useMotor)
        {
            // speed는 grab이 true 일 때 DefaultMotorSpeed(300f), grab이 false 일 때 -DefaultMotorSpeed(-300f) 임
            // [속도 배속 필터 적용]: 인스펙터에서 지정한 배수 필터를 거쳐 오므릴 때(speed > 0)와 벌어질(펴질) 때(speed < 0)의 속도를 실시간 증폭합니다.
            float scaledSpeed = speed > 0.0f ? speed * m_grabSpeedMultiplier : speed * m_releaseSpeedMultiplier;

            // 왼쪽 모터 최종 속도 결정:
            // 왼쪽 집게는 오므라들기 위해 반시계 방향(각도 증가, motorSpeed는 음수)으로 회전해야 함.
            // 따라서 기본 속도 scaledSpeed(양수)에 음의 부호를 붙인 -scaledSpeed 방향이 기본 작동 방향임.
            float leftMultiplier = m_invertLeftMotor ? -1.0f : 1.0f;
            float leftSpeed = -scaledSpeed * leftMultiplier;

            // 오른쪽 모터 최종 속도 결정:
            // 오른쪽 집게는 오므라들기 위해 시계 방향(각도 감소, motorSpeed는 양수)으로 회전해야 함.
            // 따라서 기본 속도 scaledSpeed(양수) 방향이 기본 작동 방향임.
            float rightMultiplier = m_invertRightMotor ? -1.0f : 1.0f;
            float rightSpeed = scaledSpeed * rightMultiplier;

            Debug.Log($"[ClawView] 물리 모터 액션 수신 - 기본속도: {speed}, 배속필터속도: {scaledSpeed}, 왼쪽최종속도: {leftSpeed}, 오른쪽최종속도: {rightSpeed}, 적용토크: {torque}, useMotor: {useMotor}");

            if (m_leftArmJoint != null)
            {
                JointMotor2D motor = m_leftArmJoint.motor;
                motor.motorSpeed = leftSpeed;
                motor.maxMotorTorque = torque;
                m_leftArmJoint.motor = motor;
                m_leftArmJoint.useMotor = useMotor;
                Debug.Log($"[ClawView] 왼쪽 물리 힌지 모터 대입 완료 - 실제속도: {m_leftArmJoint.motor.motorSpeed}, 실제useMotor: {m_leftArmJoint.useMotor}");
            }
            else
            {
                Debug.LogWarning("[ClawView] 왼쪽 집게 HingeJoint2D 컴포넌트가 인스펙터 상에서 유실되었습니다!");
            }

            if (m_rightArmJoint != null)
            {
                JointMotor2D motor = m_rightArmJoint.motor;
                motor.motorSpeed = rightSpeed;
                motor.maxMotorTorque = torque;
                m_rightArmJoint.motor = motor;
                m_rightArmJoint.useMotor = useMotor;
                Debug.Log($"[ClawView] 오른쪽 물리 힌지 모터 대입 완료 - 실제속도: {m_rightArmJoint.motor.motorSpeed}, 실제useMotor: {m_rightArmJoint.useMotor}");
            }
            else
            {
                Debug.LogWarning("[ClawView] 오른쪽 집게 HingeJoint2D 컴포넌트가 인스펙터 상에서 유실되었습니다!");
            }
        }

        /// <summary>
        /// [기능]: 캡슐을 쥐고 이동하거나 대기/강하 중일 때 집게다리의 Z축 회전을 강제로 완전히 잠가 물리 관성 펄럭임을 100% 원천 진압합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-21
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 기존의 수평 이동 중에만 회전을 잠그던 한계를 넘어, 오직 물리 모터가 실제로 움직이는 시퀀스(GRABBING, RELEASING) 순간을 제외하고는 항상 Z축 회전을 강제로 FreezeRotation 처리하여 이동 중단 및 급정거 시의 관성 흔들림까지 완벽 진압합니다.
        /// </summary>
        /// <summary>
        /// [기능]: 현재 집게 상태에 따라 좌우 집게다리의 Rigidbody2D 물리 방식을 Dynamic(모터 회전) 또는 Kinematic(흔들림 0% 완벽 고정)으로 동적 스위칭하고 로컬 각도를 강제 스냅합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-21
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 기존의 불완전한 FreezeRotation 및 모터 상시 벌림 방식을 폐지하고, 물리 시뮬레이션이 필요 없는 상태에서는 Rigidbody2D를 Kinematic으로 전환하여 관성 흔들림(팔랑임)을 0%로 완벽 차단하며, 오직 오므리기(GRABBING)와 펴기(RELEASING) 순간에만 Dynamic으로 전환하는 강력한 하이브리드 물리 고정 아키텍처를 도입했습니다.
        /// </summary>
        private void UpdateClawPhysicsMode(ClawState state)
        {
            if (m_leftArmJoint == null || m_rightArmJoint == null)
            {
                return;
            }

            Rigidbody2D leftRb = m_leftArmJoint.GetComponent<Rigidbody2D>();
            Rigidbody2D rightRb = m_rightArmJoint.GetComponent<Rigidbody2D>();

            if (leftRb == null || rightRb == null)
            {
                return;
            }

            // 오직 물리 모터가 돌면서 실제로 벌어지거나 오므라들어야 하는 상태에서만 Dynamic
            bool isPhysicsActive = (state == ClawState.GRABBING || state == ClawState.RELEASING);

            if (isPhysicsActive)
            {
                leftRb.bodyType = RigidbodyType2D.Dynamic;
                rightRb.bodyType = RigidbodyType2D.Dynamic;

                leftRb.constraints = RigidbodyConstraints2D.None;
                rightRb.constraints = RigidbodyConstraints2D.None;

                Debug.Log($"[ClawView] 집게다리 물리 활성화 (Dynamic): {state}");
            }
            else
            {
                // 그랩 완료 직후 상승으로의 전이 시점에 현재 다리가 위치한 실시간 물리 각도를 움켜쥔 각도로 캡처
                if (state == ClawState.ASCENDING || state == ClawState.MOVING_TO_DROP)
                {
                    m_leftGrabbedRotation = m_leftArmJoint.transform.localEulerAngles;
                    m_rightGrabbedRotation = m_rightArmJoint.transform.localEulerAngles;
                    Debug.Log($"[ClawView] 움켜쥔 물리 각도 캡처 완료 - 왼쪽: {m_leftGrabbedRotation.z:F2}도, 오른쪽: {m_rightGrabbedRotation.z:F2}도");
                }

                leftRb.bodyType = RigidbodyType2D.Kinematic;
                rightRb.bodyType = RigidbodyType2D.Kinematic;

                leftRb.linearVelocity = Vector2.zero;
                leftRb.angularVelocity = 0f;
                rightRb.linearVelocity = Vector2.zero;
                rightRb.angularVelocity = 0f;

                // 상태별 각도 정렬 (Snap)
                if (m_viewModel != null && m_viewModel.IsHolding)
                {
                    m_leftArmJoint.transform.localEulerAngles = m_leftGrabbedRotation;
                    m_rightArmJoint.transform.localEulerAngles = m_rightGrabbedRotation;
                    Debug.Log("[ClawView] 집게다리 Kinematic 잠금 실행 - 움켜쥔 각도 고정 완료");
                }
                else
                {
                    m_leftArmJoint.transform.localEulerAngles = m_leftOriginalRotation;
                    m_rightArmJoint.transform.localEulerAngles = m_rightOriginalRotation;
                    Debug.Log("[ClawView] 집게다리 Kinematic 잠금 실행 - 초기 벌림 각도 고정 완료");
                }
            }
        }

        private void HandleStateChanged(ClawState state)
        {
            Debug.Log($"[ClawView] 집게 메커니즘 상태 변경: {state}");

            UpdateClawPhysicsMode(state); // 신규 Kinematic / Dynamic 물리 방식 스위칭 및 스냅

            switch (state)
            {
                case ClawState.DESCENDING:
                    ResetCancellationTokenSource();
                    ClawGrabSequenceAsync(m_cts.Token).Forget();
                    break;
                case ClawState.RELEASING:
                    ResetCancellationTokenSource();
                    ClawReleaseSequenceAsync(m_cts.Token).Forget();
                    break;
            }
        }

        private void ResetCancellationTokenSource()
        {
            CancelCurrentSequence();
            m_cts = new CancellationTokenSource();
        }

        private void CancelCurrentSequence()
        {
            if (m_cts != null)
            {
                m_cts.Cancel();
                m_cts.Dispose();
                m_cts = null;
            }
        }



        /// <summary>
        /// [기능]: 집게 하강 -> 물리 집게 오므리기 -> 상승하여 제자리 복귀 후 조작권 대기 1단계 시퀀스 (시간 기반 안정화 보간법 적용)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-20
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 하강이 바닥이나 캡슐에 닿을 때까지 이어지도록 최대 4.0초의 넉넉한 하강 사거리를 부여하고, 레이캐스트 및 트리거/콜리전 2중 물리 감지 시 즉시 찝는 하이브리드 제어법을 장착했습니다.
        /// </summary>
        private async UniTaskVoid ClawGrabSequenceAsync(CancellationToken token)
        {
            if (m_viewModel == null)
            {
                return;
            }

            Debug.Log("[ClawView] 1단계 하강(DESCENDING) 시퀀스를 개시합니다.");
            float initialLocalY = transform.localPosition.y;
            float descendSpeed = m_viewModel.DescendSpeed;
            float elapsedTime = 0f;

            m_hasHitObstacle = false;
            int mask = m_collisionLayers.value == 0 ? ~0 : m_collisionLayers.value;

            // 1. DESCENDING (강하 - 레이캐스트 및 2중 물리 센싱 하이브리드 제어)
            while (elapsedTime < m_maxDescendDuration && !m_hasHitObstacle)
            {
                elapsedTime += Time.deltaTime;
                Vector3 newLocalPos = transform.localPosition;
                newLocalPos.y -= descendSpeed * Time.deltaTime;
                m_viewModel.SetPositionDirectly(newLocalPos);

                // 아래 방향으로 레이를 쏘아 바닥/캡슐 사전 검지
                // 자기 부품에 부딪치지 않도록 시작점을 transform.position보다 y축으로 살짝 아래에서 쏩니다.
                Vector2 rayStart = (Vector2)transform.position + (Vector2.down * 40f);
                RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, m_raycastDistance, mask);

                if (hit.collider != null)
                {
                    // 충돌한 대상이 집게 자신의 부품이 아닐 경우에만 조기 충돌 감지 탈출
                    if (!IsClawSelfComponent(hit.collider.gameObject))
                    {
                        Debug.Log($"[ClawView] 하강 중 레이캐스트 사전 장애물 포착: {hit.collider.gameObject.name}, 거리: {hit.distance}");
                        m_hasHitObstacle = true;
                        break;
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // 2. GRABBING (집기 작동 및 대기)
            Debug.Log("[ClawView] 2단계 그랩(GRABBING) 상태 전이 및 집게 오므리기 물리 모터 트리거 작동.");
            m_viewModel.ChangeState(ClawState.GRABBING);
            m_viewModel.ControlClawPhysics(grab: true);
            await UniTask.Delay(System.TimeSpan.FromSeconds(1.5f), cancellationToken: token);

            // 3. ASCENDING (상승 - 시간 보간 제어로 무한락 방지)
            Debug.Log("[ClawView] 3단계 상승(ASCENDING) 상태 전이 및 초기 위치 복원 개시.");
            m_viewModel.ChangeState(ClawState.ASCENDING);
            
            // 실제 내려간 거리만큼만 대칭으로 상승하도록 연산
            float actualDescendedDuration = elapsedTime;
            elapsedTime = 0f;
            while (elapsedTime < actualDescendedDuration)
            {
                elapsedTime += Time.deltaTime;
                Vector3 newLocalPos = transform.localPosition;
                newLocalPos.y += descendSpeed * Time.deltaTime;
                m_viewModel.SetPositionDirectly(newLocalPos);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // 오차 보정을 위해 최종 Y 좌표를 완벽히 초기 높이로 강제 대입
            Vector3 finalHeightLocalPos = transform.localPosition;
            finalHeightLocalPos.y = initialLocalY;
            m_viewModel.SetPositionDirectly(finalHeightLocalPos);

            // 4. 상승 완료 및 수동 조작 대기를 위해 조작권 환원 및 IsHolding 활성화
            Debug.Log("[ClawView] 4단계 복귀 완료. 캡슐 홀딩 상태(IsHolding=True)로 전환 및 수평 수동 조작 권한 환원.");
            m_viewModel.SetHolding(true);
            m_viewModel.ChangeState(ClawState.IDLE);
        }

        /// <summary>
        /// [기능]: 집게 벌리기(Release) 작동 후 캡슐 낙하 연출 후 대기를 수행하는 2단계 시퀀스
        /// [작성자]: 윤승종
        /// </summary>
        private async UniTaskVoid ClawReleaseSequenceAsync(CancellationToken token)
        {
            if (m_viewModel == null)
            {
                return;
            }

            // 1. RELEASING (모터 풀어서 투하)
            Debug.Log("[ClawView] 수동 놓기(Release) 시퀀스 개시 - 모터 벌리기(useMotor=False) 제어.");
            m_viewModel.ControlClawPhysics(grab: false);
            await UniTask.Delay(System.TimeSpan.FromSeconds(m_dropWaitTime), cancellationToken: token);

            // 2. 투하 완료 후 상태 환원 및 홀딩 상태 초기화
            Debug.Log("[ClawView] 수동 놓기 물리 해제 연출 종료. IDLE 상태 및 홀딩 해제(IsHolding=False) 환원.");
            m_viewModel.SetHolding(false);
            m_viewModel.ChangeState(ClawState.IDLE);
        }

        #endregion

        #region Collision & Trigger Detection

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_viewModel == null)
            {
                return;
            }

            // 하강 중 장애물(바닥/캡슐) 충돌 시 하강을 조기 중단하기 위한 물리 센싱
            if (m_viewModel.CurrentState == ClawState.DESCENDING)
            {
                if (IsClawSelfComponent(collision.gameObject))
                {
                    return;
                }

                Debug.Log($"[ClawView] 하강 중 트리거 장애물 접촉 감지: {collision.gameObject.name}");
                m_hasHitObstacle = true;
            }

            if (m_viewModel.CurrentState != ClawState.DESCENDING)
            {
                return;
            }

            CapsuleView capsule = collision.GetComponent<CapsuleView>();
            if (capsule != null)
            {
                CraneGameManager gameManager = FindAnyObjectByType<CraneGameManager>();
                if (gameManager != null)
                {
                    gameManager.EvaluateGrabbedCapsule(capsule.Value);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (m_viewModel == null || m_viewModel.CurrentState != ClawState.DESCENDING)
            {
                return;
            }

            // 집게 자체의 자식 부품들과의 충돌은 무시
            if (IsClawSelfComponent(collision.gameObject))
            {
                return;
            }

            Debug.Log($"[ClawView] 하강 중 콜리전 물리 장애물 충돌 감지: {collision.gameObject.name}");
            m_hasHitObstacle = true;
        }

        /// <summary>
        /// [기능]: 충돌한 오브젝트가 집게다리나 베이스 등 집게 자체의 물리 부품인지 판별합니다.
        /// </summary>
        private bool IsClawSelfComponent(GameObject go)
        {
            if (go == gameObject)
            {
                return true;
            }
            if (m_leftArmJoint != null && (go == m_leftArmJoint.gameObject || go.transform.IsChildOf(m_leftArmJoint.transform)))
            {
                return true;
            }
            if (m_rightArmJoint != null && (go == m_rightArmJoint.gameObject || go.transform.IsChildOf(m_rightArmJoint.transform)))
            {
                return true;
            }
            if (m_baseRigidbody != null && go == m_baseRigidbody.gameObject)
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
