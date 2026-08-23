using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace GameArifiction.TaskRush
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TaskRushPlayerView : MonoBehaviour
    {
        private TaskRushConfigSO m_config;
        private TaskRushGameView m_gameView;
        private Rigidbody2D m_rigidbody;
        private SPUM_Prefabs m_spumPrefab;
        private bool m_isGrounded;
        private bool m_isAnimationInitialized;
        private float m_damageAnimationTimer;

        [Inject]
        public void Construct(TaskRushConfigSO config, TaskRushGameView gameView)
        {
            m_config = config;
            m_gameView = gameView;
            m_rigidbody ??= GetComponent<Rigidbody2D>();
            m_rigidbody.gravityScale = config.GravityScale;
        }

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            m_rigidbody.bodyType = RigidbodyType2D.Dynamic;
            m_rigidbody.freezeRotation = true;
            m_spumPrefab = GetComponentInChildren<SPUM_Prefabs>();
        }

        private void Start()
        {
            PlayAnimation(PlayerState.MOVE);
        }

        private void Update()
        {
            if (m_damageAnimationTimer > 0f)
            {
                m_damageAnimationTimer -= Time.deltaTime;
                if (m_damageAnimationTimer <= 0f)
                {
                    PlayAnimation(PlayerState.MOVE);
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
            {
                func_Jump();
            }
        }

        public void func_Jump()
        {
            if (!m_isGrounded || m_gameView == null || m_gameView.IsPausedOrEnded)
            {
                return;
            }

            m_isGrounded = false;
            m_rigidbody.linearVelocity = new Vector2(0f, m_config.JumpForce);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.name == "Ground")
            {
                m_isGrounded = true;
                if (m_damageAnimationTimer <= 0f)
                {
                    PlayAnimation(PlayerState.MOVE);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TaskRushItemView item = other.GetComponent<TaskRushItemView>();
            if (item != null)
            {
                item.Consume();
                if (item.Type == TaskRushItemType.Obstacle)
                {
                    m_damageAnimationTimer = 0.35f;
                    PlayAnimation(PlayerState.DAMAGED);
                }
            }
        }

        private void PlayAnimation(PlayerState state)
        {
            if (m_spumPrefab == null)
            {
                return;
            }

            if (m_spumPrefab._anim == null)
            {
                m_spumPrefab._anim = m_spumPrefab.GetComponentInChildren<Animator>();
            }

            if (m_spumPrefab._anim == null || m_spumPrefab._anim.runtimeAnimatorController == null)
            {
                return;
            }

            if (!m_isAnimationInitialized)
            {
                if (!m_spumPrefab.allListsHaveItemsExist())
                {
                    m_spumPrefab.PopulateAnimationLists();
                }

                m_spumPrefab.OverrideControllerInit();
                m_isAnimationInitialized = true;
            }

            m_spumPrefab.PlayAnimation(state, 0);
            m_spumPrefab._anim.Play(state.ToString(), 0, 0f);
        }
    }
}
