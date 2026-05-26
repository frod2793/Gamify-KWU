using NUnit.Framework;
using UnityEngine;
using GameArifiction.ClawMachine;

namespace GameArifiction.Tests.Editor
{
    [TestFixture]
    public class ClawMachineModelTests
    {
        [Test]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            int initialPlayCount = 5;
            float timeLimit = 120f;

            // Act
            var model = new ClawMachineModel(initialPlayCount, timeLimit);

            // Assert
            Assert.AreEqual(initialPlayCount, model.RemainingPlayCount);
            Assert.AreEqual(timeLimit, model.TimeLimitPerPlay);
            Assert.AreEqual(timeLimit, model.RemainingTime);
            Assert.AreEqual(Vector2.zero, model.ClawPosition);
            Assert.AreEqual(0, model.ReTakeCount);
        }

        [Test]
        public void GetTimeLimitForCurrentPlay_NoReTake_ReturnsOriginalLimit()
        {
            // Arrange
            var model = new ClawMachineModel(5, 120f);

            // Act
            float limit = model.GetTimeLimitForCurrentPlay();

            // Assert
            Assert.AreEqual(120f, limit);
        }

        [Test]
        public void GetTimeLimitForCurrentPlay_WithReTakes_ReducesTimeBy20Seconds()
        {
            // Arrange
            var model = new ClawMachineModel(5, 120f);
            model.ReTakeCount = 2;

            // Act
            float limit = model.GetTimeLimitForCurrentPlay();

            // Assert
            Assert.AreEqual(80f, limit); // 120 - (2 * 20)
        }

        [Test]
        public void GetTimeLimitForCurrentPlay_ExtremeReTakes_ReturnsMinimum20Seconds()
        {
            // Arrange
            var model = new ClawMachineModel(5, 120f);
            model.ReTakeCount = 10; // 120 - 200 = -80

            // Act
            float limit = model.GetTimeLimitForCurrentPlay();

            // Assert
            Assert.AreEqual(20f, limit); // 최소 20초 방어 로직
        }
    }
}
