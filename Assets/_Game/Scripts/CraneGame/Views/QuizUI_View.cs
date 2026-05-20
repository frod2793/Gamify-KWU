using System;
using System.Collections;
using System.Threading;
using GamifyKWU.CraneGame.Data;
using GamifyKWU.CraneGame.ViewModel;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace GamifyKWU.CraneGame.View
{
    /// <summary>
    /// 퀴즈 텍스트 및 결과 피드백을 담당하는 UI View
    /// </summary>
    public class QuizUI_View : MonoBehaviour
    {
        #region Fields
        [SerializeField] 
        [Tooltip("현재 퀴즈 질문이 표시될 UI 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_questionText;

        [SerializeField] 
        [Tooltip("정답/오답 결과 메시지가 표시될 UI 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_resultText;
        
        private CraneGameViewModel m_viewModel;
        private CancellationTokenSource m_cts;
        #endregion

        #region Unity Lifecycle
        private void OnDestroy()
        {
            if (m_cts != null)
            {
                m_cts.Cancel();
                m_cts.Dispose();
                m_cts = null;
            }
        }
        #endregion

        #region Public Methods
        public void BindViewModel(CraneGameViewModel viewModel)
        {
            m_viewModel = viewModel;
            
            // ViewModel 이벤트 구독
            m_viewModel.OnQuizLoaded += UpdateQuizUI;
            m_viewModel.OnResultJudged += ShowResult;
        }
        #endregion

        #region Private Methods
        private void UpdateQuizUI(QuizData quiz)
        {
            if (m_questionText != null)
            {
                m_questionText.text = quiz.Question;
            }
            
            if (m_resultText != null)
            {
                m_resultText.text = string.Empty;
            }
        }

        private void ShowResult(bool isCorrect)
        {
            if (m_resultText != null)
            {
                m_resultText.text = isCorrect ? "<color=green>정답입니다!</color>" : "<color=red>오답입니다. 다시 해보세요!</color>";
            }

            // 기존 작업 취소 후 새 토큰 생성
            if (m_cts != null)
            {
                m_cts.Cancel();
                m_cts.Dispose();
            }
            m_cts = new CancellationTokenSource();

            if (isCorrect)
            {
                // 정답일 경우 잠시 대기 후 다음 퀴즈 요청
                NextQuizDelayedAsync(m_cts.Token).Forget();
            }
            else
            {
                // 오답일 경우 일정 시간 후 텍스트만 초기화하고 다시 Idle 상태로 돌림
                ResetResultTextAsync(m_cts.Token).Forget();
            }
        }

        private async UniTaskVoid NextQuizDelayedAsync(CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2.0f), cancellationToken: token);
            m_viewModel.RequestNextQuiz();
        }

        private async UniTaskVoid ResetResultTextAsync(CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2.0f), cancellationToken: token);
            if (m_resultText != null)
            {
                m_resultText.text = string.Empty;
            }
            m_viewModel.SetState(CraneGameViewModel.EGameState.Idle);
        }
        #endregion
    }
}
