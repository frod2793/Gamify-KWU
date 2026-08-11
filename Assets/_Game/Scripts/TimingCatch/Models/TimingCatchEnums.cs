namespace GameArifiction.TimingCatch
{
    public enum TimingCatchDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum TimingCatchJudgeType
    {
        None,
        Great,
        Miss,
    }

    public enum TimingCatchPhase
    {
        None,
        Intro,
        RoundStart,
        Playing,
        JudgeResult,
        TurnInterval,
        RoundInterval,
        Outro,
        Completed,
    }
}
