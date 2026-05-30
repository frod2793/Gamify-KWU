using UnityEngine;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using GamifyKWU.CraneGame.Data;
using GameArifiction.Player;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기(ClawMachine) 씬의 초기화 및 흐름을 제어하는 순수 C# EntryPoint 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawGameFlowController : IStartable
    {
        #region 내부 의존성 필드 (Private Fields)
        private readonly ClawGameViewModel m_viewModel;
        private readonly QuizUI_View m_quizUIView;
        private readonly ClawMachineExitView m_exitView;
        private readonly ClawGameResultPopupView m_resultPopupView;
        private readonly ClawGameView m_gameView;
        private readonly PlayerSO m_playerSO;
        private readonly QuizDatabaseSO m_quizDatabase;
        private readonly ClawSceneReferencesDTO m_sceneReferences;

        // 씬 참조 캐싱 및 기본 상수 제어
        private Transform m_dollsContainer;
        private BoxCollider2D m_spawnAreaCollider;
        private const float SPAWN_MIN_DISTANCE = 0.6f;
        private const int MAX_SPAWN_ATTEMPTS = 30;
        #endregion

        #region 생성자 의존성 주입 (Constructor DI)
        /// <summary>
        /// [기능]: VContainer를 통해 의존성을 주입받아 초기화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public ClawGameFlowController(
            ClawGameViewModel viewModel,
            QuizUI_View quizUIView,
            ClawMachineExitView exitView,
            ClawGameResultPopupView resultPopupView,
            ClawGameView gameView,
            PlayerSO playerSO,
            QuizDatabaseSO quizDatabase,
            ClawSceneReferencesDTO sceneReferences)
        {
            m_viewModel = viewModel;
            m_quizUIView = quizUIView;
            m_exitView = exitView;
            m_resultPopupView = resultPopupView;
            m_gameView = gameView;
            m_playerSO = playerSO;
            m_quizDatabase = quizDatabase;
            m_sceneReferences = sceneReferences;
        }
        #endregion

        #region 진입점 인터페이스 구현 (IStartable)
        /// <summary>
        /// [기능]: VContainer 컨테이너 빌드가 완료된 직후 실행되는 인형뽑기 씬의 진입점 메서드
        /// [작성자]: 윤승종
        /// </summary>
        public void Start()
        {
            Debug.Log("[ClawGameFlowController] 인형뽑기 게임 흐름 제어를 개시합니다.");

            // [성능 최적화]: DTO를 통해 전달받은 참조를 바로 사용합니다. (GameObject.Find 완전 배제)
            if (m_sceneReferences != null)
            {
                m_dollsContainer = m_sceneReferences.DollsContainer;
                m_spawnAreaCollider = m_sceneReferences.SpawnAreaCollider;
            }
            else
            {
                Debug.LogError("[ClawGameFlowController] ClawSceneReferencesDTO가 주입되지 않았습니다.");
            }

            // 1. 세션 시간 리셋
            if (m_playerSO != null)
            {
                m_playerSO.TotalMinigamePlayTime = 0f;
            }

            if (m_viewModel == null)
            {
                Debug.LogError("[ClawGameFlowController] ClawGameViewModel이 올바르게 주입되지 않았습니다.");
                return;
            }

            // 2. 퀴즈 무작위 추출 및 뷰모델 세팅
            QuizData selectedQuiz = null;
            if (m_quizDatabase != null && m_quizDatabase.QuizList != null && m_quizDatabase.QuizList.Count > 0)
            {
                List<QuizData> clawQuizzes = new List<QuizData>();
                for (int i = 0; i < m_quizDatabase.QuizList.Count; i++)
                {
                    if (m_quizDatabase.QuizList[i] != null && m_quizDatabase.QuizList[i].QuizType == QuizType.ClawMachine)
                    {
                        clawQuizzes.Add(m_quizDatabase.QuizList[i]);
                    }
                }

                if (clawQuizzes.Count > 0)
                {
                    int randomIndex = Random.Range(0, clawQuizzes.Count);
                    selectedQuiz = clawQuizzes[randomIndex];
                }
            }

            if (selectedQuiz == null)
            {
                selectedQuiz = new QuizData(
                    "UX/UI [?] 에 대해 아십니까?",
                    "아이콘",
                    new List<string> { "폰트", "팝업", "체크박스" },
                    QuizType.ClawMachine
                );
            }

            m_viewModel.SetQuiz(selectedQuiz);

            // 3. 퀴즈 캡슐 동적 스폰
            SpawnQuizDolls(m_viewModel, selectedQuiz);

            // 4. 결과 팝업 뷰 초기화
            if (m_resultPopupView != null)
            {
                m_resultPopupView.Initialize(m_viewModel);
                Debug.Log("[ClawGameFlowController] ClawGameResultPopupView 초기화 완료.");
            }

            // 5. 최상위 UI 게임 뷰 초기화
            if (m_gameView != null)
            {
                m_gameView.Initialize(m_viewModel, m_resultPopupView);
                Debug.Log("[ClawGameFlowController] ClawGameView 초기화 및 팝업 연결 완료.");
            }

            // 6. 물리 센서 및 퀴즈 UI 뷰 초기화
            if (m_exitView != null)
            {
                m_exitView.Initialize(m_viewModel);
            }
            if (m_quizUIView != null)
            {
                m_quizUIView.Initialize(m_viewModel);
            }

            // 7. 실시간 타이머 및 인게임 가동 개시
            m_viewModel.StartGame();
            
            Debug.Log("[ClawGameFlowController] 퀴즈 기반 인형뽑기 씬 초기화 및 의존성 조립 성공.");
        }
        #endregion

        #region 내부 헬퍼 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 출제된 퀴즈의 정답 1개와 오답 3개를 캡슐에 담아 물리 공간에 배치하고 뷰모델에 정답 테이블을 등록합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void SpawnQuizDolls(ClawGameViewModel viewModel, QuizData quiz)
        {
            if (m_dollsContainer == null)
            {
                Debug.LogError("[ClawGameFlowController] Dolls_Container가 지정되지 않았습니다.");
                return;
            }

            if (m_sceneReferences == null || m_sceneReferences.CapsulePrefab == null)
            {
                Debug.LogError("[ClawGameFlowController] 템플릿 Capsule 프리팹 참조가 DTO에 없습니다.");
                return;
            }

            GameObject templateCapsule = m_sceneReferences.CapsulePrefab;
            templateCapsule.SetActive(false); // 원본 숨김

            // 1. 선택지 리스트 조립 (정답 1개 + 오답 최대 3개)
            var choices = new List<string>();
            choices.Add(quiz.CorrectAnswer);

            int wrongCount = Mathf.Min(quiz.WrongAnswers.Count, 3);
            for (int i = 0; i < wrongCount; i++)
            {
                choices.Add(quiz.WrongAnswers[i]);
            }

            // 2. 선택지 피셔-예이츠 셔플로 순서 무작위화
            for (int i = choices.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                string temp = choices[i];
                choices[i] = choices[r];
                choices[r] = temp;
            }

            viewModel.ClearDollAnswers();

            // 3. 캡슐 4개 동적 스폰 및 시각적 HSL 컬러링
            Color[] capsuleColors = new Color[]
            {
                new Color(0.4f, 0.7f, 1.0f, 1.0f),
                new Color(1.0f, 0.5f, 0.5f, 1.0f),
                new Color(0.5f, 0.9f, 0.6f, 1.0f),
                new Color(1.0f, 0.85f, 0.4f, 1.0f)
            };

            List<Vector2> spawnedPositions = new List<Vector2>(choices.Count);

            for (int i = 0; i < choices.Count; i++)
            {
                GameObject dollGo = UnityEngine.Object.Instantiate(templateCapsule, m_dollsContainer);
                if (dollGo != null)
                {
                    string answerText = choices[i];
                    bool isCorrect = (answerText == quiz.CorrectAnswer);
                    
                    dollGo.name = $"Capsule_Answer_{i}";
                    dollGo.SetActive(true);

                    // BoxCollider2D 영역 내부에서 겹치지 않는 무작위 위치 획득
                    Vector3 pos = GetRandomNonOverlappingPosition(spawnedPositions);
                    dollGo.transform.position = pos;
                    spawnedPositions.Add(pos);

                    SpriteRenderer sr = dollGo.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = capsuleColors[i % capsuleColors.Length];
                    }

                    ClawMachineDollView dollView = dollGo.GetComponent<ClawMachineDollView>();
                    if (dollView != null)
                    {
                        DollModel dollModel = new DollModel(
                            dollGo.name, 
                            $"Choice_{i}", 
                            1.0f, 
                            false, 
                            answerText, 
                            isCorrect
                        );
                        dollView.Initialize(dollModel);
                    }

                    viewModel.RegisterDollAnswer(dollGo.name, isCorrect);
                }
            }

            Debug.Log($"[ClawGameFlowController] 퀴즈 캡슐 {choices.Count}개 동적 스폰 완료.");
        }

        /// <summary>
        /// [기능]: 지정된 BoxCollider2D 내부 영역에서 기존에 배치된 캡슐 위치와 겹치지 않는 무작위 좌표를 계산합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private Vector3 GetRandomNonOverlappingPosition(List<Vector2> existingPositions)
        {
            Vector3 defaultPos = m_dollsContainer != null ? m_dollsContainer.position : Vector3.zero;
            if (m_spawnAreaCollider == null)
            {
                Debug.LogWarning("[ClawGameFlowController] m_spawnAreaCollider가 설정되지 않아 기본 위치에 스폰합니다.");
                return defaultPos;
            }

            Bounds bounds = m_spawnAreaCollider.bounds;
            Vector3 bestPos = Vector3.zero;

            for (int attempt = 0; attempt < MAX_SPAWN_ATTEMPTS; attempt++)
            {
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);
                Vector2 candidatePos = new Vector2(randomX, randomY);

                bool isOverlapping = false;
                for (int i = 0; i < existingPositions.Count; i++)
                {
                    if (Vector2.Distance(candidatePos, existingPositions[i]) < SPAWN_MIN_DISTANCE)
                    {
                        isOverlapping = true;
                        break;
                    }
                }

                if (!isOverlapping)
                {
                    bestPos = new Vector3(candidatePos.x, candidatePos.y, bounds.center.z);
                    return bestPos;
                }

                bestPos = new Vector3(candidatePos.x, candidatePos.y, bounds.center.z);
            }

            return bestPos;
        }
        #endregion
    }
}
