using System;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 판정 계산을 담당하는 전략 인터페이스.
    /// [작성자]: 윤승종
    /// </summary>
    public interface ITimingJudgeCalculator
    {
        /// <summary>
        /// [기능]: 현재 게이지 위치와 판정 구간을 바탕으로 판정 타입을 계산합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 판정 계산 인터페이스 정식화.
        /// </summary>
        /// <param name="gaugeNormalized">0~1 정규화된 게이지 위치.</param>
        /// <param name="perfectWindow">Perfect 판정 허용 절대 오차.</param>
        /// <param name="goodWindow">Good 판정 허용 절대 오차.</param>
        /// <returns>판정 타입.</returns>
        TimingCatchJudgeType Evaluate(float gaugeNormalized, float perfectWindow, float goodWindow);
    }
}

