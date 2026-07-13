using System;
using System.Reflection;
using NUnit.Framework;
using GameArifiction.TimingCatch;

namespace GameArifiction.Tests.Editor
{
    /// <summary>
    /// [기능]: 타이밍 캐치 판정과 캐릭터 반응을 연결하는 Presenter 계약을 검증합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [TestFixture]
    public sealed class TimingCatchCharacterPresenterTests
    {
        #region 내부 필드 (Private Fields)
        private FakeJudgeEventSource m_eventSource;
        private FakeCharacterView m_characterView;
        private TimingCatchCharacterPresenter m_presenter;
        #endregion

        #region 테스트 생명주기 (Test Lifecycle)
        /// <summary>
        /// [기능]: 각 테스트에 사용할 판정 이벤트 소스, View, Presenter를 구성합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Presenter 판정 라우팅 테스트 초기화 추가.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            m_eventSource = new FakeJudgeEventSource();
            m_characterView = new FakeCharacterView();
            m_presenter = new TimingCatchCharacterPresenter
            {
                JudgeEventSource = m_eventSource,
                CharacterView = m_characterView
            };
            m_presenter.Start();
        }

        /// <summary>
        /// [기능]: 테스트 종료 시 Presenter 이벤트 구독을 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Presenter 판정 라우팅 테스트 정리 추가.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            m_presenter.Dispose();
        }
        #endregion

        #region 테스트 (Tests)
        /// <summary>
        /// [기능]: 런타임 어셈블리에 판정 이벤트, 캐릭터 View, Presenter 계약 타입이 제공되는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이밍 캐릭터 반응 계약 타입 검증 추가.
        /// </summary>
        [Test]
        public void RequiredPresenterTypes_AreAvailable()
        {
            Assembly runtimeAssembly = typeof(TimingCatchGameViewModel).Assembly;

            Assert.IsNotNull(
                runtimeAssembly.GetType("GameArifiction.TimingCatch.ITimingCatchJudgeEventSource"),
                "판정 이벤트 소스 인터페이스가 필요합니다."
            );
            Assert.IsNotNull(
                runtimeAssembly.GetType("GameArifiction.TimingCatch.ITimingCatchCharacterView"),
                "캐릭터 반응 View 인터페이스가 필요합니다."
            );
            Assert.IsNotNull(
                runtimeAssembly.GetType("GameArifiction.TimingCatch.TimingCatchCharacterPresenter"),
                "판정과 캐릭터 반응을 연결할 Presenter가 필요합니다."
            );
        }

        /// <summary>
        /// [기능]: Perfect와 Good 판정이 성공 반응을 한 번 요청하는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 성공 판정 라우팅 검증 추가.
        /// </summary>
        [TestCase(TimingCatchJudgeType.Perfect)]
        [TestCase(TimingCatchJudgeType.Good)]
        public void JudgeEvaluated_WithSuccessJudge_PlaysSuccessReaction(TimingCatchJudgeType judge)
        {
            m_eventSource.Raise(judge);

            Assert.AreEqual(1, m_characterView.SuccessCount);
            Assert.AreEqual(0, m_characterView.FailureCount);
        }

        /// <summary>
        /// [기능]: Miss 판정이 실패 반응을 한 번 요청하는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 실패 판정 라우팅 검증 추가.
        /// </summary>
        [Test]
        public void JudgeEvaluated_WithMiss_PlaysFailureReaction()
        {
            m_eventSource.Raise(TimingCatchJudgeType.Miss);

            Assert.AreEqual(0, m_characterView.SuccessCount);
            Assert.AreEqual(1, m_characterView.FailureCount);
        }

        /// <summary>
        /// [기능]: Dispose 이후 판정 이벤트가 캐릭터 반응을 요청하지 않는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 이벤트 구독 해제 검증 추가.
        /// </summary>
        [Test]
        public void Dispose_AfterStart_StopsReactionRequests()
        {
            m_presenter.Dispose();

            m_eventSource.Raise(TimingCatchJudgeType.Perfect);

            Assert.AreEqual(0, m_characterView.SuccessCount);
            Assert.AreEqual(0, m_characterView.FailureCount);
        }
        #endregion

        #region 테스트 대역 (Test Doubles)
        /// <summary>
        /// [기능]: 타이밍 판정 이벤트를 수동 발행하는 테스트 대역입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private sealed class FakeJudgeEventSource : ITimingCatchJudgeEventSource
        {
            public event Action<TimingCatchJudgeType> OnJudgeEvaluated;

            /// <summary>
            /// [기능]: 테스트용 타이밍 판정 이벤트를 발행합니다.
            /// [작성자]: 윤승종
            /// [수정 날짜]: 2026-07-13
            /// [마지막 수정 작성자]: 윤승종
            /// [수정 내용]: 판정 이벤트 테스트 대역 추가.
            /// </summary>
            public void Raise(TimingCatchJudgeType judge)
            {
                OnJudgeEvaluated?.Invoke(judge);
            }
        }

        /// <summary>
        /// [기능]: 캐릭터 반응 요청 횟수를 기록하는 테스트 대역입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private sealed class FakeCharacterView : ITimingCatchCharacterView
        {
            public int SuccessCount { get; private set; }
            public int FailureCount { get; private set; }

            /// <summary>
            /// [기능]: 성공 반응 요청 횟수를 기록합니다.
            /// [작성자]: 윤승종
            /// [수정 날짜]: 2026-07-13
            /// [마지막 수정 작성자]: 윤승종
            /// [수정 내용]: 성공 반응 테스트 대역 추가.
            /// </summary>
            public void PlaySuccessReaction()
            {
                SuccessCount++;
            }

            /// <summary>
            /// [기능]: 실패 반응 요청 횟수를 기록합니다.
            /// [작성자]: 윤승종
            /// [수정 날짜]: 2026-07-13
            /// [마지막 수정 작성자]: 윤승종
            /// [수정 내용]: 실패 반응 테스트 대역 추가.
            /// </summary>
            public void PlayFailureReaction()
            {
                FailureCount++;
            }

            /// <summary>
            /// [기능]: 테스트 대역의 IDLE 복귀 명령을 수신합니다.
            /// [작성자]: 윤승종
            /// [수정 날짜]: 2026-07-13
            /// [마지막 수정 작성자]: 윤승종
            /// [수정 내용]: 캐릭터 View 계약 완성용 IDLE 메서드 추가.
            /// </summary>
            public void ResetToIdle()
            {
            }
        }
        #endregion
    }
}
