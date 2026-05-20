using System;
using System.Collections.Generic;
using GamifyKWU.CraneGame.Data;
using UnityEngine;

namespace GamifyKWU.CraneGame.ViewModel
{
    /// <summary>
    /// 인형 뽑기 게임의 상태와 로직을 관리하는 ViewModel
    /// </summary>
    public class CraneGameViewModel
    {
        #region Enums
        public enum EGameState
        {
            Idle,       // 시작 대기 및 퀴즈 표시
            Moving,     // 집게 이동 중
            Descending, // 집게 하강 중
            Ascending,  // 집게 상승 중
            Judging     // 정답 확인 및 연출 중
        }
        #endregion

        #region Fields
        private QuizDatabaseSO m_quizDatabase;
        private int m_currentQuizIndex = 0;
        private EGameState m_currentState = EGameState.Idle;
        
        // Events for View Binding
        public Action<EGameState> OnStateChanged;
        public Action<QuizData> OnQuizLoaded;
        public Action<bool> OnResultJudged; // true: Correct, false: Wrong
        #endregion

        #region Properties
        public EGameState CurrentState => m_currentState;
        public QuizData CurrentQuiz => m_quizDatabase.GetQuizByIndex(m_currentQuizIndex);
        #endregion

        #region Public Methods
        public void Initialize(QuizDatabaseSO database)
        {
            m_quizDatabase = database;
            m_currentQuizIndex = 0;
            LoadCurrentQuiz();
        }

        public void SetState(EGameState newState)
        {
            if (m_currentState == newState)
            {
                return;
            }

            m_currentState = newState;
            OnStateChanged?.Invoke(m_currentState);
            
            Debug.Log($"[CraneGame] 상태 변경: {newState}");
        }

        public void LoadCurrentQuiz()
        {
            var quiz = CurrentQuiz;
            if (quiz != null)
            {
                OnQuizLoaded?.Invoke(quiz);
                SetState(EGameState.Idle);
            }
            else
            {
                Debug.LogWarning("[CraneGame] 더 이상 로드할 퀴즈가 없습니다.");
            }
        }

        public void JudgeResult(string capsuleValue)
        {
            SetState(EGameState.Judging);
            
            bool isCorrect = capsuleValue == CurrentQuiz.CorrectAnswer;
            OnResultJudged?.Invoke(isCorrect);
            
            if (isCorrect)
            {
                Debug.Log("[CraneGame] 정답입니다!");
                m_currentQuizIndex++;
                // 다음 퀴즈 로드는 View에서 연출이 끝난 후 호출하도록 유도하거나 여기서 지연 처리
            }
            else
            {
                Debug.Log("[CraneGame] 오답입니다. 다시 시도하세요.");
            }
        }

        public void RequestNextQuiz()
        {
            if (m_currentQuizIndex < m_quizDatabase.GetTotalQuizCount())
            {
                LoadCurrentQuiz();
            }
            else
            {
                Debug.Log("[CraneGame] 모든 퀴즈를 완료했습니다!");
                // 게임 클리어 처리 로직 추가 가능
            }
        }
        #endregion
    }
}
