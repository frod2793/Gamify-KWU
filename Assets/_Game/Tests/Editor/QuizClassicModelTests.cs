using NUnit.Framework;
using System.Collections.Generic;
using GameArifiction.QuizClassic;
using GamifyKWU.CraneGame.Data;

namespace GameArifiction.Tests.Editor
{
    [TestFixture]
    public class QuizClassicModelTests
    {
        [Test]
        public void Constructor_WithValidList_InitializesCorrectly()
        {
            // Arrange
            var quizList = new List<QuizData>
            {
                new QuizData("Q1", "A", new List<string> { "W1", "W2", "W3" }, QuizType.Classic)
            };
            float expectedTimeLimit = 25f;

            // Act
            var model = new QuizClassicModel(quizList, expectedTimeLimit);

            // Assert
            Assert.AreEqual(quizList, model.QuizList);
            Assert.AreEqual(0, model.CurrentQuizIndex);
            Assert.AreEqual(0, model.Score);
            Assert.AreEqual(expectedTimeLimit, model.TimeLimitPerQuestion);
            Assert.AreEqual(expectedTimeLimit, model.RemainingTime);
        }

        [Test]
        public void Constructor_WithNullList_InitializesWithEmptyList()
        {
            // Act
            var model = new QuizClassicModel(null);

            // Assert
            Assert.IsNotNull(model.QuizList);
            Assert.AreEqual(0, model.QuizList.Count);
        }

        [Test]
        public void Properties_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var model = new QuizClassicModel(new List<QuizData>());

            // Act
            model.CurrentQuizIndex = 2;
            model.Score = 200;
            model.RemainingTime = 15.5f;

            // Assert
            Assert.AreEqual(2, model.CurrentQuizIndex);
            Assert.AreEqual(200, model.Score);
            Assert.AreEqual(15.5f, model.RemainingTime);
        }
    }
}
