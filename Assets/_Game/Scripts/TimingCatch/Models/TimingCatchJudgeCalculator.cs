using System;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 좌우 게이지 타이밍 게임용 판정 계산 전략 구현체.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchJudgeCalculator : ITimingJudgeCalculator
    {
        /// <summary>
        /// [기능]: 게이지 중심(0.5)을 기준으로 허용 오차 폭에 따라 판정을 계산합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 판정 경계선 분리 계산을 절대 오차 방식으로 통일.
        /// </summary>
        public TimingCatchJudgeType Evaluate(float gaugeNormalized, float perfectWindow, float goodWindow)
        {
            float distance = gaugeNormalized - 0.5f;
            if (distance < 0f)
            {
                distance = -distance;
            }

            if (distance <= perfectWindow)
            {
                return TimingCatchJudgeType.Perfect;
            }
            if (distance <= goodWindow)
            {
                return TimingCatchJudgeType.Good;
            }

            return TimingCatchJudgeType.Miss;
        }
    }
}

