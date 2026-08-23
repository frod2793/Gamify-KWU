using UnityEngine;

namespace GameArifiction.TaskRush
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class TaskRushItemView : MonoBehaviour
    {
        [SerializeField] private TaskRushItemType m_type;
        private TaskRushGameView m_gameView;
        private bool m_isConsumed;

        public TaskRushItemType Type => m_type;

        public void Initialize(TaskRushGameView gameView, TaskRushItemType type)
        {
            m_gameView = gameView;
            m_type = type;
            m_isConsumed = false;
        }

        private void Update()
        {
            if (m_gameView == null || m_gameView.IsPausedOrEnded)
            {
                return;
            }

            transform.Translate(Vector3.left * (m_gameView.WorldSpeed * Time.deltaTime), Space.World);
            if (transform.position.x < -13f)
            {
                Destroy(gameObject);
            }
        }

        public void Consume()
        {
            if (m_isConsumed || m_gameView == null)
            {
                return;
            }

            m_isConsumed = true;
            m_gameView.Collect(m_type);
            Destroy(gameObject);
        }
    }
}
