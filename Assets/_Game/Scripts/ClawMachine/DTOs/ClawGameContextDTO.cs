using System.Collections.Generic;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 게임 진입 시 초기화에 필요한 데이터 전달 객체
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawGameContextDTO
    {
        public int MaxPlayCount { get; }
        public float TimeLimitPerPlay { get; }
        public List<string> InitialDollIds { get; }

        public ClawGameContextDTO(int maxPlayCount, float timeLimitPerPlay, List<string> initialDollIds)
        {
            MaxPlayCount = maxPlayCount;
            TimeLimitPerPlay = timeLimitPerPlay;
            InitialDollIds = initialDollIds ?? new List<string>();
        }
    }
}
