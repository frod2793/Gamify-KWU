using UnityEngine;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using GameArifiction.Player;
using GamifyKWU.CraneGame.Data;

namespace GameArifiction.QuizClassic
{
    /// <summary>
    /// [기능]: 클래식 4지선다 퀴즈 씬의 초기화 및 흐름을 제어하는 순수 C# EntryPoint 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class QuizClassicFlowController
    {
        #region 내부 의존성 필드 (Private Fields)
        private readonly QuizClassicView m_classicView;
        private readonly PlayerSO m_playerSO;
        private readonly QuizDatabaseSO m_quizDatabase;

        // 게임 설정 기본 상수
        private const float TIME_LIMIT_PER_QUESTION = 30f;
        #endregion

        #region 생성자 의존성 주입 (Constructor DI)
        /// <summary>
        /// [기능]: VContainer를 통해 의존성을 주입받아 초기화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public QuizClassicFlowController(
            QuizClassicView classicView,
            PlayerSO playerSO,
            QuizDatabaseSO quizDatabase)
        {
            m_classicView = classicView;
            m_playerSO = playerSO;
            m_quizDatabase = quizDatabase;
        }
        #endregion

        #region 진입점 인터페이스 구현 (IStartable)
        /// <summary>
        /// [기능]: VContainer 빌드가 완료된 직후 실행되어, 모델 및 뷰모델을 조립하고 퀴즈 게임을 개시합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void StartClassicQuiz()
        {
            Debug.Log("[QuizClassicFlowController] 클래식 4지선다 퀴즈 흐름 제어를 개시합니다.");
            InitializeClassicQuiz();
        }
        #endregion

        #region 내부 초기화 로직 (Private Methods)
        /// <summary>
        /// [기능]: 퀴즈 데이터를 수집하고 MVVM 단방향 의존성 주입 조립을 완료한 뒤 게임을 개시합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void InitializeClassicQuiz()
        {
            // 1. 퀴즈 에셋 데이터 로드 및 '클래식' 퀴즈 필터링
            List<QuizData> quizzes = new List<QuizData>();

            if (m_quizDatabase != null && m_quizDatabase.QuizList != null && m_quizDatabase.QuizList.Count > 0)
            {
                for (int i = 0; i < m_quizDatabase.QuizList.Count; i++)
                {
                    if (m_quizDatabase.QuizList[i] != null && m_quizDatabase.QuizList[i].QuizType == QuizType.Classic)
                    {
                        quizzes.Add(m_quizDatabase.QuizList[i]);
                    }
                }
            }

            // 폴백 무결 보호용 더미 데이터 폴백
            if (quizzes.Count == 0)
            {
                Debug.LogWarning("[QuizClassicFlowController] QuizDatabaseSO 에셋이 누락되었거나 데이터가 없어 더미 데이터를 사용합니다.");
                quizzes.Add(new QuizData(
                    "사용자 경험을 뜻하며, 사용자가 제품이나 서비스를 이용하면서 느끼는 감정을 뜻하는 용어는?",
                    "UX",
                    new List<string> { "UI", "BX", "CX" },
                    QuizType.Classic
                ));
                quizzes.Add(new QuizData(
                    "유니티 엔진에서 비동기 연산을 코루틴보다 뛰어난 효율로 다루게 해주는 외부 라이브러리는?",
                    "UniTask",
                    new List<string> { "DOTween", "UniRx", "Zenject" },
                    QuizType.Classic
                ));
            }

            // 2. Model 생성 (POCO)
            QuizClassicModel model = new QuizClassicModel(quizzes, TIME_LIMIT_PER_QUESTION);

            // 3. ViewModel 생성 (POCO)
            QuizClassicViewModel viewModel = new QuizClassicViewModel(model, m_playerSO);

            // 4. View 주입 및 초기화
            if (m_classicView != null)
            {
                m_classicView.Initialize(viewModel);
            }
            else
            {
                Debug.LogError("[QuizClassicFlowController] QuizClassicView 가 하이어라키 내에 탐색되지 않았습니다. 바인딩 조립이 불가능합니다.");
                return;
            }

            // 5. 클래식 퀴즈 정식 가동 개시
            viewModel.StartGame();
            Debug.Log("[QuizClassicFlowController] 4지선다 클래식 퀴즈 미니게임 시스템 초기화 완료 및 개시 성공.");
        }
        #endregion
    }
}
