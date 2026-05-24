namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 개별 인형의 고유 데이터를 관리하는 순수 Model
    /// [작성자]: 윤승종
    /// </summary>
    public class DollModel
    {
        #region 내부 필드 (Private Fields)
        private readonly string m_dollId;
        private readonly string m_dollName;
        private readonly float m_weight;
        private readonly bool m_isDisagree;
        private readonly string m_answerText;
        private readonly bool m_isCorrect;
        #endregion

        #region 공개 프로퍼티 (Properties)
        public string DollId => m_dollId;
        public string DollName => m_dollName;
        public float Weight => m_weight;
        public bool IsDisagree => m_isDisagree;
        public string AnswerText => m_answerText;
        public bool IsCorrect => m_isCorrect;
        #endregion

        #region 초기화 (Initialization)
        public DollModel(string dollId, string dollName, float weight, bool isDisagree = false, string answerText = "", bool isCorrect = false)
        {
            m_dollId = dollId;
            m_dollName = dollName;
            m_weight = weight;
            m_isDisagree = isDisagree;
            m_answerText = answerText;
            m_isCorrect = isCorrect;
        }
        #endregion
    }
}
