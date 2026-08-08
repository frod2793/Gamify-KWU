namespace GameArifiction.TimingCatch
{
    public sealed class TimingCatchGameResultDTO
    {
        public int TotalScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public int GreatCount { get; set; }
        public int MissCount { get; set; }
        public int GreatBonusCount { get; set; }
        public GameArifiction.Player.MinigameGrade MinigameGrade { get; set; }
        public string MinigameId { get; set; }
        public float PlayTimeSeconds { get; set; }
    }
}
