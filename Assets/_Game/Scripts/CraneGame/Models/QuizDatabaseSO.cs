using System.Collections.Generic;
using UnityEngine;

namespace GamifyKWU.CraneGame.Data
{
    /// <summary>
    /// 게임 내 모든 퀴즈 목록을 관리하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "QuizDatabase", menuName = "GamifyKWU/CraneGame/QuizDatabase")]
    public class QuizDatabaseSO : ScriptableObject
    {
        #region Fields
        [SerializeField]
        [Tooltip("게임에서 사용할 퀴즈 데이터들의 리스트입니다.")]
        private List<QuizData> m_quizList = new List<QuizData>();
        #endregion

        #region Properties
        public List<QuizData> QuizList => m_quizList;
        #endregion

        #region Public Methods
        public QuizData GetQuizByIndex(int index)
        {
            if (index >= 0 && index < m_quizList.Count)
            {
                return m_quizList[index];
            }
            
            return null;
        }

        public int GetTotalQuizCount()
        {
            return m_quizList.Count;
        }
        #endregion
    }
}
