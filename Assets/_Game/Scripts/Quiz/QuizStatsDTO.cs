namespace GameArifiction.DTO
{
    /// <summary>
    /// [기능]: 퀴즈 결과에 따른 집게 아귀힘 토크 배율 및 정답 정보를 보관하는 DTO 클래스
    /// [작성자]: [Senior Client Developer]
    /// </summary>
    public class QuizStatsDTO
    {
        #region 공개 프로퍼티 (Public Properties)

        public bool IsCorrect { get; }
        public float TorqueMultiplier { get; }

        #endregion

        #region 초기화 (Initialization)

        public QuizStatsDTO(bool isCorrect, float torqueMultiplier)
        {
            IsCorrect = isCorrect;
            TorqueMultiplier = torqueMultiplier;
        }

        #endregion
    }
}
