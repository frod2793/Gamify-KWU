using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamifyKWU.CraneGame.Data
{
    /// <summary>
    /// [기능]: 객관식 문제 하나에 대한 질문과 정답 및 오답 리스트를 보관하는 데이터 클래스
    /// [작성자]: 윤승종
    /// </summary>
    [Serializable]
    public class QuizData
    {
        #region 내부 필드 (Private Fields)
        [SerializeField]
        [Tooltip("출제될 퀴즈의 질문 내용입니다.")]
        private string m_question;

        [SerializeField]
        [Tooltip("질문에 대한 유일한 정답 텍스트입니다.")]
        private string m_correctAnswer;

        [SerializeField]
        [Tooltip("정답 외의 오답 선택지 리스트입니다.")]
        private List<string> m_wrongAnswers;
        #endregion

        #region 공개 프로퍼티 (Properties)
        public string Question
        {
            get
            {
                return m_question;
            }
        }

        public string CorrectAnswer
        {
            get
            {
                return m_correctAnswer;
            }
        }

        public List<string> WrongAnswers
        {
            get
            {
                return m_wrongAnswers;
            }
        }
        #endregion

        #region 생성자 (Constructor)
        public QuizData(string question, string correctAnswer, List<string> wrongAnswers)
        {
            m_question = question;
            m_correctAnswer = correctAnswer;
            m_wrongAnswers = wrongAnswers;
        }
        #endregion
    }
}
