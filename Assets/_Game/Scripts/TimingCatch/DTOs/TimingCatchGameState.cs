using System;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 뷰 바인딩용 타이밍 게임 상태 DTO.
    /// [작성자]: 윤승종
    /// </summary>
    public struct TimingCatchGameState
    {
        public float Gauge;
        public float PerfectWindow;
        public float GoodWindow;
        public int CurrentStage;
        public int MaxStage;
        public int Score;
        public int PerfectCount;
        public int GoodCount;
        public int MissCount;
        public bool IsRunning;
        public bool IsFinished;
        public float StageElapsed;
        public float StageTimeout;
        public int PerfectScore;
        public int GoodScore;
    }
}

