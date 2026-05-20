using UnityEngine;
using Terresquall;

namespace GameArifiction.Player
{
    /// <summary>
    /// [기능]: 플레이어의 입력 전달 및 시각적 표현(애니메이션, 위치)을 담당하는 뷰 클래스
    /// [작성자]: [성함/팀명]
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("References")]
        private SPUM_Prefabs m_spumPrefab;
        
        [Header("Settings")]
        private readonly int m_joystickId = 0;
        [SerializeField] private float m_moveSpeed = 5f;
        [SerializeField] private RuntimeAnimatorController m_defaultSpumController;

        #endregion

        #region 내부 필드 (Private Fields)

        private PlayerViewModel m_viewModel;
        private bool m_isInitialized = false;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            InitializeMVVM();
        }

        private void Start()
        {
            m_spumPrefab = GetComponentInChildren<SPUM_Prefabs>();

            // SPUM 초기화 확인
            if (m_spumPrefab != null)
            {
                // Animator가 할당되지 않았다면 동적으로 탐색
                if (m_spumPrefab._anim == null)
                {
                    m_spumPrefab._anim = m_spumPrefab.GetComponentInChildren<Animator>();
                }

                if (m_spumPrefab._anim != null && m_spumPrefab._anim.runtimeAnimatorController == null && m_defaultSpumController != null)
                {
                    m_spumPrefab._anim.runtimeAnimatorController = m_defaultSpumController;
                    Debug.Log("[PlayerView] SPUM Animator에 컨트롤러가 없어 기본 컨트롤러를 자동 주입했습니다.");
                }

                // Animator 및 컨트롤러 유효성 검사 후 초기화 진행
                if (m_spumPrefab._anim != null && m_spumPrefab._anim.runtimeAnimatorController != null)
                {
                    if (!m_spumPrefab.allListsHaveItemsExist())
                    {
                        m_spumPrefab.PopulateAnimationLists();
                    }

                    m_spumPrefab.OverrideControllerInit();

                    m_isInitialized = true;

                    // 초기 IDLE 상태 반영
                    UpdateAnimation(m_viewModel.CurrentState);
                }
                else
                {
                    Debug.LogError("[PlayerView] SPUM_Prefabs 초기화에 실패했습니다. 인스펙터의 'Default Spum Controller' 필드에 SPUMController 에셋을 할당했는지 확인해주세요.");
                }
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region 초기화 (Initialization)

        private void InitializeMVVM()
        {
            // Model 및 ViewModel 생성
            var model = new PlayerModel(m_moveSpeed);
            m_viewModel = new PlayerViewModel(model, transform.position);

            // 이벤트 구독
            m_viewModel.OnPositionChanged += UpdatePosition;
            m_viewModel.OnStateChanged += UpdateAnimation;
            m_viewModel.OnFlipChanged += UpdateFlip;
            m_viewModel.OnIntensityChanged += UpdateAnimationSpeed;
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

        #region 내부 메서드 (Private Methods)

        private void HandleInput()
        {
            // VirtualJoystick에서 입력 획득
            Vector2 joystickInput = VirtualJoystick.GetAxis(m_joystickId);
            
            // ViewModel에 입력 전달
            m_viewModel.ProcessInput(joystickInput, Time.deltaTime);
        }

        private void UpdatePosition(Vector2 newPosition)
        {
            transform.position = newPosition;
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
                    // SPUM의 PlayAnimation 호출 (0번 인덱스 애니메이션 사용)
                    m_spumPrefab.PlayAnimation(newState, 0);
                    
                    // 상태 전환 즉시성을 확보하기 위해 Animator.Play 명시적 호출
                    m_spumPrefab._anim.Play(newState.ToString(), 0, 0f);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayerView] Failed to play animation {newState}: {e.Message}");
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
                    // 이동 중일 때는 입력 강도에 따라 속도 조절 (최소 0.1f)
                    m_spumPrefab._anim.speed = Mathf.Max(0.1f, intensity);
                }
                else
                {
                    // 이동 중이 아닐 때는 기본 속도 유지
                    m_spumPrefab._anim.speed = 1.0f;
                }
            }
        }

        private void UpdateFlip(bool isFlipped)
        {
            if (m_spumPrefab != null)
            {
                // 캐릭터 좌우 반전 처리 (방향이 반대인 경우 로직 반전)
                Vector3 scale = m_spumPrefab.transform.localScale;
                scale.x = isFlipped ? 1f : -1f;
                m_spumPrefab.transform.localScale = scale;
            }
        }

        #endregion
    }
}
