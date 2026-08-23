using UnityEngine;

namespace GameArifiction.TaskRush
{
    [CreateAssetMenu(fileName = "TaskRushConfigSO", menuName = "Gamify-KWU/TaskRushConfigSO")]
    public sealed class TaskRushConfigSO : ScriptableObject
    {
        [SerializeField] private float m_gameDuration = 100f;
        [SerializeField] private float m_baseWorldSpeed = 5f;
        [SerializeField] private float m_spawnInterval = 0.35f;
        [SerializeField, Min(0f), Tooltip("플레이어 점프의 초기 수직 속도")]
        private float m_jumpForce = 16f;
        [SerializeField, Min(0f), Tooltip("플레이어 Rigidbody2D에 적용할 중력 배율")]
        private float m_gravityScale = 4f;

        public float GameDuration => m_gameDuration;
        public float BaseWorldSpeed => m_baseWorldSpeed;
        public float SpawnInterval => m_spawnInterval;
        public float JumpForce => m_jumpForce;
        public float GravityScale => m_gravityScale;
    }
}
