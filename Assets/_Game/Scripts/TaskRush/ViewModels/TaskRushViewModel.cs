using System;
using GameArifiction.Player;

namespace GameArifiction.TaskRush
{
    public enum TaskRushItemType
    {
        Score,
        Obstacle,
        TimeBonus
    }

    public sealed class TaskRushViewModel : IDisposable
    {
        private readonly TaskRushModel m_model;

        public event Action StateChanged;
        public event Action<MinigameGrade> GameEnded;

        public bool IsPaused { get; private set; }
        public int Score => m_model.Score;
        public float RemainingTime => m_model.RemainingTime;
        public TaskRushPhase CurrentPhase => m_model.CurrentPhase;
        public float SpeedMultiplier => m_model.SpeedMultiplier;
        public bool IsEnded => m_model.IsEnded;

        public TaskRushViewModel(TaskRushModel model)
        {
            m_model = model;
            m_model.Ended += HandleEnded;
        }

        public void Tick(float deltaTime)
        {
            if (IsPaused || IsEnded)
            {
                return;
            }

            m_model.Tick(deltaTime);
            StateChanged?.Invoke();
        }

        public void Collect(TaskRushItemType type)
        {
            if (IsPaused || IsEnded)
            {
                return;
            }

            switch (type)
            {
                case TaskRushItemType.Score:
                    m_model.AddScore(10);
                    break;
                case TaskRushItemType.Obstacle:
                    m_model.ApplyPenalty(10);
                    break;
                case TaskRushItemType.TimeBonus:
                    m_model.AddTime(10f);
                    break;
            }

            StateChanged?.Invoke();
        }

        public void SetPaused(bool isPaused)
        {
            if (IsEnded)
            {
                return;
            }

            IsPaused = isPaused;
            StateChanged?.Invoke();
        }

        public void Dispose()
        {
            m_model.Ended -= HandleEnded;
        }

        private void HandleEnded()
        {
            GameEnded?.Invoke(m_model.CalculateGrade());
            StateChanged?.Invoke();
        }
    }
}
