using NUnit.Framework;
using GameArifiction.ClawMachine;
using GamifyKWU.CraneGame.Data;

namespace GameArifiction.Tests.Editor
{
    [TestFixture]
    public class ClawGameViewModelTests
    {
        private ClawMachineModel m_model;
        private ClawGameViewModel m_viewModel;

        [SetUp]
        public void Setup()
        {
            m_model = new ClawMachineModel(5, 120f);
            m_viewModel = new ClawGameViewModel(m_model);
        }

        [TearDown]
        public void Teardown()
        {
            m_viewModel.Dispose();
        }

        [Test]
        public void MoveLeftAndRight_ChangesStateProperly()
        {
            // Idle -> MoveLeft -> Stop -> Idle
            m_viewModel.StartMoveLeft();
            Assert.AreEqual(ClawStateType.MovingLeft, m_viewModel.CurrentState);
            
            m_viewModel.StopMove();
            Assert.AreEqual(ClawStateType.Idle, m_viewModel.CurrentState);

            // Idle -> MoveRight
            m_viewModel.StartMoveRight();
            Assert.AreEqual(ClawStateType.MovingRight, m_viewModel.CurrentState);
        }

        [Test]
        public void SubmitAnswer_CorrectAnswer_TransitionsToResult()
        {
            // Arrange
            m_viewModel.RegisterDollAnswer("doll_1", true);
            bool successFired = false;
            m_viewModel.OnQuizSuccess += () => successFired = true;

            // Act
            m_viewModel.func_SubmitAnswer("doll_1");

            // Assert
            Assert.IsTrue(successFired);
            Assert.AreEqual(ClawStateType.Result, m_viewModel.CurrentState);
        }

        [Test]
        public void SubmitAnswer_WrongAnswer_TransitionsToReTakeRequest()
        {
            // Arrange
            m_viewModel.RegisterDollAnswer("doll_2", false);
            bool failedFired = false;
            bool reTakeFired = false;
            m_viewModel.OnQuizFailed += () => failedFired = true;
            m_viewModel.OnReTakeRequested += () => reTakeFired = true;

            // Act
            m_viewModel.func_SubmitAnswer("doll_2");

            // Assert
            Assert.IsTrue(failedFired);
            Assert.IsTrue(reTakeFired);
            Assert.AreEqual(ClawStateType.ReTakeRequest, m_viewModel.CurrentState);
        }

        [Test]
        public void AcceptReTake_RestoresPlayCountAndTransitionsToIdle()
        {
            // Arrange
            m_viewModel.RegisterDollAnswer("doll_2", false);
            m_viewModel.func_SubmitAnswer("doll_2"); // Moves to ReTakeRequest
            m_model.RemainingPlayCount = 0; // deplete count

            // Act
            m_viewModel.AcceptReTake();

            // Assert
            Assert.AreEqual(1, m_model.ReTakeCount);
            Assert.AreEqual(5, m_model.RemainingPlayCount);
            Assert.AreEqual(ClawStateType.Idle, m_viewModel.CurrentState);
        }

        [Test]
        public void DescendClaw_ReducesPlayCount()
        {
            // Arrange
            Assert.AreEqual(5, m_model.RemainingPlayCount);

            // Act
            m_viewModel.DescendClaw();

            // Assert
            Assert.AreEqual(4, m_model.RemainingPlayCount);
            Assert.AreEqual(ClawStateType.Descending, m_viewModel.CurrentState);
        }
    }
}
