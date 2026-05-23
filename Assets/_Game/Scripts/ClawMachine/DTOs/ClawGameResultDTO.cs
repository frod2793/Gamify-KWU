using System.Collections.Generic;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 게임 종료 후 획득한 결과 데이터를 외부로 전달하기 위한 데이터 객체
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawGameResultDTO
    {
        public int RemainingPlayCount { get; }
        public List<string> AcquiredDollIds { get; }

        public ClawGameResultDTO(int remainingPlayCount, List<string> acquiredDollIds)
        {
            RemainingPlayCount = remainingPlayCount;
            AcquiredDollIds = acquiredDollIds ?? new List<string>();
        }
    }
}
