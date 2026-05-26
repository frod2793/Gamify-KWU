using NUnit.Framework;
using System.Collections.Generic;
using GameArifiction.QuizClassic;
using GamifyKWU.CraneGame.Data;

namespace GameArifiction.Tests.Editor
{
    [TestFixture]
    public class QuizClassicViewModelTests
    {
        private QuizClassicModel m_model;
        private QuizClassicViewModel m_viewModel;

        [SetUp]
        public void Setup()
        {
            var quizList = new List<QuizData>
            {
                new QuizData("Q1", "Correct", new List<string> { "W1", "W2", "W3" }, QuizType.Classic),
                new QuizData("Q2", "Correct2", new List<string> { "W4", "W5", "W6" }, QuizType.Classic)
            };
            m_model = new QuizClassicModel(quizList, 30f);
            m_viewModel = new QuizClassicViewModel(m_model);
        }

        [TearDown]
        public void Teardown()
        {
            m_viewModel.Dispose();
        }

        [Test]
        public void Constructor_SetsStateToIdle()
        {
            Assert.AreEqual(QuizStateType.Idle, m_viewModel.CurrentState);
        }

        [Test]
        public void StartGame_LoadsFirstQuizAndChangesStateToPlaying()
        {
            // Arrange
            bool eventFired = false;
            m_viewModel.OnNextQuizLoaded += (quiz, choices) =>
            {
                eventFired = true;
                Assert.AreEqual("Q1", quiz.Question);
                Assert.AreEqual(4, choices.Count);
                Assert.IsTrue(choices.Contains("Correct"));
            };

            // Act
            m_viewModel.StartGame();

            // Assert
            Assert.IsTrue(eventFired);
            Assert.AreEqual(QuizStateType.Playing, m_viewModel.CurrentState);
            Assert.AreEqual("Q1", m_viewModel.CurrentQuiz.Question);
        }

        [Test]
        public void SelectAnswer_CorrectAnswer_IncreasesScoreAndFiresSuccessEvent()
        {
            // Arrange
            m_viewModel.StartGame();
            int correctIndex = m_viewModel.CurrentChoiceTexts.IndexOf("Correct");
            bool successFired = false;
            m_viewModel.OnQuizSuccess += () => successFired = true;

            // Act
            m_viewModel.func_SelectAnswer(correctIndex);

            // Assert
            Assert.IsTrue(successFired);
            Assert.AreEqual(100, m_model.Score);
        }

        [Test]
        public void SelectAnswer_WrongAnswer_FiresFailedEventAndChangesState()
        {
            // Arrange
            m_viewModel.StartGame();
            int wrongIndex = m_viewModel.CurrentChoiceTexts.IndexOf("W1");
            bool failedFired = false;
            m_viewModel.OnQuizFailed += () => failedFired = true;

            // Act
            m_viewModel.func_SelectAnswer(wrongIndex);

            // Assert
            Assert.IsTrue(failedFired);
            Assert.AreEqual(QuizStateType.ReTakeRequest, m_viewModel.CurrentState);
        }

        [Test]
        public void AcceptReTake_IncrementsReTakeCountAndRestartsGame()
        {
            // Arrange
            m_viewModel.StartGame();
            int wrongIndex = m_viewModel.CurrentChoiceTexts.IndexOf("W1");
            m_viewModel.func_SelectAnswer(wrongIndex); // Fail -> ReTakeRequest
            
            // Act
            m_viewModel.AcceptReTake();

            // Assert
            Assert.AreEqual(1, m_viewModel.ReTakeCount);
            Assert.AreEqual(QuizStateType.Playing, m_viewModel.CurrentState); // restarted
            Assert.AreEqual(0, m_model.Score); // score reset
        }
    }
}
