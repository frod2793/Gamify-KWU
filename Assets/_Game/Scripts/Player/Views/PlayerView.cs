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
        [Tooltip("플레이어 이동 가능 영역 (Center & Size)")]
        private Bounds m_movementBounds;
        #endregion

        #region 이벤트
        /// <summary>
        /// 상호작용 가능한 대상이 플레이어 감지 범위에 들어왔을 때 발생하는 이벤트입니다.
        /// </summary>
        public event Action<IInteractable> OnInteractableTargetDetected;

        /// <summary>
        /// 상호작용 대상이 플레이어 감지 범위를 벗어났을 때 발생하는 이벤트입니다.
        /// </summary>
        public event Action OnInteractableTargetLost;
        #endregion

        #region 내부 필드
        private PlayerViewModel m_viewModel;
        private bool m_isInitialized = false;
        private IInteractable m_currentInteractable;
        #endregion

        #region 유니티 생명주기
        /// <summary>
        /// [기능]: 오브젝트 활성화 시 유니티 기본 구동을 준비합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 유니티 씬 초기화 시 덮어쓰기 오작동 해결을 위해 InitializeMVVM의 구동 위치를 Awake()에서 Start()로 안전 조정
        /// </summary>
        private void Awake()
        {
        }

        /// <summary>
        /// [기능]: 씬 초기 로딩이 완료된 시점에 플레이어의 상태 보존 복원 및 MVVM 바인딩을 실행하고 SPUM 프리팹 애니메이션 상태를 초기화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Start() 맨 첫 줄에서 InitializeMVVM()을 구동하여 유니티 물리 컴포넌트의 복구 좌표 덮어쓰기 버그 완벽 차단
        /// </summary>
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
        /// <summary>
        /// [기능]: 플레이어 세션 정보(PlayerSO)가 있는 경우 마지막 보존 위치를 물리 컴포넌트(Rigidbody2D)와 뷰 좌표에 강제 복구 및 MVVM을 초기화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Rigidbody2D 컴포넌트가 있을 경우 rb.position을 동시 싱크하도록 추가하여 물리 바디의 좌표 덮어쓰기 복구 실패 버그 해결 및 디버그용 임시 로그 정리
        /// </summary>
        private void InitializeMVVM()
        {
            Vector2 startPos = transform.position;

            // PlayerSO 데이터가 있고 마지막 저장 위치 정보가 존재한다면 좌표를 오버라이드 복구
            if (m_playerSO != null)
            {
                if (m_playerSO.HasSavedPosition)
                {
                    startPos = m_playerSO.LastPosition;
                    transform.position = startPos; // 뷰 좌표 강제 싱크

                    // [추가]: Rigidbody2D 컴포넌트가 있을 경우 물리 좌표를 명시적으로 세팅하여 스폰 위치 덮어쓰기 오작동을 해결합니다.
                    Rigidbody2D rb = GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.position = startPos;
                    }

                    m_playerSO.HasSavedPosition = false; // 1회성 복구가 완료되었으므로 플래그 해제
                    Debug.Log($"[PlayerView] SO로부터 플레이어의 최종 위치를 감지하여 성공적으로 복구 스폰했습니다: {startPos}");
                }
            }

            var model = new PlayerModel(m_moveSpeed);
            m_viewModel = new PlayerViewModel(model, startPos);

            m_viewModel.OnPositionChanged += UpdatePosition;
            m_viewModel.OnStateChanged += UpdateAnimation;
            m_viewModel.OnFlipChanged += UpdateFlip;
            m_viewModel.OnIntensityChanged += UpdateAnimationSpeed;

            if (m_useMovementBounds)
            {
                m_viewModel.SetBounds(m_movementBounds);
            }
        }

        /// <summary>
        /// [기능]: 외부 트리거(예: 포탈) 등으로부터 마지막 좌표 복구 저장을 위임받아 PlayerSO 에셋에 기록합니다.
        /// [작성자]: 윤승종
        /// </summary>
        /// <param name="targetPosition">저장할 복귀 스폰 월드 좌표</param>
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
        /// [기능]: 외부 상호작용 UI 등으로부터 요청을 위임받아 현재 감지 중인 타겟의 상호작용을 실행합니다.
        /// [작성자]: 윤승종
        /// </summary>
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
            // 1. 가상 조이스틱 입력 (모바일 환경 지원)
            Vector2 joystickInput = VirtualJoystick.GetAxis(m_joystickId);

            // 2. 키보드 입력 (PC WebGL 환경 지원 - Input System)
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

                // [추가]: PC 환경에서 F 키 혹은 스페이스바를 눌러 즉시 감지 대상과 상호작용하도록 바인딩
                if (keyboard.fKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    RequestInteraction();
                }
            }

            Vector2 keyboardInput = new Vector2(horizontal, vertical);

            // 3. 입력 병합 및 정규화 (대각선 및 중복 입력 시 최대 크기 1 제한)
            Vector2 combinedInput = joystickInput + keyboardInput;
            if (combinedInput.sqrMagnitude > 1f)
            {
                combinedInput.Normalize();
            }

            m_viewModel.ProcessInput(combinedInput, Time.deltaTime);
        }

        /// <summary>
        /// [기능]: 뷰모델로부터 전달된 플레이어의 실시간 계산 좌표를 transform과 Rigidbody2D에 반영합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Rigidbody2D가 존재할 시 물리 좌표를 동시 동기화하도록 구현
        /// </summary>
        private void UpdatePosition(Vector2 newPosition)
        {
            transform.position = newPosition;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = newPosition;
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
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(m_movementBounds.center, m_movementBounds.size);
            }
        }
        #endregion
    }
}
