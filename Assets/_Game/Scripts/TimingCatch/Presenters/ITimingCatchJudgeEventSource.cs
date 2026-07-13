using System;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 캐치 판정 이벤트를 구독자에게 제공하는 읽기 전용 계약입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public interface ITimingCatchJudgeEventSource
    {
        #region 이벤트 (Events)
        event Action<TimingCatchJudgeType> OnJudgeEvaluated;
        #endregion
    }
}
