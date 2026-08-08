namespace GameArifiction.TimingCatch
{
    public sealed class TimingCatchJudgeCalculator : ITimingJudgeCalculator
    {
        public TimingCatchJudgeType Evaluate(float gaugeNormalized, float greatZoneWidth)
        {
            return Evaluate(gaugeNormalized, greatZoneWidth * .5f, 0f);
        }

        public TimingCatchJudgeType Evaluate(float gaugeNormalized, float greatZoneHalfWidth, float unusedWindow)
        {
            float distance = gaugeNormalized - .5f;
            if (distance < 0f) distance = -distance;
            return distance <= greatZoneHalfWidth ? TimingCatchJudgeType.Great : TimingCatchJudgeType.Miss;
        }
    }
}
