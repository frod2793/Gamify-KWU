using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamifyKWU.CraneGame.Data
{
    /// <summary>
    /// 개별 퀴즈 데이터를 담는 순수 C# 클래스 (POCO)
    /// </summary>
    [Serializable]
    public class QuizData
    {
        #region Fields
        [SerializeField]
        [Tooltip("퀴즈 질문 내용입니다.")]
        private string m_question;
        
        [SerializeField]
        [Tooltip("퀴즈의 정답 텍스트입니다.")]
        private string m_correctAnswer;
        
        [SerializeField]
        [Tooltip("퀴즈의 오답 리스트입니다.")]
        private List<string> m_wrongAnswers = new List<string>();
        #endregion

        #region Properties
        public string Question => m_question;
        public string CorrectAnswer => m_correctAnswer;
        public List<string> WrongAnswers => m_wrongAnswers;
        #endregion

        #region Constructor
        public QuizData(string question, string correctAnswer, List<string> wrongAnswers)
        {
            m_question = question;
            m_correctAnswer = correctAnswer;
            m_wrongAnswers = wrongAnswers;
        }
        #endregion
    }
}
