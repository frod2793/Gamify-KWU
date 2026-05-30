using UnityEngine;
using Terresquall;
using UnityEngine.InputSystem;
using System;
using GameArifiction.Interaction;

namespace GameArifiction.Player
{
    /// <summary>
    /// [기능]: 플레이어의 입력 전달, 시각적 표현 및 상호작용 감지를 담당하는 뷰 클래스
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-28
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 인트로 연출 조작 잠금 기능(IsInputLocked) 분기 추가
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        #region UI 참조
        [Header("References")]
        private SPUM_Prefabs m_spumPrefab;
        
        [Header("Settings")]
        private readonly int m_joystickId = 0;
        [SerializeField] private float m_moveSpeed = 5f;
        [SerializeField] private RuntimeAnimatorController m_defaultSpumController;

        [Header("세션 데이터")]
        [SerializeField]
        [Tooltip("씬 간 플레이어 위치 상태 보존을 위한 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;

        [Header("이동 제한 (Bounds)")]
        [SerializeField]
        [Tooltip("체크 시 아래 설정된 Bounds 영역 내로 플레이어 이동을 제한합니다.")]
        private bool m_useMovementBounds = false;

        [SerializeField]
        [Tooltip("플레이어 이동 가능 영역을 획득할 대상 맵 오브젝트입니다.")]
        private GameObject m_mapObject;
        #endregion

        #region 이벤트
        public event Action<IInteractable> OnInteractableTargetDetected;
        public event Action OnInteractableTargetLost;
        #endregion

        #region 내부 필드
        private PlayerViewModel m_viewModel;
        private bool m_isInitialized = false;
        private IInteractable m_currentInteractable;
        private PolygonCollider2D m_cachedFloorCollider;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
        }

        private void Start()
        {
            InitializeMVVM();

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
                    Debug.Log("[PlayerView] SPUM 애니메이터에 컨트롤러가 할당되어 있지 않아 기본 컨트롤러를 자동으로 주입했습니다.");
                }

                if (m_spumPrefab._anim != null && m_spumPrefab._anim.runtimeAnimatorController != null)
                {
                    if (!m_spumPrefab.allListsHaveItemsExist())
                    {
                        m_spumPrefab.PopulateAnimationLists();
                    }

                    m_spumPrefab.OverrideControllerInit();
                    m_isInitialized = true;

                    UpdateAnimation(m_viewModel.CurrentState);
                    UpdateFlip(m_viewModel.IsFlipped);
                }
                else
                {
                    Debug.LogError("[PlayerView] SPUM 프리팹 초기화에 실패했습니다. 인스펙터의 기본 SPUM 컨트롤러 필드에 에셋이 올바르게 할당되었는지 확인하십시오.");
                }
            }
        }

        private void Update()
        {
            if (m_viewModel != null && m_viewModel.IsInputLocked)
            {
                return; // 컷씬 플레이 중일 때는 로컬 입력 핸들링 스킵
            }
            HandleInput();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            IInteractable interactable = other.GetComponent<IInteractable>();
            if (interactable != null)
            {
                m_currentInteractable = interactable;
                OnInteractableTargetDetected?.Invoke(interactable);
                Debug.Log($"[PlayerView] 상호작용 가능한 대상 진입을 감지했습니다: {interactable.InteractionPrompt}");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            IInteractable interactable = other.GetComponent<IInteractable>();
            if (interactable != null && m_currentInteractable == interactable)
            {
                m_currentInteractable = null;
                OnInteractableTargetLost?.Invoke();
                Debug.Log("[PlayerView] 상호작용 대상이 플레이어 감지 범위를 벗어났습니다.");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region 초기화
        private void InitializeMVVM()
        {
            Vector2 startPos = transform.position;

            if (m_playerSO != null)
            {
                if (m_playerSO.HasSavedPosition)
                {
                    startPos = m_playerSO.LastPosition;
                    transform.position = startPos;

                    Rigidbody2D rb = GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.position = startPos;
                    }

                    m_playerSO.HasSavedPosition = false;
                    Debug.Log($"[PlayerView] SO로부터 플레이어의 최종 위치를 감지하여 성공적으로 복구 스폰했습니다: {startPos}");
                }
            }

            var model = new PlayerModel(m_moveSpeed);
            m_viewModel = new PlayerViewModel(model, startPos);

            m_viewModel.OnPositionChanged += UpdatePosition;
            m_viewModel.OnStateChanged += UpdateAnimation;
            m_viewModel.OnFlipChanged += UpdateFlip;
            m_viewModel.OnIntensityChanged += UpdateAnimationSpeed;

            if (m_useMovementBounds && m_mapObject != null)
            {
                int floorLayer = LayerMask.NameToLayer("Floor");
                PolygonCollider2D[] colliders = m_mapObject.GetComponentsInChildren<PolygonCollider2D>(true);
                
                int colliderCount = colliders.Length;
                for (int i = 0; i < colliderCount; i++)
                {
                    if (colliders[i] != null && colliders[i].gameObject.layer == floorLayer)
                    {
                        m_cachedFloorCollider = colliders[i];
                        break;
                    }
                }

                if (m_cachedFloorCollider != null)
                {
                    // 뷰모델의 기본 사각형 Bounds 제한은 해제하고 뷰포트 폴리곤에서 직접 정밀 처리
                    m_viewModel.ClearBounds();
                    Debug.Log($"[PlayerView] 맵의 Floor 레이어 PolygonCollider2D 감지 완료. 다각형 이동 제한 영역 바인딩 성공.");
                }
                else
                {
                    Debug.LogWarning("[PlayerView] m_mapObject에서 'Floor' 레이어를 가진 PolygonCollider2D를 찾을 수 없어 이동 제한 영역을 적용하지 못했습니다.");
                }
            }
        }

        public void SavePosition(Vector2 targetPosition)
        {
            if (m_playerSO != null)
            {
                m_playerSO.LastPosition = targetPosition;
                Debug.Log($"[PlayerView] 플레이어가 외부 요청에 의해 복귀 스폰 위치를 SO에 저장했습니다: {targetPosition}");
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnPositionChanged -= UpdatePosition;
                m_viewModel.OnStateChanged -= UpdateAnimation;
                m_viewModel.OnFlipChanged -= UpdateFlip;
                m_viewModel.OnIntensityChanged -= UpdateAnimationSpeed;
            }
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [기능]: 외부 및 LifetimeScope에서 플레이어 세션 데이터 자산에 안전하게 접근할 수 있도록 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public PlayerSO PlayerSO => m_playerSO;

        /// <summary>
        /// [기능]: 외부 및 컨트롤러에서 리플렉션 없이 안전하게 뷰모델의 상태와 잠금 명령을 다룰 수 있도록 뷰모델 인스턴스를 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        /// <returns>플레이어의 뷰모델 인스턴스</returns>
        public PlayerViewModel GetViewModel()
        {
            return m_viewModel;
        }

        public void RequestInteraction()
        {
            if (m_currentInteractable != null)
            {
                m_currentInteractable.Interact(gameObject);
            }
        }
        #endregion

        #region 내부 메서드
        private void HandleInput()
        {
            Vector2 joystickInput = Vector2.zero;
            
            // 씬에 활성화된 VirtualJoystick 인스턴스가 존재할 경우에만 안전하게 조이스틱 입력 수신 (경고 및 컴파일 에러 방지)
            if (VirtualJoystick.CountActiveInstances() > 0)
            {
                joystickInput = VirtualJoystick.GetAxis(m_joystickId);
            }

            float horizontal = 0f;
            float vertical = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    vertical += 1f;
                }
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    vertical -= 1f;
                }
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    horizontal -= 1f;
                }
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    horizontal += 1f;
                }

                if (keyboard.fKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    RequestInteraction();
                }
            }

            Vector2 keyboardInput = new Vector2(horizontal, vertical);
            Vector3 combinedInput = joystickInput + keyboardInput;
            if (combinedInput.sqrMagnitude > 1f)
            {
                combinedInput.Normalize();
            }

            m_viewModel.ProcessInput(combinedInput, Time.deltaTime);
        }

        private void UpdatePosition(Vector2 newPosition)
        {
            Vector2 targetPos = newPosition;

            // 폴리곤 영역 클램핑 정밀 계산
            if (m_useMovementBounds && m_cachedFloorCollider != null)
            {
                // 해당 위치가 폴리곤 영역 밖인 경우
                if (!m_cachedFloorCollider.OverlapPoint(newPosition))
                {
                    // 폴리곤 경계선에서 가장 가까운 포인트로 흡착 클램프
                    targetPos = m_cachedFloorCollider.ClosestPoint(newPosition);

                    // POCO 동기화를 위해 뷰모델 내부 좌표도 클램핑된 피드백 위치로 동적 리셋
                    m_viewModel.ForceSetPosition(targetPos);
                }
            }

            transform.position = targetPos;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = targetPos;
            }
        }

        private void UpdateAnimation(PlayerState newState)
        {
            if (!m_isInitialized)
            {
                return;
            }

            if (m_spumPrefab != null && m_spumPrefab._anim != null)
            {
                try
                {
                    m_spumPrefab.PlayAnimation(newState, 0);
                    m_spumPrefab._anim.Play(newState.ToString(), 0, 0f);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayerView] 캐릭터 애니메이션 재생 중 오류가 발생했습니다 ({newState}): {e.Message}");
                }
            }
        }

        private void UpdateAnimationSpeed(float intensity)
        {
            if (!m_isInitialized)
            {
                return;
            }

            if (m_spumPrefab != null && m_spumPrefab._anim != null)
            {
                if (m_viewModel.CurrentState == PlayerState.MOVE)
                {
                    m_spumPrefab._anim.speed = Mathf.Max(0.1f, intensity);
                }
                else
                {
                    m_spumPrefab._anim.speed = 1.0f;
                }
            }
        }

        private void UpdateFlip(bool isFlipped)
        {
            if (m_spumPrefab != null)
            {
                Vector3 scale = m_spumPrefab.transform.localScale;
                scale.x = isFlipped ? 1f : -1f;
                m_spumPrefab.transform.localScale = scale;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (m_useMovementBounds)
            {
                PolygonCollider2D poly = m_cachedFloorCollider;
                if (poly == null && m_mapObject != null)
                {
                    // 런타임 실행 이전 에디터 상태에서도 실시간 기즈모 확인을 위한 폴백 검색 작동
                    int floorLayer = LayerMask.NameToLayer("Floor");
                    PolygonCollider2D[] colliders = m_mapObject.GetComponentsInChildren<PolygonCollider2D>(true);
                    
                    int colliderCount = colliders.Length;
                    for (int i = 0; i < colliderCount; i++)
                    {
                        if (colliders[i] != null && colliders[i].gameObject.layer == floorLayer)
                        {
                            poly = colliders[i];
                            break;
                        }
                    }
                }

                if (poly != null)
                {
                    Gizmos.color = Color.green;

                    // 폴리곤의 모든 외각 경로(Path)를 루프로 순회하며 씬에 실시간 아웃라인 선화 렌더링
                    int pathCount = poly.pathCount;
                    for (int p = 0; p < pathCount; p++)
                    {
                        Vector2[] points = poly.GetPath(p);
                        int pointCount = points.Length;
                        if (pointCount < 2)
                        {
                            continue;
                        }

                        for (int i = 0; i < pointCount; i++)
                        {
                            Vector3 p1 = poly.transform.TransformPoint(points[i]);
                            Vector3 p2 = poly.transform.TransformPoint(points[(i + 1) % pointCount]);
                            Gizmos.DrawLine(p1, p2);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
