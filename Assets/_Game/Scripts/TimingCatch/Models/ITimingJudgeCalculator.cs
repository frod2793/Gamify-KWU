namespace GameArifiction.TimingCatch
{
    public interface ITimingJudgeCalculator
    {
        TimingCatchJudgeType Evaluate(float gaugeNormalized, float greatZoneHalfWidth, float unusedWindow);
    }
}
