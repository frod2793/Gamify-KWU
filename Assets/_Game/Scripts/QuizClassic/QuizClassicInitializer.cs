using System.Collections.Generic;
using UnityEngine;
using GamifyKWU.CraneGame.Data;
using GameArifiction.Player;

namespace GameArifiction.QuizClassic
{
    /// <summary>
    /// [기능]: 클래식 4지선다 퀴즈 씬의 런타임 진입점(Composition Root).
    ///         수동 의존성 주입(DI)을 통해 Model, ViewModel, View 간의 관계를 수립합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class QuizClassicInitializer : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [SerializeField]
        [Tooltip("클래식 퀴즈의 UI 출력 및 연쇄 연출을 제어하는 메인 View입니다.")]
        private QuizClassicView m_classicView;

        [Header("퀴즈 주입 리소스")]
        [SerializeField]
        [Tooltip("씬에 할당할 퀴즈 데이터베이스 스크립터블 오브젝트입니다.")]
        private QuizDatabaseSO m_quizDatabase;

        [SerializeField]
        [Tooltip("문제를 풀 각 퀴즈당 제한 시간(초)입니다. 기본값 30초.")]
        private float m_timeLimitPerQuestion = 30f;

        [SerializeField]
        [Tooltip("씬 개시(Start) 시점에 자동으로 클래식 퀴즈를 초기화하고 가동할지 여부입니다. 연계 플레이 시에는 반드시 false로 지정해야 합니다.")]
        private bool m_initializeOnStart = false;

        [Header("세션 데이터")]
        [SerializeField]
        [Tooltip("씬 간 플레이어 위치 상태 보존을 위한 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Start()
        {
            if (m_initializeOnStart)
            {
                InitializeClassicQuiz();
            }
        }

        #endregion

        #region 내부 및 공개 메서드 (Methods)

        /// <summary>
        /// [기능]: 퀴즈 데이터 목록을 수집하여 MVP/MVVM 단방향 의존성 결합을 진행하고 게임을 공식 스타트시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void InitializeClassicQuiz()
        {
            // 1. 퀴즈 에셋 데이터 로드 및 셔플 리스트 준비
            List<QuizData> quizzes = new List<QuizData>();

            if (m_quizDatabase != null && m_quizDatabase.QuizList != null && m_quizDatabase.QuizList.Count > 0)
            {
                // 원본 데이터를 훼손하지 않기 위해 카피본 작성 및 '클래식' 퀴즈 유형 필터링
                for (int i = 0; i < m_quizDatabase.QuizList.Count; i++)
                {
                    if (m_quizDatabase.QuizList[i] != null && m_quizDatabase.QuizList[i].QuizType == QuizType.Classic)
                    {
                        quizzes.Add(m_quizDatabase.QuizList[i]);
                    }
                }
            }

            // 만약 퀴즈 데이터베이스 에셋이 인스펙터에 누락되었을 시 폴백 무결 보호 더미 데이터 세팅
            if (quizzes.Count == 0)
            {
                Debug.LogWarning("[QuizClassicInitializer] QuizDatabaseSO 에셋이 지정되지 않았거나 클래식 퀴즈가 비어있어, 더미 퀴즈를 자동 구성합니다.");
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
            QuizClassicModel model = new QuizClassicModel(quizzes, m_timeLimitPerQuestion);

            // 3. ViewModel 생성 (POCO)
            QuizClassicViewModel viewModel = new QuizClassicViewModel(model, m_playerSO);

            // 4. View 주입 및 초기화 (DI)
            if (m_classicView != null)
            {
                m_classicView.Initialize(viewModel);
            }
            else
            {
                Debug.LogError("[QuizClassicInitializer] QuizClassicView 참조가 인스펙터에 없습니다! 게임 가동 불능.");
                return;
            }

            // 5. 클래식 퀴즈 게임 정식 개시
            viewModel.StartGame();
            Debug.Log("[QuizClassicInitializer] 최종 4지선다 클래식 퀴즈 미니게임 시스템 초기화 완료 및 시작 성공.");
        }

        #endregion
    }
}
