using GamifyKWU.CraneGame.Data;
using GamifyKWU.CraneGame.ViewModel;
using GameArifiction.Claw;
using GameArifiction.DTO;
using UnityEngine;

namespace GamifyKWU.CraneGame.View
{
    /// <summary>
    /// [기능]: 인형 뽑기 퀴즈 게임의 비즈니스 로직(ViewModel)과 물리 집게 시스템(ClawView)의 통합 의존성을 관리하는 Entry Point 매니저
    /// [작성자]: [Senior Client Developer]
    /// </summary>
    public class CraneGameManager : MonoBehaviour
    {
        #region Fields (Inspector & References)

        [Header("Data")]
        [SerializeField] private QuizDatabaseSO m_quizDatabase;
        
        [Header("Views")]
        [SerializeField] private ClawView m_clawView;
        [SerializeField] private SpawnerView m_spawnerView;
        [SerializeField] private QuizUI_View m_quizUI;
        
        private CraneGameViewModel m_viewModel;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeGame();
        }

        #endregion

        #region Public Methods (UI & Spawner Link)

        /// <summary>
        /// [기능]: 캡슐 그랩 시 해당 캡슐의 정답 판정 후 집게 물리 아귀힘을 동적으로 분기 주입합니다.
        /// </summary>
        public void EvaluateGrabbedCapsule(string capsuleValue)
        {
            if (m_viewModel == null || m_clawView == null)
            {
                return;
            }

            QuizData currentQuiz = m_viewModel.CurrentQuiz;
            if (currentQuiz == null)
            {
                return;
            }

            bool isCorrect = capsuleValue == currentQuiz.CorrectAnswer;
            
            // 하이브리드 난이도 시스템: 정답 시 1.0배(기본 강력 아귀힘), 오답 시 0.15배(초약화 미끄러짐 토크) 주입
            float torqueMultiplier = isCorrect ? 1.0f : 0.15f;
            QuizStatsDTO stats = new QuizStatsDTO(isCorrect, torqueMultiplier);

            m_clawView.InjectQuizResult(stats);

            // 뷰모델에 판정 신호 전달 및 내부 연출 시작
            m_viewModel.JudgeResult(capsuleValue);
        }

        #endregion

        #region Private Helper Methods

        private void InitializeGame()
        {
            if (m_quizDatabase == null)
            {
                Debug.LogError("[CraneGameManager] QuizDatabaseSO가 할당되지 않았습니다!");
                return;
            }

            // 1. ViewModel 생성
            m_viewModel = new CraneGameViewModel();
            
            // 2. View 바인딩
            if (m_spawnerView != null)
            {
                m_spawnerView.BindViewModel(m_viewModel);
            }
            
            if (m_quizUI != null)
            {
                m_quizUI.BindViewModel(m_viewModel);
            }
            
            // 3. ViewModel 이벤트 구독
            m_viewModel.OnResultJudged += HandleJudgeResult;

            // 4. 게임 시작
            m_viewModel.Initialize(m_quizDatabase);
            
            Debug.Log("[CraneGameManager] 물리 연동 하이브리드 퀴즈 뽑기 게임 초기화 완료.");
        }

        private void HandleJudgeResult(bool isCorrect)
        {
            // 퀴즈 결과에 따라 UI 혹은 씬 연출이 완료된 후 다음 퀴즈 요청 진행 가능
            if (isCorrect)
            {
                Debug.Log("[CraneGameManager] 정답 처리 연출 구동.");
                // 추가 정답 연출(이펙트 등) 구현 가능
            }
            else
            {
                Debug.Log("[CraneGameManager] 오답 처리 연출 구동.");
                // 추가 오답 연출(집게 미끄러짐 가속 등) 구현 가능
            }
        }

        #endregion
    }
}
