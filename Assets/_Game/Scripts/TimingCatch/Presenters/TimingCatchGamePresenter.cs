using UnityEngine;
using VContainer.Unity;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 뷰모델 타이밍 갱신을 프레임 단위로 제어하는 프레젠터.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchGamePresenter : ITickable
    {
        #region 내부 필드 (Private Fields)
        private readonly TimingCatchGameViewModel m_viewModel;
        #endregion

        #region 생성자 (Constructor)
        public TimingCatchGamePresenter(TimingCatchGameViewModel viewModel)
        {
            m_viewModel = viewModel;
        }
        #endregion

        #region ITickable
        /// <summary>
        /// [기능]: 매 프레임 갱신 시 게이지와 타이머 상태를 업데이트합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        public void Tick()
        {
            if (m_viewModel != null)
            {
                m_viewModel.UpdateTick(Time.deltaTime);
            }
        }
        #endregion
    }
}
