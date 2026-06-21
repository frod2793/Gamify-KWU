using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamifyKWU.CraneGame.Data
{
    /// <summary>
    /// [기능]: 객관식 문제의 타깃 플랫폼 구분(집게용 / 클래식 4지선다용)을 지원하는 형식
    /// [작성자]: 윤승종
    /// </summary>
    public enum QuizType
    {
        ClawMachine, // 집게(물리 인형뽑기) 미니게임용
        Classic      // 일반 4지선다 클래식 화면용
    }

    /// <summary>
    /// [기능]: 퀴즈 문제의 종류/주제를 분류하는 카테고리 형식
    /// [작성자]: 윤승종
    /// </summary>
    public enum QuizCategory
    {
        UIUX,                 // UI/UX 디자인
        SoftwareEngineering,  // 소프트웨어 공학
        DesignPattern,        // 디자인 패턴
        UnityEngine,          // 유니티 엔진
        GeneralCS             // 컴퓨터 사이언스 일반
    }

    /// <summary>
    /// [기능]: 객관식 문제 하나에 대한 질문과 정답 및 오답 리스트, 그리고 타깃 게임 타입을 보관하는 데이터 클래스
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

        [SerializeField]
        [Tooltip("해당 퀴즈가 사용될 미니게임의 유형입니다.")]
        private QuizType m_quizType = QuizType.ClawMachine;

        [SerializeField]
        [Tooltip("퀴즈 정답에 대한 상세 설명 내용입니다.")]
        private string m_explanation;

        [SerializeField]
        [Tooltip("해당 퀴즈가 분류될 주제 카테고리입니다.")]
        private QuizCategory m_category = QuizCategory.UIUX;

        [SerializeField]
        [Tooltip("퀴즈 문제 풀이에 제공될 힌트 내용입니다.")]
        private string m_hint = "";
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

        public QuizType QuizType
        {
            get
            {
                return m_quizType;
            }
        }

        public string Explanation
        {
            get
            {
                return m_explanation;
            }
        }

        public QuizCategory Category
        {
            get
            {
                return m_category;
            }
        }

        public string Hint
        {
            get
            {
                return m_hint;
            }
        }
        #endregion

        #region 생성자 (Constructor)
        public QuizData(string question, string correctAnswer, List<string> wrongAnswers, QuizType quizType = QuizType.ClawMachine, string explanation = "", QuizCategory category = QuizCategory.UIUX, string hint = "")
        {
            m_question = question;
            m_correctAnswer = correctAnswer;
            m_wrongAnswers = wrongAnswers;
            m_quizType = quizType;
            m_explanation = explanation;
            m_category = category;
            m_hint = hint;
        }
        #endregion
    }
}

