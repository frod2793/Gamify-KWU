using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 미니게임 씬의 고수준 흐름을 제어하는 EntryPoint.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchGameFlowController : IStartable
    {
        #region 내부 필드 (Private Fields)
        private readonly TimingCatchGameView m_gameView;
        private readonly TimingCatchGameViewModel m_viewModel;
        #endregion

        #region 생성자 (Constructor)
        [Inject]
        public TimingCatchGameFlowController(TimingCatchGameView gameView, TimingCatchGameViewModel viewModel)
        {
            m_gameView = gameView;
            m_viewModel = viewModel;
        }
        #endregion

        #region IStartable
        /// <summary>
        /// [기능]: 씬 로드 시 뷰모델 바인딩 및 게임 시작을 진행합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        public void Start()
        {
            if (m_gameView == null || m_viewModel == null)
            {
                Debug.LogWarning("[TimingCatchGameFlowController] 뷰 또는 뷰모델이 바인딩되지 않아 게임 시작을 중단합니다.");
                return;
            }

            m_gameView.Initialize(m_viewModel);
            m_viewModel.StartGame();
            Debug.Log("[TimingCatchGameFlowController] 타이밍 미니게임 흐름 시작.");
        }
        #endregion
    }
}

