using NUnit.Framework;
using GameArifiction.ClawMachine;
using GamifyKWU.CraneGame.Data;
using GameArifiction.Player;

/// <summary>
/// [기능]: 인형 뽑기 미니게임 뷰모델(ClawGameViewModel)의 비즈니스 로직 및 상태 제어를 검증하는 에디터 테스트 클래스
/// [작성자]: 윤승종
/// [수정 날짜]: 2026-05-27
/// [마지막 수정 작성자]: 윤승종
/// [수정 내용]: WrongAnswer 테스트 시 불필요했던 OnReTakeRequested 수신 기대를 제거하여 테스트 정합성 수정
/// </summary>
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
            m_viewModel = new ClawGameViewModel(m_model, null);
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

        /// <summary>
        /// [기능]: 잘못된 오답을 제출했을 때 실패 상태 전이 및 OnQuizFailed 이벤트 발행만 발생하는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 오답 시 OnReTakeRequested는 발사되지 않는 도메인 설계 사양에 맞게 Assert.IsFalse로 검증 수정
        /// </summary>
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
            Assert.IsFalse(reTakeFired);
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
