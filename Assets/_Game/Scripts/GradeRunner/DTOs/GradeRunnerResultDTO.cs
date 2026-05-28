using GameArifiction.Player;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner) 종료 시 최종 플레이 결과를 씬 전이 및 결과 창에 온전히 전달하기 위한 불변 데이터 전송 객체 (DTO)
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class GradeRunnerResultDTO
    {
        #region 공개 프로퍼티 (Public Properties)

        public float FinalGradePoint { get; }
        public string GradeLetter { get; }
        public MinigameGrade MinigameGrade { get; }
        public float ElapsedTime { get; }

        #endregion

        #region 초기화 (Initialization)

        public GradeRunnerResultDTO(float finalGradePoint, string gradeLetter, MinigameGrade minigameGrade, float elapsedTime)
        {
            FinalGradePoint = finalGradePoint;
            GradeLetter = gradeLetter;
            MinigameGrade = minigameGrade;
            ElapsedTime = elapsedTime;
        }

        #endregion
    }
}
