using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using GamifyKWU.CraneGame.Data;
using TMPro;
using GameArifiction.Player;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 씬의 진입점(Composition Root). 싱글톤을 배제하고 퀴즈 데이터 및 관련 뷰 의존성을 런타임 수동 주입합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawGameInitializer : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("게임 전체 UI와 입력을 관리하는 최상위 View 객체입니다.")]
        private ClawGameView m_gameView;

        [SerializeField]
        [Tooltip("인형이 배치될 물리 공간의 부모(Dolls_Container) Transform입니다.")]
        private Transform m_dollsContainer;

        [Header("퀴즈 관련 주입 컴포넌트")]
        [SerializeField]
        [Tooltip("인스펙터에 할당할 퀴즈 데이터베이스 스크립터블 오브젝트입니다.")]
        private QuizDatabaseSO m_quizDatabase;

        [SerializeField]
        [Tooltip("UI Canvas 상의 퀴즈 패널 뷰 컴포넌트입니다.")]
        private QuizUI_View m_quizUIView;

        [SerializeField]
        [Tooltip("물리 퇴출구 영역 센서 뷰 컴포넌트입니다.")]
        private ClawMachineExitView m_exitView;

        [Header("캡슐 스폰 영역 설정")]
        [SerializeField]
        [Tooltip("캡슐들이 스폰될 2D 영역을 지정하는 BoxCollider2D 컴포넌트입니다.")]
        private BoxCollider2D m_spawnAreaCollider;

        [SerializeField]
        [Tooltip("캡슐 스폰 시 다른 캡슐들과 유지해야 할 최소 물리적 거리(반경)입니다.")]
        private float m_spawnMinDistance = 0.6f;

        [SerializeField]
        [Tooltip("적절한 겹치지 않는 위치를 찾기 위한 최대 시도 횟수입니다.")]
        private int m_maxSpawnAttempts = 30;

        [SerializeField]
        [Tooltip("씬에 배치되어 있는 결과 팝업 View 컴포넌트입니다. 인스펙터 미할당 시 씬에서 자동 탐색합니다.")]
        private ClawGameResultPopupView m_resultPopupView;

        [Header("세션 데이터")]
        [SerializeField]
        [Tooltip("씬 간 플레이어 위치 상태 보존을 위한 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;
        #endregion


        #region 유니티 생명주기 (Unity Lifecycle)
        private void Start()
        {
            // [세션 시간 리셋]: 뽑기 게임 개시 시점 기준이므로 총 소요시간 누적값을 0으로 리셋합니다.
            if (m_playerSO != null)
            {
                m_playerSO.TotalMinigamePlayTime = 0f;
            }

            // 1. DTO 수신 (5회 기본 도전 기회, 120초 제한시간)
            var contextDTO = new ClawGameContextDTO(5, 120f, null);
            
            // 2. Model 생성 (DTO 데이터 기반)
            var model = new ClawMachineModel(contextDTO.MaxPlayCount, contextDTO.TimeLimitPerPlay);
            
            // 3. ViewModel 생성
            var viewModel = new ClawGameViewModel(model, m_playerSO);

            // 4. [퀴즈 복원]: 퀴즈 데이터베이스에서 '집게용' 퀴즈만 무작위 1문제 출제 및 바인딩
            QuizData selectedQuiz = null;
            if (m_quizDatabase != null && m_quizDatabase.QuizList != null && m_quizDatabase.QuizList.Count > 0)
            {
                // [필터링]: QuizType.ClawMachine 인 퀴즈만 추출
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
                // 데이터베이스 에셋 유실 대비 최상급 예방용 더미 데이터 폴백 구성
                selectedQuiz = new QuizData(
                    "UX/UI [?] 에 대해 아십니까?",
                    "아이콘",
                    new List<string> { "폰트", "팝업", "체크박스" },
                    QuizType.ClawMachine
                );
            }



            viewModel.SetQuiz(selectedQuiz);
            
            // 5. 객관식 선택지 캡슐 4개 동적 스폰 및 물리 셔플링
            SpawnQuizDolls(viewModel, selectedQuiz);

            // 6. 프리팹 유실 대비 런타임 자체 무결 모달 팝업 조립 및 DI 연동
            SetupResultPopup(viewModel);

            
            // 7. View 주입 (Dependency Injection)
            if (m_gameView != null)
            {
                m_gameView.Initialize(viewModel);
            }
            else
            {
                Debug.LogError("[ClawGameInitializer] ClawGameView가 할당되지 않았습니다. 인스펙터를 확인하세요.");
            }

            // [신규]: 의존성 주입 완료 후 인형뽑기 게임 및 실시간 타이머 가동 개시
            viewModel.StartGame();

            if (m_exitView != null)
            {
                m_exitView.Initialize(viewModel);
                Debug.Log("[ClawGameInitializer] ClawMachineExitView에 대한 의존성 주입 완료.");
            }
            else
            {
                Debug.LogWarning("[ClawGameInitializer] ClawMachineExitView가 지정되지 않았습니다. 물리 감지가 불가능합니다.");
            }

            if (m_quizUIView != null)
            {
                m_quizUIView.Initialize(viewModel);
                Debug.Log("[ClawGameInitializer] QuizUI_View에 대한 의존성 주입 완료.");
            }
            else
            {
                Debug.LogWarning("[ClawGameInitializer] QuizUI_View가 지정되지 않았습니다. 퀴즈 화면 출력이 불가능합니다.");
            }
            
            Debug.Log("[ClawGameInitializer] 퀴즈 기반 인형뽑기 게임 초기화 완료 및 의존성 주입 성공.");
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
                Debug.LogError("[ClawGameInitializer] Dolls_Container가 지정되지 않았습니다.");
                return;
            }

            GameObject templateCapsule = GameObject.Find("ClawMachine_World/Dolls_Container/Capsule");
            if (templateCapsule == null)
            {
                Debug.LogError("[ClawGameInitializer] 템플릿 Capsule 인스턴스를 씬에서 찾을 수 없습니다.");
                return;
            }

            templateCapsule.SetActive(false); // 원본 숨김

            // 1. 선택지 리스트 조립 (정답 1개 + 오답 최대 3개)
            var choices = new List<string>();
            choices.Add(quiz.CorrectAnswer);

            int wrongCount = Mathf.Min(quiz.WrongAnswers.Count, 3);
            for (int i = 0; i < wrongCount; i++)
            {
                choices.Add(quiz.WrongAnswers[i]);
            }


            // 2. 선택지 피셔-예이츠 셔플로 순서 무작위화 (정답 캡슐의 편중 방지)
            for (int i = choices.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                string temp = choices[i];
                choices[i] = choices[r];
                choices[r] = temp;
            }

            // 뷰모델 정답지 초기 청소
            viewModel.ClearDollAnswers();

            // 3. 캡슐 4개 동적 스폰 및 시각적 색상 분산 연출 (고급 HSL 조화 톤)
            Color[] capsuleColors = new Color[]
            {
                new Color(0.4f, 0.7f, 1.0f, 1.0f), // 하늘빛
                new Color(1.0f, 0.5f, 0.5f, 1.0f), // 코랄빛 레드
                new Color(0.5f, 0.9f, 0.6f, 1.0f), // 그린 계열
                new Color(1.0f, 0.85f, 0.4f, 1.0f) // 옐로우 계열
            };

            // 스폰된 캡슐들의 위치를 추적할 리스트 (GC allocation을 최소화하기 위한 사전 할당 용량 지정)
            List<Vector2> spawnedPositions = new List<Vector2>(choices.Count);

            for (int i = 0; i < choices.Count; i++)
            {
                GameObject dollGo = Instantiate(templateCapsule, m_dollsContainer);
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

                    // 개별 컬러 피드백 반영
                    SpriteRenderer sr = dollGo.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = capsuleColors[i % capsuleColors.Length];
                    }

                    // 캡슐 뷰 초기화
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

                    // 뷰모델 정답지에 등록
                    viewModel.RegisterDollAnswer(dollGo.name, isCorrect);
                }
            }

            Debug.Log($"[ClawGameInitializer] 퀴즈 캡슐 {choices.Count}개 동적 스폰 및 뷰모델 퀴즈 바인딩 완료.");
        }

        /// <summary>
        /// [기능]: 씬에 이미 배치되어 있는 결과 패널 모달 팝업 UI를 탐색하고 뷰모델 바인딩 의존성을 주입합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void SetupResultPopup(ClawGameViewModel viewModel)
        {
            // 1. 인스펙터에 미할당된 경우 씬에서 탐색 (비활성화된 오브젝트도 탐색 범위에 포함)
            if (m_resultPopupView == null)
            {
                ClawGameResultPopupView[] popups = FindObjectsByType<ClawGameResultPopupView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (popups != null && popups.Length > 0)
                {
                    m_resultPopupView = popups[0];
                }
            }

            // 2. 뷰 바인딩 및 뷰모델 DI 연동
            if (m_resultPopupView != null)
            {
                m_resultPopupView.Initialize(viewModel);

                // ClawGameView 내부 필드에도 확실히 주입 연동
                if (m_gameView != null)
                {
                    typeof(ClawGameView)
                        .GetField("m_resultPopup", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.SetValue(m_gameView, m_resultPopupView);
                }

                Debug.Log("[ClawGameInitializer] 씬에 배치된 결과 패널 팝업 발견 및 의존성 주입 완료.");
            }
            else
            {
                Debug.LogError("[ClawGameInitializer] 씬에 배치된 ClawGameResultPopupView를 찾을 수 없으며, 인스펙터에도 할당되지 않았습니다.");
            }
        }


        /// <summary>
        /// [기능]: 지정된 BoxCollider2D 내부 영역에서 기존에 배치된 캡슐 위치와 겹치지 않는 무작위 좌표를 계산합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private Vector3 GetRandomNonOverlappingPosition(List<Vector2> existingPositions)
        {
            Vector3 defaultPos = transform.position;
            if (m_spawnAreaCollider == null)
            {
                Debug.LogWarning("[ClawGameInitializer] m_spawnAreaCollider가 설정되지 않아 기본 위치에 스폰합니다.");
                return defaultPos;
            }

            Bounds bounds = m_spawnAreaCollider.bounds;
            Vector3 bestPos = Vector3.zero;

            for (int attempt = 0; attempt < m_maxSpawnAttempts; attempt++)
            {
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);
                Vector2 candidatePos = new Vector2(randomX, randomY);

                bool isOverlapping = false;
                for (int i = 0; i < existingPositions.Count; i++)
                {
                    if (Vector2.Distance(candidatePos, existingPositions[i]) < m_spawnMinDistance)
                    {
                        isOverlapping = true;
                        break;
                    }
                }

                // 겹치지 않는 깨끗한 위치를 찾았다면 즉각 반환
                if (!isOverlapping)
                {
                    bestPos = new Vector3(candidatePos.x, candidatePos.y, bounds.center.z);
                    return bestPos;
                }

                // 만약 겹쳤다면 최종 시도 좌표로 일단 기록(백업용 폴백)
                bestPos = new Vector3(candidatePos.x, candidatePos.y, bounds.center.z);
            }

            Debug.LogWarning($"[ClawGameInitializer] {m_maxSpawnAttempts}회 시도 내에 완전히 겹치지 않는 위치를 찾지 못해 근사 영역에 스폰합니다.");
            return bestPos;
        }
        #endregion

    }
}
