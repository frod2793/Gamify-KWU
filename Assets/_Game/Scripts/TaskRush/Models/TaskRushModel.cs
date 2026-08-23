using System;
using GameArifiction.Player;

namespace GameArifiction.TaskRush
{
    public enum TaskRushPhase
    {
        Phase1,
        Phase2,
        Phase3
    }

    public sealed class TaskRushModel
    {
        public event Action Ended;

        public int Score { get; private set; }
        public float ElapsedTime { get; private set; }
        public float RemainingTime { get; private set; }
        public bool IsEnded { get; private set; }

        public TaskRushPhase CurrentPhase
        {
            get
            {
                if (ElapsedTime >= 71f)
                {
                    return TaskRushPhase.Phase3;
                }

                return ElapsedTime >= 36f ? TaskRushPhase.Phase2 : TaskRushPhase.Phase1;
            }
        }

        public float SpeedMultiplier
        {
            get
            {
                switch (CurrentPhase)
                {
                    case TaskRushPhase.Phase2:
                        return 1.2f;
                    case TaskRushPhase.Phase3:
                        return 1.45f;
                    default:
                        return 1f;
                }
            }
        }

        public TaskRushModel(float duration)
        {
            RemainingTime = Math.Max(0f, duration);
        }

        public void Tick(float deltaTime)
        {
            if (IsEnded || deltaTime <= 0f)
            {
                return;
            }

            ElapsedTime += deltaTime;
            RemainingTime = Math.Max(0f, RemainingTime - deltaTime);
            if (RemainingTime > 0f)
            {
                return;
            }

            IsEnded = true;
            Ended?.Invoke();
        }

        public void AddScore(int amount)
        {
            Score = Math.Max(0, Score + amount);
        }

        public void ApplyPenalty(int amount)
        {
            AddScore(-Math.Abs(amount));
        }

        public void AddTime(float seconds)
        {
            if (!IsEnded && seconds > 0f)
            {
                RemainingTime += seconds;
            }
        }

        public MinigameGrade CalculateGrade()
        {
            if (Score >= 900)
            {
                return MinigameGrade.A;
            }
            if (Score >= 800)
            {
                return MinigameGrade.B;
            }
            if (Score >= 700)
            {
                return MinigameGrade.C;
            }
            if (Score >= 600)
            {
                return MinigameGrade.D;
            }
            return MinigameGrade.F;
        }
    }
}
