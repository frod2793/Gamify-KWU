using UnityEngine;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using GamifyKWU.CraneGame.Data;
using GameArifiction.Player;
using GameArifiction.Core.Audio;
using GameArifiction.UI.Common;
using GameArifiction.QuizClassic;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기(ClawMachine) 씬의 초기화 및 흐름을 제어하는 순수 C# EntryPoint 클래스입니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-12
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: ClawGameView.Initialize 호출 시 의존성 주입 최적화에 맞추어 단일 매개변수(viewModel)만 전달하도록 수정
    /// </summary>
    public class ClawGameFlowController : IStartable, System.IDisposable
    {
        #region 내부 의존성 필드 (Private Fields)
        private readonly ClawGameViewModel m_viewModel;
        private readonly ClawGameQuizUI_View m_quizUIView;
        private readonly ClawMachineExitView m_exitView;
        private readonly ClawGameResultPopupView m_resultPopupView;
        private readonly ClawGameView m_gameView;
        private readonly QuizDatabaseSO m_quizDatabase;
        private readonly ClawSceneReferencesDTO m_sceneReferences;
        private readonly ClawGameTutorialPopupView m_tutorialPopupView;
        private readonly ISoundService m_soundService;
        private readonly QuizClassicFlowController m_quizFlowController;
        private readonly QuizClassicView m_quizClassicView;

        // 씬 참조 캐싱 및 기본 상수 제어
        private const float SPAWN_MIN_DISTANCE = 0.6f;
        private const int MAX_SPAWN_ATTEMPTS = 30;

        // [최적화]: 캡슐 재사용을 위한 Object Pool
        private List<GameObject> m_dollPool = new List<GameObject>();

        // [신규]: 사용자가 제공한 5대 실제 퀴즈 폴백 데이터셋
        private static readonly List<QuizData> s_fallbackQuizzes = new List<QuizData>
        {
            new QuizData(
                "(?) 정보를 시각적으로 압축하여 전달하는 UI 요소이다. 텍스트보다 빠르게 의미를 전달하며 메뉴, 기능, 상태 표시 등에 활용되고 스마트폰 앱에서 자주 사용된다.",
                "아이콘",
                new List<string> { "폰트", "팝업", "체크박스" },
                QuizType.ClawMachine,
                "정보를 시각적으로 압축하여 전달하는 UI 요소입니다.",
                QuizCategory.UIUX,
                "그림으로 핵심을 보여주는 것!"
            ),
            new QuizData(
                "(?)은 화면 위에 나타나 사용자의 주의를 끌고, 중요한 정보를 전달하는 UI 요소이다. 사용자의 시선을 집중시키며 알림, 경고, 확인 요청, 안내 등에 활용되고 사용자의 선택(확인, 취소 등)을 받아 다음 행동을 유도한다.",
                "팝업",
                new List<string> { "아이콘", "드롭박스", "체크박스" },
                QuizType.ClawMachine,
                "화면 위에 나타나 사용자의 주의를 끌고 정보를 전달하는 UI 요소입니다.",
                QuizCategory.UIUX,
                "갑자기 나타나는 창!"
            ),
            new QuizData(
                "(?)은 여러 옵션 중 하나를 선택할 수 있게 해주는 UI 요소이다. 클릭하여 목록을 열고 옵션을 확인하며, 여러 항목 중 하나를 선택하는 데 활용되고 선택한 내용을 현재 화면에 간결하게 표시한다.",
                "드롭박스",
                new List<string> { "아이콘", "팝업", "체크박스" },
                QuizType.ClawMachine,
                "여러 옵션 중 하나를 선택할 수 있게 해주는 UI 요소입니다.",
                QuizCategory.UIUX,
                "목록에서 하나를 골라 선택하는 것!"
            ),
            new QuizData(
                "(?)은 항목을 선택하거나 해제할 수 있게 해주는 UI 요소이다. 클릭하여 선택 상태를 변경하며, 여러 항목을 동시에 선택하거나 해제하는 데 활용되고 선택 여부를 시각적으로 보여준다.",
                "체크박스",
                new List<string> { "아이콘", "드롭박스", "팝업" },
                QuizType.ClawMachine,
                "항목을 선택하거나 해제할 수 있게 해주는 UI 요소입니다.",
                QuizCategory.UIUX,
                "작은 상자를 클릭해 선택을 표시하는 것!"
            ),
            new QuizData(
                "(?)은 글자의 모양, 크기, 두께 등을 설정할 수 있게 해주는 UI 요소이다. 클릭하여 글자 스타일을 변경하고 여러 스타일 중 하나를 선택하여 적용하는 데 활용되며 가독성과 분위기를 조절한다.",
                "폰트",
                new List<string> { "아이콘", "팝업", "체크박스" },
                QuizType.ClawMachine,
                "글자의 모양, 크기, 두께 등을 설정할 수 있게 해주는 UI 요소입니다.",
                QuizCategory.UIUX,
                "글자의 스타일을 바꿔 적용하는 것!"
            )
        };
        #endregion

        #region 생성자 의존성 주입 (Constructor DI)
        /// <summary>
        /// [기능]: VContainer를 통해 의존성을 주입받아 초기화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public ClawGameFlowController(
            ClawGameViewModel viewModel,
            ClawGameQuizUI_View quizUIView,
            ClawMachineExitView exitView,
            ClawGameResultPopupView resultPopupView,
            ClawGameView gameView,
            QuizDatabaseSO quizDatabase,
            ClawSceneReferencesDTO sceneReferences,
            ClawGameTutorialPopupView tutorialPopupView,
            ISoundService soundService,
            QuizClassicFlowController quizFlowController,
            QuizClassicView quizClassicView)
        {
            m_viewModel = viewModel;
            m_quizUIView = quizUIView;
            m_exitView = exitView;
            m_resultPopupView = resultPopupView;
            m_gameView = gameView;
            m_quizDatabase = quizDatabase;
            m_sceneReferences = sceneReferences;
            m_tutorialPopupView = tutorialPopupView;
            m_soundService = soundService;
            m_quizFlowController = quizFlowController;
            m_quizClassicView = quizClassicView;
        }
        #endregion

        #region 진입점 인터페이스 구현 (IStartable, IDisposable)
        /// <summary>
        /// [기능]: VContainer 컨테이너 빌드가 완료된 직후 실행되는 인형뽑기 씬의 진입점 메서드
        /// [작성자]: 윤승종
        /// </summary>
        public void Start()
        {
            Debug.Log("[ClawGameFlowController] 인형뽑기 게임 흐름 제어를 개시합니다.");

            // [사운드 설정]: 씬 시작 시 배경음악 재생
            if (m_soundService != null)
            {
                m_soundService.PlayBGM(SoundDefine.Bgm_claw);
                Debug.Log("[ClawGameFlowController] 배경음악(Bgm_claw) 재생을 개시했습니다.");
            }

            // [성능 최적화]: DTO를 통해 전달받은 참조를 바로 사용합니다. (GameObject.Find 완전 배제)
            if (m_sceneReferences == null)
            {
                Debug.LogError("[ClawGameFlowController] ClawSceneReferencesDTO가 주입되지 않았습니다.");
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
                int randomIndex = Random.Range(0, s_fallbackQuizzes.Count);
                selectedQuiz = s_fallbackQuizzes[randomIndex];
            }

            m_viewModel.SetQuiz(selectedQuiz);

            // 3. 퀴즈 캡슐 동적 스폰
            SpawnQuizDolls(m_viewModel, selectedQuiz);

            // 4. 결과 팝업 뷰 초기화 (정오답 이벤트 자체 구독 및 연출)
            if (m_resultPopupView != null)
            {
                m_resultPopupView.Initialize(m_viewModel);
                Debug.Log("[ClawGameFlowController] ClawGameResultPopupView 초기화 완료.");
            }

            // 5. 최상위 UI 게임 뷰 초기화 (단일 뷰모델 바인딩으로 최적화)
            if (m_gameView != null)
            {
                m_gameView.Initialize(m_viewModel);
                Debug.Log("[ClawGameFlowController] ClawGameView 초기화 완료.");
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
            if (m_tutorialPopupView != null)
            {
                m_tutorialPopupView.Initialize(m_viewModel);
                Debug.Log("[ClawGameFlowController] ClawGameTutorialPopupView 초기화 완료.");
            }

            // 7. 실시간 타이머 및 인게임 가동 개시
            m_viewModel.StartGame();
            
            Debug.Log("[ClawGameFlowController] 퀴즈 기반 인형뽑기 씬 초기화 및 의존성 조립 성공.");
        }

        /// <summary>
        /// [기능]: FlowController가 제거되거나 씬 전환으로 스코프가 해제될 때 리소스를 정리하고 사운드를 정지합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Dispose()
        {


            if (m_soundService != null)
            {
                m_soundService.StopBGM();
                Debug.Log("[ClawGameFlowController] 씬 퇴출로 인한 배경음악 정지 완료.");
            }
        }
        #endregion

        #region 내부 헬퍼 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 출제된 퀴즈의 정답 1개와 오답 4개를 캡슐에 담아 물리 공간에 배치하고 뷰모델에 정답 테이블을 등록합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 프리팹에 지정된 AnswerSpriteConfigs의 키워드만 사용하여 캡슐 선택지 구성하도록 제한
        /// </summary>
        private void SpawnQuizDolls(ClawGameViewModel viewModel, QuizData quiz)
        {
            if (m_sceneReferences == null || m_sceneReferences.DollsContainer == null)
            {
                Debug.LogError("[ClawGameFlowController] DollsContainer DTO 참조가 지정되지 않았습니다.");
                return;
            }

            if (m_sceneReferences.CapsulePrefab == null)
            {
                Debug.LogError("[ClawGameFlowController] 템플릿 Capsule 프리팹 참조가 DTO에 없습니다.");
                return;
            }

            GameObject templateCapsule = m_sceneReferences.CapsulePrefab;
            templateCapsule.SetActive(false); // 원본 숨김

            // 1. 선택지 리스트 조립 (ClawMachineDollView에 구성된 AnswerSpriteConfigs의 키워드만 추출하여 사용)
            var choices = new List<string>();
            ClawMachineDollView templateDollView = templateCapsule.GetComponent<ClawMachineDollView>();
            if (templateDollView != null && templateDollView.AnswerSpriteConfigs != null && templateDollView.AnswerSpriteConfigs.Count > 0)
            {
                for (int i = 0; i < templateDollView.AnswerSpriteConfigs.Count; i++)
                {
                    choices.Add(templateDollView.AnswerSpriteConfigs[i].AnswerTextKey);
                }
            }
            else
            {
                // 폴백 방어 코드: 인스펙터 설정이 누락되어 비어있는 경우 기존 퀴즈 데이터를 토대로 추출
                choices.Add(quiz.CorrectAnswer);
                int wrongCount = Mathf.Min(quiz.WrongAnswers.Count, 4);
                for (int i = 0; i < wrongCount; i++)
                {
                    choices.Add(quiz.WrongAnswers[i]);
                }
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

            // 3. 캡슐 5개 동적 스폰 및 시각적 HSL 컬러링
            Color[] capsuleColors = new Color[]
            {
                new Color(0.4f, 0.7f, 1.0f, 1.0f),
                new Color(1.0f, 0.5f, 0.5f, 1.0f),
                new Color(0.5f, 0.9f, 0.6f, 1.0f),
                new Color(1.0f, 0.85f, 0.4f, 1.0f),
                new Color(0.8f, 0.6f, 0.9f, 1.0f)
            };

            List<Vector2> spawnedPositions = new List<Vector2>(choices.Count);

            // [최적화]: 기존 풀 비활성화 및 상태 초기화
            for (int i = 0; i < m_dollPool.Count; i++)
            {
                if (m_dollPool[i] != null)
                {
                    m_dollPool[i].SetActive(false);
                }
            }

            for (int i = 0; i < choices.Count; i++)
            {
                GameObject dollGo;
                if (i < m_dollPool.Count && m_dollPool[i] != null)
                {
                    dollGo = m_dollPool[i];
                }
                else
                {
                    dollGo = UnityEngine.Object.Instantiate(templateCapsule, m_sceneReferences.DollsContainer);
                    m_dollPool.Add(dollGo);
                }

                if (dollGo != null)
                {
                    string answerText = choices[i];
                    bool isCorrect = (answerText == quiz.CorrectAnswer);
                    
                    dollGo.name = $"Capsule_Answer_{i}";

                    // 물리 상태 리셋 (Zero Allocation)
                    Rigidbody2D rb = dollGo.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                        dollGo.transform.rotation = Quaternion.identity;
                    }

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
                        dollView.Initialize(new DollStateDTO(dollModel));
                    }

                    viewModel.RegisterDollAnswer(dollGo.name, answerText, isCorrect);
                }
            }

            Debug.Log($"[ClawGameFlowController] 퀴즈 캡슐 {choices.Count}개 Object Pool 재활용 배치 완료.");
        }

        /// <summary>
        /// [기능]: 지정된 BoxCollider2D 내부 영역에서 기존에 배치된 캡슐 위치와 겹치지 않는 무작위 좌표를 계산합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private Vector3 GetRandomNonOverlappingPosition(List<Vector2> existingPositions)
        {
            Vector3 defaultPos = Vector3.zero;
            if (m_sceneReferences != null)
            {
                if (m_sceneReferences.DollsContainer != null)
                {
                    defaultPos = m_sceneReferences.DollsContainer.position;
                }
            }

            if (m_sceneReferences == null || m_sceneReferences.SpawnAreaCollider == null)
            {
                Debug.LogWarning("[ClawGameFlowController] SpawnAreaCollider DTO 참조가 설정되지 않아 기본 위치에 스폰합니다.");
                return defaultPos;
            }

            Bounds bounds = m_sceneReferences.SpawnAreaCollider.bounds;
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
