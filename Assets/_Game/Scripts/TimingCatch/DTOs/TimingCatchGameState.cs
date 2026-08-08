namespace GameArifiction.TimingCatch
{
    public struct TimingCatchGameState
    {
        public float Gauge;
        public float GreatZoneWidth;
        public int CurrentRound;
        public int CurrentTurn; // 1-based turn within the current round.
        public int CurrentTurnTotal; // 1-based turn across the complete 12-turn game.
        public int Round;
        public int Turn;
        public int TurnInRound;
        public int TotalTurn;
        public TimingCatchDifficulty Difficulty;
        public int Score;
        public int GreatCount;
        public int MissCount;
        public int ConsecutiveGreat;
        public int GreatBonusCount;
        public bool IsRunning;
        public bool IsIntermission;
        public bool IsFinished;
        public bool InputEnabled;
        public float IntermissionRemaining;
        public float TurnElapsed;
        public float TurnTimeout;
        public int GreatScore;
    }
}
