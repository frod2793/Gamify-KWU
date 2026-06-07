using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)의 플레이어 이동 제어, 화면 이탈 방지 경계 제한 및 낙하물 충돌 처리를 전담하는 View 컴포넌트.
///         플레이어의 가로 이동 한계를 화면 전체 대신 지정된 땅(Ground) 오브젝트의 좌우 실제 너비 영역으로 제한합니다.
/// [작성자]: 윤승종
/// [수정 날짜]: 2026-06-06
/// [마지막 수정 작성자]: 윤승종
/// [수정 내용]: SPUM_Prefabs 연동을 통해 조작 방향에 따른 이동 애니메이션(IDLE/MOVE) 및 플립 연출 보강
/// </summary>
namespace GameArifiction.GradeRunner
{
    [RequireComponent(typeof(Collider2D))]
    public class GradeRunnerPlayerView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("이동 제한 바닥")]
        [SerializeField]
        [Tooltip("플레이어의 가로 이동 한계 좌우 영역을 제한할 땅(Ground) 오브젝트의 Collider2D입니다.")]
        private Collider2D m_groundCollider;

        [Header("SPUM 애니메이션 설정")]
        [SerializeField]
        [Tooltip("SPUM 프리팹 초기화 실패 시의 런타임 폴백용 기본 컨트롤러 에셋입니다.")]
        private RuntimeAnimatorController m_defaultSpumController;
        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;
        private Rigidbody2D m_rigidbody;
        private float m_minX;
        private float m_maxX;
        private bool m_isInitialized = false;
        private bool m_isSpumInitialized = false;

        private SPUM_Prefabs m_spumPrefab;
        private PlayerState m_currentAnimState = PlayerState.IDLE;
        private float m_currentInputX = 0f;
        private float m_damageAnimTimer = 0f;
        #endregion

        #region 의존성 주입 (Dependency Injection)

        /// <summary>
        /// [기능]: VContainer를 통해 뷰모델 의존성을 주입받습니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public void Construct(GradeRunnerViewModel viewModel)
        {
            m_viewModel = viewModel;
            Debug.Log("[GradeRunnerPlayerView] VContainer를 통해 뷰모델 의존성 주입이 완료되었습니다.");
        }

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Start()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            if (m_rigidbody != null)
            {
                // 충돌 감지(Trigger) 및 스크립트 기반 MovePosition 이동을 위해 Kinematic 타입 강제 할당
                m_rigidbody.bodyType = RigidbodyType2D.Kinematic;
                m_rigidbody.useFullKinematicContacts = true;
            }
            
            CalculateMovementBounds();
            InitializeSPUM();
            m_isInitialized = true;
            Debug.Log($"[GradeRunnerPlayerView] 플레이어 뷰 초기화 완료. 최종 이동 제한 경계(땅 기준): [{m_minX:F2} ~ {m_maxX:F2}]");
        }

        private void Update()
        {
            if (!m_isInitialized || m_viewModel == null)
            {
                return;
            }

            if (m_damageAnimTimer > 0f)
            {
                m_damageAnimTimer -= Time.deltaTime;
            }

            if (!m_viewModel.IsPlayable)
            {
                m_currentInputX = 0f;
                return;
            }

            HandleInputAndAnimation();
        }

        private void FixedUpdate()
        {
            if (!m_isInitialized || m_viewModel == null || !m_viewModel.IsPlayable)
            {
                return;
            }

            ApplyMovement();
        }

        #endregion



        #region 내부 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 지정된 바닥(Ground) 콜라이더의 좌우 경계 좌표를 정밀 파악하고, 플레이어 스프라이트의 가로 반절 크기를 오프셋 삼아 최종 이동 한계를 계산합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void CalculateMovementBounds()
        {
            if (m_groundCollider != null)
            {
                Bounds groundBounds = m_groundCollider.bounds;

                // 플레이어의 반절 가로 크기를 고려하여 한계선 안착 오프셋 설정
                float padding = 0.5f; 
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    padding = sr.bounds.extents.x;
                }

                m_minX = groundBounds.min.x + padding;
                m_maxX = groundBounds.max.x - padding;

                // 극단적으로 좁은 땅 영역 충돌 처리 예방 가드
                if (m_minX >= m_maxX)
                {
                    m_minX = groundBounds.min.x;
                    m_maxX = groundBounds.max.x;
                }
            }
            else
            {
                Debug.LogWarning("[GradeRunnerPlayerView] m_groundCollider가 지정되지 않아 화면 끝 전체 영역을 기준으로 가로 경계를 산출합니다.");
                CalculateCameraScreenBounds();
            }
        }

        /// <summary>
        /// [기능]: 바닥 콜라이더 누락 시의 안전한 폴백용 카메라 메인 뷰포트 월드 경계 좌표 계산 기법입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void CalculateCameraScreenBounds()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                float zDistance = Mathf.Abs(cam.transform.position.z - transform.position.z);
                if (zDistance <= 0f)
                {
                    zDistance = 10f;
                }

                Vector3 leftBottom = cam.ViewportToWorldPoint(new Vector3(0f, 0f, zDistance));
                Vector3 rightBottom = cam.ViewportToWorldPoint(new Vector3(1f, 0f, zDistance));

                float padding = 0.5f;
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    padding = sr.bounds.extents.x;
                }

                m_minX = leftBottom.x + padding;
                m_maxX = rightBottom.x - padding;

                if (m_minX >= m_maxX)
                {
                    m_minX = leftBottom.x;
                    m_maxX = rightBottom.x;
                    if (m_minX >= m_maxX)
                    {
                        m_minX = -8f;
                        m_maxX = 8f;
                    }
                }
            }
            else
            {
                m_minX = -8f;
                m_maxX = 8f;
            }
        }

        /// <summary>
        /// [기능]: 자식 오브젝트로부터 SPUM_Prefabs 컴포넌트를 탐색하고 애니메이터 컨트롤러 및 리스트를 초기화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void InitializeSPUM()
        {
            m_spumPrefab = GetComponentInChildren<SPUM_Prefabs>();
            if (m_spumPrefab != null)
            {
                if (m_spumPrefab._anim == null)
                {
                    m_spumPrefab._anim = m_spumPrefab.GetComponentInChildren<Animator>();
                }

                if (m_spumPrefab._anim != null && m_spumPrefab._anim.runtimeAnimatorController == null && m_defaultSpumController != null)
                {
                    m_spumPrefab._anim.runtimeAnimatorController = m_defaultSpumController;
                    Debug.Log("[GradeRunnerPlayerView] SPUM 애니메이터에 컨트롤러가 할당되어 있지 않아 기본 컨트롤러를 자동으로 주입했습니다.");
                }

                if (m_spumPrefab._anim != null && m_spumPrefab._anim.runtimeAnimatorController != null)
                {
                    if (!m_spumPrefab.allListsHaveItemsExist())
                    {
                        m_spumPrefab.PopulateAnimationLists();
                    }
                    m_spumPrefab.OverrideControllerInit();
                    m_isSpumInitialized = true;
                    UpdateAnimation(PlayerState.IDLE);
                    UpdateFlip(false);
                }
                else
                {
                    Debug.LogError("[GradeRunnerPlayerView] SPUM 프리팹 초기화에 실패했습니다. 인스펙터의 기본 SPUM 컨트롤러 필드에 에셋이 올바르게 할당되었는지 확인하십시오.");
                }
            }
            else
            {
                Debug.LogWarning("[GradeRunnerPlayerView] 자식 오브젝트에서 SPUM_Prefabs를 찾을 수 없습니다. 일반 2D 캐릭터로 동작하거나 애니메이션이 재생되지 않습니다.");
            }
        }

        /// <summary>
        /// [기능]: 플레이어의 현재 애니메이션 상태(대기/이동)를 갱신하고 재생합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateAnimation(PlayerState newState)
        {
            if (!m_isSpumInitialized)
            {
                return;
            }

            if (m_currentAnimState == newState)
            {
                return;
            }

            m_currentAnimState = newState;

            if (m_spumPrefab != null && m_spumPrefab._anim != null)
            {
                try
                {
                    m_spumPrefab.PlayAnimation(newState, 0);
                    m_spumPrefab._anim.Play(newState.ToString(), 0, 0f);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[GradeRunnerPlayerView] 캐릭터 애니메이션 재생 중 오류가 발생했습니다 ({newState}): {e.Message}");
                }
            }
        }

        /// <summary>
        /// [기능]: 입력 방향에 맞춰 플레이어의 좌우 스프라이트 렌더링 방향을 반전시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void UpdateFlip(bool isFlipped)
        {
            if (!m_isSpumInitialized)
            {
                return;
            }

            if (m_spumPrefab != null)
            {
                // 로컬 스케일을 임의로 덮어쓰지 않고, Y축 180도 회전을 적용하여 좌우 반전 처리
                float yRotation = isFlipped ? 0f : 180f;
                m_spumPrefab.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            }
        }

        /// <summary>
        /// [기능]: 새 Input System 패키지를 통해 좌우(A/D, 화살표) 입력 및 모바일 입력을 감지하고 애니메이션을 제어합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleInputAndAnimation()
        {
            float inputX = 0f;

            // 새 Input System의 키보드 정밀 입력 감지
            Keyboard currentKeyboard = Keyboard.current;
            if (currentKeyboard != null)
            {
                if (currentKeyboard.aKey.isPressed || currentKeyboard.leftArrowKey.isPressed)
                {
                    inputX = -1f;
                }
                else if (currentKeyboard.dKey.isPressed || currentKeyboard.rightArrowKey.isPressed)
                {
                    inputX = 1f;
                }
            }

            // 키보드 입력이 없을 경우 모바일 가상 패드 입력 감지 (동시 지원)
            if (Mathf.Approximately(inputX, 0f) && m_viewModel != null)
            {
                inputX = m_viewModel.MobileInputX;
            }

            m_currentInputX = inputX;



            // 애니메이션 상태 및 플립 갱신
            if (m_damageAnimTimer > 0f)
            {
                return;
            }

            if (!Mathf.Approximately(m_currentInputX, 0f))
            {
                UpdateAnimation(PlayerState.MOVE);
                UpdateFlip(m_currentInputX < -0.1f);
            }
            else
            {
                UpdateAnimation(PlayerState.IDLE);
            }
        }

        /// <summary>
        /// [기능]: FixedUpdate에서 Rigidbody2D를 사용하여 물리적 충돌 없이 안전하게 이동을 처리합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void ApplyMovement()
        {
            if (m_rigidbody == null)
            {
                Debug.LogError("[GradeRunnerPlayerView-Debug] m_rigidbody가 누락되었습니다!");
                return;
            }



            if (Mathf.Approximately(m_currentInputX, 0f))
            {
                return;
            }

            // 가로 전체 편도 폭 계산
            float screenWidth = m_maxX - m_minX;
            if (screenWidth <= 0f)
            {
                screenWidth = 16f; // 안전 복구 폴백
            }
            
            // 뷰모델을 통해 현재 프레임당 스피드 취득
            float speed = m_viewModel.GetPlayerMoveSpeed(screenWidth);

            // 현재 위치에서 이동량 적용 후 클램핑
            Vector2 beforePos = m_rigidbody.position;
            Vector2 nextPosition = beforePos;
            nextPosition.x += m_currentInputX * speed * Time.fixedDeltaTime;
            nextPosition.x = Mathf.Clamp(nextPosition.x, m_minX, m_maxX);
            
            // 물리 엔진을 통한 위치 갱신
            m_rigidbody.MovePosition(nextPosition);
        }

        /// <summary>
        /// [기능]: 2D 트리거 충돌 시 족보/코드 태그를 분별하여 뷰모델로 데이터를 이전 처리합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_viewModel == null || !m_viewModel.IsPlayable)
            {
                return;
            }

            FallingObjectView objView = collision.GetComponent<FallingObjectView>();
            if (objView != null)
            {
                Vector2 contactPos = collision.transform.position;

                if (objView.ObjectType == FallingObjectType.Code)
                {
                    m_viewModel.ApplyCodeHit(contactPos);

                    // 스펌 피격(DAMAGED) 애니메이션 재생 및 타이머 설정
                    if (m_isSpumInitialized && m_spumPrefab != null)
                    {
                        m_damageAnimTimer = 0.35f;
                        m_currentAnimState = PlayerState.DAMAGED;
                        
                        m_spumPrefab.PlayAnimation(PlayerState.DAMAGED, 0);
                        if (m_spumPrefab._anim != null)
                        {
                            m_spumPrefab._anim.Play(PlayerState.DAMAGED.ToString(), 0, 0f);
                        }
                    }
                }
                else if (objView.ObjectType == FallingObjectType.CheatSheet)
                {
                    m_viewModel.ApplyCheatSheetPickup(contactPos);
                }

                // 충돌된 낙하 오브젝트는 풀로 회수 처리
                objView.func_Deactivate();
            }
        }

        #endregion
    }
}
