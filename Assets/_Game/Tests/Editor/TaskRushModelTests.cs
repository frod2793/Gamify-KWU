using GameArifiction.Player;
using GameArifiction.TaskRush;
using NUnit.Framework;

namespace GamifyKWU.Tests.Editor
{
    public sealed class TaskRushModelTests
    {
        [TestCase(35.999f, TaskRushPhase.Phase1, 1f)]
        [TestCase(36f, TaskRushPhase.Phase2, 1.2f)]
        [TestCase(70.999f, TaskRushPhase.Phase2, 1.2f)]
        [TestCase(71f, TaskRushPhase.Phase3, 1.45f)]
        public void Tick_경과시간에따라단계와속도를변경한다(float elapsed, TaskRushPhase phase, float speed)
        {
            var model = new TaskRushModel(100f);

            model.Tick(elapsed);

            Assert.AreEqual(phase, model.CurrentPhase);
            Assert.AreEqual(speed, model.SpeedMultiplier, 0.001f);
        }

        [Test]
        public void AddTime_시간은늘리지만단계는되돌리지않는다()
        {
            var model = new TaskRushModel(100f);
            model.Tick(71f);

            model.AddTime(10f);

            Assert.AreEqual(39f, model.RemainingTime, 0.001f);
            Assert.AreEqual(TaskRushPhase.Phase3, model.CurrentPhase);
        }

        [Test]
        public void ApplyPenalty_점수를0미만으로내리지않는다()
        {
            var model = new TaskRushModel(100f);

            model.ApplyPenalty(10);

            Assert.AreEqual(0, model.Score);
        }

        [TestCase(599, MinigameGrade.F)]
        [TestCase(600, MinigameGrade.D)]
        [TestCase(699, MinigameGrade.D)]
        [TestCase(700, MinigameGrade.C)]
        [TestCase(799, MinigameGrade.C)]
        [TestCase(800, MinigameGrade.B)]
        [TestCase(899, MinigameGrade.B)]
        [TestCase(900, MinigameGrade.A)]
        public void CalculateGrade_기획서경계값을적용한다(int score, MinigameGrade expected)
        {
            var model = new TaskRushModel(100f);
            model.AddScore(score);

            Assert.AreEqual(expected, model.CalculateGrade());
        }

        [Test]
        public void Tick_남은시간이0이면한번만종료된다()
        {
            var model = new TaskRushModel(1f);
            int endedCount = 0;
            model.Ended += () => endedCount++;

            model.Tick(1f);
            model.Tick(1f);

            Assert.IsTrue(model.IsEnded);
            Assert.AreEqual(1, endedCount);
        }

        [Test]
        public void ViewModel_일시정지중에는시간을진행하지않는다()
        {
            var model = new TaskRushModel(100f);
            var viewModel = new TaskRushViewModel(model);

            viewModel.SetPaused(true);
            viewModel.Tick(10f);

            Assert.AreEqual(100f, viewModel.RemainingTime, 0.001f);
        }

        [Test]
        public void ViewModel_아이템종류에맞는보상을적용한다()
        {
            var model = new TaskRushModel(100f);
            var viewModel = new TaskRushViewModel(model);

            viewModel.Collect(TaskRushItemType.Score);
            viewModel.Collect(TaskRushItemType.Obstacle);
            viewModel.Tick(20f);
            viewModel.Collect(TaskRushItemType.TimeBonus);

            Assert.AreEqual(0, viewModel.Score);
            Assert.AreEqual(90f, viewModel.RemainingTime, 0.001f);
        }

        [TestCase(TaskRushPhase.Phase1, 10, 7)]
        [TestCase(TaskRushPhase.Phase2, 8, 5)]
        [TestCase(TaskRushPhase.Phase3, 6, 3)]
        public void CoursePlanner_단계별주기로장애물을배치한다(TaskRushPhase phase, int period, int firstObstacle)
        {
            for (int column = 0; column < period * 2; column++)
            {
                TaskRushCourseColumn point = TaskRushCoursePlanner.GetColumn(phase, column);
                Assert.AreEqual(column == firstObstacle || column == firstObstacle + period, point.HasObstacle);
            }
        }

        [Test]
        public void CoursePlanner_장애물위로연속포물선을만든다()
        {
            float ground = TaskRushCoursePlanner.GetColumn(TaskRushPhase.Phase1, 3).CollectibleY;
            float approach = TaskRushCoursePlanner.GetColumn(TaskRushPhase.Phase1, 5).CollectibleY;
            float beforeObstacle = TaskRushCoursePlanner.GetColumn(TaskRushPhase.Phase1, 6).CollectibleY;
            float overObstacle = TaskRushCoursePlanner.GetColumn(TaskRushPhase.Phase1, 7).CollectibleY;
            float afterObstacle = TaskRushCoursePlanner.GetColumn(TaskRushPhase.Phase1, 8).CollectibleY;

            Assert.Greater(approach, ground);
            Assert.Greater(beforeObstacle, approach);
            Assert.Greater(overObstacle, beforeObstacle);
            Assert.AreEqual(beforeObstacle, afterObstacle, 0.001f);
        }
    }
}
