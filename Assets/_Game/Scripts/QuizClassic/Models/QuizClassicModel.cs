using System.Collections.Generic;
using GamifyKWU.CraneGame.Data;

namespace GameArifiction.QuizClassic
{
    /// <summary>
    /// [기능]: 클래식 객관식 퀴즈 게임의 점수, 시간, 문제 인덱스 등 순수 인게임 데이터 상태를 관리하는 모델 클래스 (POCO)
    /// [작성자]: 윤승종
    /// </summary>
    public class QuizClassicModel
    {
        #region 내부 필드 (Private Fields)

        private readonly List<QuizData> m_quizList;
        private int m_currentQuizIndex;
        private int m_score;
        private float m_remainingTime;
        private float m_timeLimitPerQuestion;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public List<QuizData> QuizList => m_quizList;

        public int CurrentQuizIndex
        {
            get => m_currentQuizIndex;
            set => m_currentQuizIndex = value;
        }

        public int Score
        {
            get => m_score;
            set => m_score = value;
        }

        public float RemainingTime
        {
            get => m_remainingTime;
            set => m_remainingTime = value;
        }

        public float TimeLimitPerQuestion => m_timeLimitPerQuestion;

        #endregion

        #region 초기화 (Initialization)

        public QuizClassicModel(List<QuizData> quizList, float timeLimitPerQuestion = 30f)
        {
            m_quizList = quizList ?? new List<QuizData>();
            m_currentQuizIndex = 0;
            m_score = 0;
            m_timeLimitPerQuestion = timeLimitPerQuestion;
            m_remainingTime = timeLimitPerQuestion;
        }

        #endregion
    }
}
