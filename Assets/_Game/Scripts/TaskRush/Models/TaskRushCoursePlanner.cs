using UnityEngine;

namespace GameArifiction.TaskRush
{
    public readonly struct TaskRushCourseColumn
    {
        public TaskRushCourseColumn(TaskRushItemType collectibleType, float collectibleY, bool hasObstacle)
        {
            CollectibleType = collectibleType;
            CollectibleY = collectibleY;
            HasObstacle = hasObstacle;
        }

        public TaskRushItemType CollectibleType { get; }
        public float CollectibleY { get; }
        public bool HasObstacle { get; }
    }

    public static class TaskRushCoursePlanner
    {
        private const float GroundY = -2.45f;
        private const float ArcHeight = 2.1f;
        private const int ArcRadius = 3;

        public static TaskRushCourseColumn GetColumn(TaskRushPhase phase, int columnIndex)
        {
            int period = phase == TaskRushPhase.Phase1 ? 10 : phase == TaskRushPhase.Phase2 ? 8 : 6;
            int obstaclePosition = period - 3;
            int offset = (columnIndex % period) - obstaclePosition;
            if (offset < -period / 2)
            {
                offset += period;
            }
            else if (offset > period / 2)
            {
                offset -= period;
            }

            float y = GroundY;
            if (Mathf.Abs(offset) <= ArcRadius)
            {
                float normalized = offset / (float)ArcRadius;
                y += ArcHeight * (1f - normalized * normalized);
            }

            bool hasObstacle = offset == 0;
            TaskRushItemType type = phase == TaskRushPhase.Phase3 && columnIndex > 0 && columnIndex % 24 == 12
                ? TaskRushItemType.TimeBonus
                : TaskRushItemType.Score;
            return new TaskRushCourseColumn(type, y, hasObstacle);
        }
    }
}
