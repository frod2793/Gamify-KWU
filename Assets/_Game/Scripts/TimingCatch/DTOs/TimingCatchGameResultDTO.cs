using System;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 게임 결과를 전달하는 DTO.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchGameResultDTO
    {
        public int TotalScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public int PerfectCount { get; set; }
        public int GoodCount { get; set; }
        public int MissCount { get; set; }
        public GameArifiction.Player.MinigameGrade MinigameGrade { get; set; }
        public string MinigameId { get; set; }
        public float PlayTimeSeconds { get; set; }
        public float StageTimeoutSeconds { get; set; }
    }
}
