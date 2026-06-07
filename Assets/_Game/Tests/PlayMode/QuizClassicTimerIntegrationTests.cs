using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameArifiction.QuizClassic;
using GamifyKWU.CraneGame.Data;
using GameArifiction.Player;

namespace GameArifiction.Tests.PlayMode
{
    public class QuizClassicTimerIntegrationTests
    {
        private QuizClassicModel m_model;
        private QuizClassicViewModel m_viewModel;

        [SetUp]
        public void Setup()
        {
            var quizList = new List<QuizData>
            {
                new QuizData("Q1", "A", new List<string> { "W1", "W2", "W3" }, QuizType.Classic)
            };
            // 2초의 짧은 제한 시간으로 타이머 시나리오 테스트
            m_model = new QuizClassicModel(quizList, 2f);
            m_viewModel = new QuizClassicViewModel(m_model, null);
        }

        [TearDown]
        public void Teardown()
        {
            m_viewModel?.Dispose();
        }

        [UnityTest]
        public IEnumerator Timer_WhenExpired_MaintainsPlayingState()
        {
            // 게임 시작 (타이머 시작됨)
            m_viewModel.StartGame();

            Assert.AreEqual(QuizStateType.Playing, m_viewModel.CurrentState);

            // 2초(제한 시간) + 약간의 딜레이(마진) 대기
            yield return new WaitForSeconds(2.5f);

            // [기획 변경 반영]: 제한 시간이 초과되어도 실패 전이 없이 순수 소요 시간만 누적하므로 상태는 여전히 Playing이어야 함
            Assert.AreEqual(QuizStateType.Playing, m_viewModel.CurrentState, "상태가 여전히 Playing으로 유지되어야 합니다.");
        }
    }
}
