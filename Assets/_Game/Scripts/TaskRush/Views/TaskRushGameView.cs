using GameArifiction.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace GameArifiction.TaskRush
{
    public sealed class TaskRushGameView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_scoreText;
        [SerializeField] private TextMeshProUGUI m_phaseText;
        [SerializeField] private Image m_timerFill;
        [SerializeField] private GameObject m_resultPanel;
        [SerializeField] private TextMeshProUGUI m_resultText;
        [SerializeField] private Transform[] m_backgrounds;
        [SerializeField] private float m_backgroundWidth = 21.87f;
        [SerializeField] private Sprite m_scoreSprite;
        [SerializeField] private Sprite m_obstacleSprite;
        [SerializeField] private Sprite m_bonusSprite;

        private TaskRushConfigSO m_config;
        private PlayerSO m_playerSO;
        private TaskRushViewModel m_viewModel;
        private float m_spawnTimer;
        private int m_courseColumn;

        public bool IsPausedOrEnded => m_viewModel == null || m_viewModel.IsPaused || m_viewModel.IsEnded;
        public float WorldSpeed => m_config.BaseWorldSpeed * m_viewModel.SpeedMultiplier;

        [Inject]
        public void Construct(TaskRushConfigSO config, PlayerSO playerSO, TaskRushViewModel viewModel)
        {
            m_config = config;
            m_playerSO = playerSO;
            m_viewModel = viewModel;
            m_viewModel.StateChanged += RefreshHud;
            m_viewModel.GameEnded += ShowResult;
        }

        private void Start()
        {
            if (m_resultPanel != null)
            {
                m_resultPanel.SetActive(false);
            }
            RefreshHud();
        }

        private void Update()
        {
            if (m_viewModel == null)
            {
                return;
            }

            m_viewModel.Tick(Time.deltaTime);
            if (IsPausedOrEnded)
            {
                return;
            }

            MoveBackgrounds();
            m_spawnTimer -= Time.deltaTime;
            if (m_spawnTimer <= 0f)
            {
                SpawnColumn();
                m_spawnTimer = m_config.SpawnInterval / m_viewModel.SpeedMultiplier;
            }
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.StateChanged -= RefreshHud;
                m_viewModel.GameEnded -= ShowResult;
            }
        }

        public void Collect(TaskRushItemType type)
        {
            m_viewModel.Collect(type);
        }

        public void func_TogglePause()
        {
            m_viewModel.SetPaused(!m_viewModel.IsPaused);
        }

        public void func_ReturnToLobby()
        {
            SceneManager.LoadScene("Lobby");
        }

        private void MoveBackgrounds()
        {
            for (int i = 0; i < m_backgrounds.Length; i++)
            {
                Transform target = m_backgrounds[i];
                target.Translate(Vector3.left * (WorldSpeed * Time.deltaTime), Space.World);
                if (target.position.x <= -m_backgroundWidth)
                {
                    target.position += Vector3.right * (m_backgroundWidth * 2f);
                }
            }
        }

        private void SpawnColumn()
        {
            TaskRushCourseColumn column = TaskRushCoursePlanner.GetColumn(m_viewModel.CurrentPhase, m_courseColumn++);
            SpawnItem(column.CollectibleType, column.CollectibleY);
            if (column.HasObstacle)
            {
                SpawnItem(TaskRushItemType.Obstacle, -2.7f);
            }
        }

        private void SpawnItem(TaskRushItemType type, float y)
        {
            Sprite sprite = type == TaskRushItemType.Score ? m_scoreSprite : type == TaskRushItemType.Obstacle ? m_obstacleSprite : m_bonusSprite;
            var item = new GameObject(type.ToString());
            item.transform.position = new Vector3(11f, y, 0f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;
            item.transform.localScale = type == TaskRushItemType.Obstacle ? Vector3.one * 0.8f : Vector3.one * 0.9f;
            BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            if (type == TaskRushItemType.Obstacle)
            {
                collider.size = Vector2.Scale(collider.size, new Vector2(0.7f, 0.8f));
            }
            item.AddComponent<TaskRushItemView>().Initialize(this, type);
        }

        private void RefreshHud()
        {
            if (m_scoreText != null)
            {
                m_scoreText.text = $"SCORE {m_viewModel.Score:000}";
            }
            if (m_phaseText != null)
            {
                m_phaseText.text = m_viewModel.IsPaused ? "PAUSE" : $"{(int)m_viewModel.CurrentPhase + 1}단계";
            }
            if (m_timerFill != null)
            {
                m_timerFill.fillAmount = Mathf.Clamp01(m_viewModel.RemainingTime / m_config.GameDuration);
            }
        }

        private void ShowResult(MinigameGrade grade)
        {
            m_playerSO.SetMinigameGrade("TaskRush", grade);
            if (m_resultText != null)
            {
                m_resultText.text = $"과제 제출 완료\n점수 {m_viewModel.Score}\n등급 {grade}";
            }
            if (m_resultPanel != null)
            {
                m_resultPanel.SetActive(true);
            }
        }
    }
}
