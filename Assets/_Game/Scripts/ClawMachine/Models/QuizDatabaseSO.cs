using System.Collections.Generic;
using UnityEngine;

namespace GamifyKWU.CraneGame.Data
{
    /// <summary>
    /// [기능]: 직렬화된 퀴즈 문제 목록(QuizList)을 들고 있는 ScriptableObject 컨테이너
    /// [작성자]: 윤승종
    /// </summary>
    [CreateAssetMenu(fileName = "QuizDatabase", menuName = "GamifyKWU/Quiz/QuizDatabase")]
    public class QuizDatabaseSO : ScriptableObject
    {
        #region 내부 필드 (Private Fields)
        [SerializeField]
        [Tooltip("전체 퀴즈 문제 목록 리스트입니다.")]
        private List<QuizData> m_quizList;
        #endregion

        #region 공개 프로퍼티 (Properties)
        public List<QuizData> QuizList
        {
            get
            {
                return m_quizList;
            }
        }
        #endregion
    }
}
