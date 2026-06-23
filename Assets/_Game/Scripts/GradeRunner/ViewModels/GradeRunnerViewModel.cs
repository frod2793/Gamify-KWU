using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameArifiction.Player;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)의 핵심 타이머 루프, 장애물/아이템 스폰 주기 통제, 충돌 연산 및 점수/등급 판정을 수행하는 ViewModel (POCO)
/// [작성자]: 윤승종
/// [수정 날짜]: 2026-06-13
/// [마지막 수정 작성자]: 윤승종
/// [수정 내용]: 2페이즈 전환 시점에 남은 시간 소수점 오차 정밀 클램핑 추가
/// </summary>
namespace GameArifiction.GradeRunner
{
    public enum GradeRunnerState
    {
        Idle,
        Tutorial,       // [신규]: 튜토리얼 팝업 대기 상태
        IntroCutscene,
        Playing,
        Phase2Cutscene,
        GameEndCutscene,
        Result
    }

    /// <summary>
    /// 게임 시간 경과에 따른 교수 공격 페이즈 단계
    /// </summary>
    public enum GradeRunnerPhase
    {
        Phase1, // 1페이즈 (온화한 교수님)
        Phase2  // 2페이즈 (분노/열정의 교수님)
    }

    public class GradeRunnerViewModel : IStartable, IDisposable
    {
        #region 내부 필드 (Private Fields)

        private readonly GradeRunnerModel m_model;
        private readonly GradeRunnerConfigSO m_config;
        private readonly GradeRunnerDialogueSO m_dialogueSO;
        private readonly PlayerSO m_playerSO;

        private GradeRunnerState m_currentState;
        private GradeRunnerPhase m_currentPhase;
        private CancellationTokenSource m_gameCts;

        private float m_playerTraverseDuration; // 플레이어의 이번 편도 소요 시간 (4~5초)
        private bool m_isPaused; // 2페이즈 전환 컷씬 등을 위한 일시정지 플래그

        // 족보 지정 초 스폰 완료 여부 플래그
        private bool m_spawned24;
        private bool m_spawned15;
        private bool m_spawned10;
        private bool m_spawned7;
        private bool m_spawned3;
        private bool m_allCheatSheetsSpawned;
        private float m_mobileInputX; // 모바일 가상 패드 입력 값

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        public event Action<float> OnTimeChanged;
        public event Action<float> OnGradePointChanged;
        public event Action<string> OnGradeLetterChanged;
        public event Action<GradeRunnerState> OnGameStateChanged;
        public event Action<GradeRunnerPhase> OnPhaseChanged; 
        public event Action<float, Vector2> OnScoreFeedback; // 학점 변화량, 충돌 위치
        public event Action<FallingObjectType, CodeColorType, float> OnSpawnFallingObject; // 스폰할 오브젝트 정보 (타입, 색상, 스폰 가속도)
        public event Action OnClearFallingObjects; // 스폰된 모든 낙하물 청소 이벤트
        public event Action<GradeRunnerResultDTO> OnGameResult;

        public event Action OnIntroCutsceneStarted; // 도입부 교수님 등장 대사 이벤트
        public event Action OnPhase2CutsceneStarted; // 2페이즈 돌입 교수님 분노 대사 이벤트
        public event Action OnGameEndCutsceneStarted; // 게임 종료 교수님 연출 이벤트
        public event Action<bool, bool> OnPauseStateChanged; // 일시정지 상태 변경 알림 이벤트 (bool isPaused, bool isUserPause)

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public GradeRunnerState CurrentState => m_currentState;
        public GradeRunnerPhase CurrentPhase => m_currentPhase;
        public float CurrentGradePoint => m_model.CurrentGradePoint;
        public float RemainingTime => m_model.RemainingTime;
        public float MaxGradePoint => m_model.MaxGradePoint;
        public float GameDuration => m_model.GameDuration;

        /// <summary>
        /// [기능]: 현재 게임이 실질적 플레이 진행이 가능한 활성 상태인지를 판별합니다.
        /// </summary>
        public bool IsPlayable => m_currentState == GradeRunnerState.Playing && !m_isPaused;

        public string IntroDialogue => m_dialogueSO != null ? m_dialogueSO.IntroDialogue : "자, 지금부터 코딩 테스트를 시작하겠다!";
        public string Phase2Dialogue => m_dialogueSO != null ? m_dialogueSO.Phase2Dialogue : "아직 끝나지 않았다! 진정한 매운맛을 보여주지!";
        public string GameEndDialogue => m_dialogueSO != null ? m_dialogueSO.GameEndDialogue : "만족스럽군요!!";

        public float MobileInputX
        {
            get
            {
                return m_mobileInputX;
            }
            set
            {
                m_mobileInputX = value;
            }
        }

        #endregion

        #region 초기화 (Initialization)

        public GradeRunnerViewModel(GradeRunnerModel model, GradeRunnerConfigSO config, GradeRunnerDialogueSO dialogueSO, PlayerSO playerSO)
        {
            m_model = model;
            m_config = config;
            m_dialogueSO = dialogueSO;
            m_playerSO = playerSO;
            m_currentState = GradeRunnerState.Idle;
            m_currentPhase = GradeRunnerPhase.Phase1;

            // 랜덤 편도 소요 시간 설정 (4초 ~ 5초 사이)
            m_playerTraverseDuration = UnityEngine.Random.Range(m_config.PlayerTraverseDurationMin, m_config.PlayerTraverseDurationMax);
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: VContainer IStartable 진입점. 게임을 가동시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Start()
        {
            StartGame();
        }

        /// <summary>
        /// [기능]: 미니게임을 시작하고 도입부 컷씬 연출을 트리거합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void StartGame()
        {
            StopGameTasks();

            // 스폰되어 있는 잔여 코드/족보 오브젝트들 일괄 제거(회수) 이벤트 통보
            OnClearFallingObjects?.Invoke();

            m_model.CurrentGradePoint = m_config.StartGradePoint;
            m_model.RemainingTime = m_config.GameDuration;
            m_currentPhase = GradeRunnerPhase.Phase1;
            m_isPaused = false;

            // 족보 스폰 플래그 초기화
            m_spawned24 = false;
            m_spawned15 = false;
            m_spawned10 = false;
            m_spawned7 = false;
            m_spawned3 = false;
            m_allCheatSheetsSpawned = false;

            if (m_playerSO != null)
            {
                m_playerSO.TotalMinigamePlayTime = 0f;
            }

            m_gameCts = new CancellationTokenSource();

            // 초기 이벤트 데이터 방송
            NotifyGradeChanged();
            OnPhaseChanged?.Invoke(m_currentPhase);

            // 도입부 튜토리얼 대기 상태로 진입
            ChangeState(GradeRunnerState.Tutorial);

            Debug.Log("[GradeRunnerViewModel] 피하기 미니게임이 기동되어 등장 컷씬이 시작되었습니다.");
        }

        /// <summary>
        /// [기능]: 도입부 교수 대사 연출이 끝났을 때 뷰에서 호출하여 실제 게임 플레이(1페이즈)를 개시합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void CompleteIntroCutscene()
        {
            if (m_currentState != GradeRunnerState.IntroCutscene)
            {
                return;
            }

            ChangeState(GradeRunnerState.Playing);

            // 실시간 타이머 및 오브젝트 스폰 루프 기동
            RunTimerAsync(m_gameCts.Token).Forget();
            RunCodeSpawnerAsync(m_gameCts.Token).Forget();

            Debug.Log("[GradeRunnerViewModel] 교수 등장 컷씬 완료. 1페이즈 플레이 개시.");
        }

        /// <summary>
        /// [기능]: 2페이즈 전환 대사 연출이 끝났을 때 뷰에서 호출하여 2페이즈 게임 진행을 재개합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void CompletePhase2Cutscene()
        {
            if (m_currentState != GradeRunnerState.Phase2Cutscene)
            {
                return;
            }

            ChangeState(GradeRunnerState.Playing);
            OnPauseStateChanged?.Invoke(false, false);

            Debug.Log("[GradeRunnerViewModel] 2페이즈 전환 컷씬 완료. 게임플레이 재개.");
        }

        /// <summary>
        /// [기능]: 0초 종료 연출 컷씬이 끝났을 때 뷰에서 호출하여 최종 결과를 집계하고 결과 팝업을 띄웁니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void CompleteGameEndCutscene()
        {
            if (m_currentState != GradeRunnerState.GameEndCutscene)
            {
                return;
            }

            Debug.Log("[GradeRunnerViewModel] 게임 종료 컷씬 완료. 결과 화면으로 진입합니다.");
            FinishGame();
        }

        /// <summary>
        /// [기능]: 플레이어 편도 시간(4~5초)과 화면 해상도 폭을 기준으로 플레이어의 프레임당 가로 이동 속도를 계산합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public float GetPlayerMoveSpeed(float screenWidth)
        {
            // 속도 = 거리(화면 편도 폭) / 편도 소요시간(초)
            return screenWidth / m_playerTraverseDuration;
        }

        /// <summary>
        /// [기능]: 플레이어가 낙하하는 코드(장애물)와 부딪혔을 때 감점 및 시각적 피드백 연출 이벤트를 송출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ApplyCodeHit(Vector2 hitPosition)
        {
            if (!IsPlayable)
            {
                return;
            }

            float penalty = m_config.CodePenalty;
            m_model.CurrentGradePoint -= penalty;

            NotifyGradeChanged();
            OnScoreFeedback?.Invoke(-penalty, hitPosition);
        }

        /// <summary>
        /// [기능]: 플레이어가 락하하는 족보(아이템)와 부딪혔을 때 가점 및 한도 캡 체크 후 연출 이벤트를 송출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ApplyCheatSheetPickup(Vector2 hitPosition)
        {
            if (!IsPlayable)
            {
                return;
            }

            if (m_model.CurrentGradePoint >= m_model.MaxGradePoint)
            {
                // 이미 Full(5점) 상태이면 더 이상 채워지지 않음
                OnScoreFeedback?.Invoke(0f, hitPosition);
                return;
            }

            float bonus = m_config.CheatSheetBonus;
            m_model.CurrentGradePoint += bonus;

            NotifyGradeChanged();
            OnScoreFeedback?.Invoke(bonus, hitPosition);
        }

        /// <summary>
        /// [기능]: 플레이어가 튜토리얼을 종료했을 때 호출되며, 도입부 컷씬 상태로 전환하거나 일시정지를 해제합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_CompleteTutorial()
        {
            if (m_currentState == GradeRunnerState.Tutorial)
            {
                ChangeState(GradeRunnerState.IntroCutscene);
                OnIntroCutsceneStarted?.Invoke();
                Debug.Log("[GradeRunnerViewModel] 튜토리얼이 완료되어 등장 컷씬이 시작되었습니다.");
            }
            else
            {
                ResumeGame();
                Debug.Log("[GradeRunnerViewModel] 인게임 중 튜토리얼 다시보기 완료로 일시정지를 해제합니다.");
            }
        }

        /// <summary>
        /// [기능]: 게임을 일시정지 상태로 변경합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void PauseGame()
        {
            m_isPaused = true;
            OnPauseStateChanged?.Invoke(true, true);
            Debug.Log("[GradeRunnerViewModel] 게임이 일시정지되었습니다.");
        }

        /// <summary>
        /// [기능]: 일시정지된 게임을 다시 진행 상태로 재개합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ResumeGame()
        {
            m_isPaused = false;
            OnPauseStateChanged?.Invoke(false, true);
            Debug.Log("[GradeRunnerViewModel] 게임이 재개되었습니다.");
        }

        /// <summary>
        /// [기능]: 뷰모델 객체 해제 시 활성화된 비동기 루프를 안전하게 종료시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Dispose()
        {
            StopGameTasks();
        }

        #endregion

        #region 내부 메서드 (Private Methods)

        private void ChangeState(GradeRunnerState newState)
        {
            if (m_currentState == newState)
            {
                return;
            }

            m_currentState = newState;
            OnGameStateChanged?.Invoke(m_currentState);
        }

        private void StopGameTasks()
        {
            if (m_gameCts != null)
            {
                m_gameCts.Cancel();
                m_gameCts.Dispose();
                m_gameCts = null;
            }
        }

        /// <summary>
        /// [기능]: 30초에서 0초까지 매 프레임 남은 시간을 줄이고, 지정 조건(10초 이하)에서 2페이즈로 넘어가며, 지정 특정 잔여시간(24s/15s/10s/7s/3s)에 족보를 스폰시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        private async UniTaskVoid RunTimerAsync(CancellationToken token)
        {
            OnTimeChanged?.Invoke(m_model.RemainingTime);

            while (m_model.RemainingTime > 0f)
            {
                bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
                if (isCanceled || token.IsCancellationRequested)
                {
                    return;
                }

                await WaitWhilePausedAsync(token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                float dt = Time.deltaTime;
                m_model.RemainingTime -= dt;

                if (m_playerSO != null)
                {
                    m_playerSO.TotalMinigamePlayTime += dt;
                }

                OnTimeChanged?.Invoke(m_model.RemainingTime);

                // [족보 기획 스폰 연동]: 잔여 초(24s/15s/10s/7s/3s) 크로싱 시점에 정확히 1회씩 스폰
                CheckAndTriggerCheatSheet();

                // [신규 페이즈 체크]: 남은 시간 10초 이하로 떨어지면 2페이즈로 돌입
                if (m_currentPhase == GradeRunnerPhase.Phase1 && m_model.RemainingTime <= m_config.Phase2TransitionTime)
                {
                    m_model.RemainingTime = m_config.Phase2TransitionTime;
                    OnTimeChanged?.Invoke(m_model.RemainingTime); // 타이머 텍스트 10초 동기화 추가
                    m_currentPhase = GradeRunnerPhase.Phase2;
                    OnPhaseChanged?.Invoke(m_currentPhase);

                    // 2페이즈 돌입 시 전환 컷씬 상태로 설정하고 타이머 일시정지
                    OnPauseStateChanged?.Invoke(true, false);
                    ChangeState(GradeRunnerState.Phase2Cutscene);
                    OnPhase2CutsceneStarted?.Invoke();

                    Debug.Log("[GradeRunnerViewModel] 2페이즈에 돌입하였습니다! 컷씬 연출을 위해 시스템 정지를 전파합니다.");
                }
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            // 0초 도달 시 즉시 FinishGame을 하지 않고, 컷씬 상태로 넘긴 후 일시정지 처리
            m_model.RemainingTime = 0f;
            OnPauseStateChanged?.Invoke(true, false);
            ChangeState(GradeRunnerState.GameEndCutscene);
            OnGameEndCutsceneStarted?.Invoke();
            Debug.Log("[GradeRunnerViewModel] 타이머 0초 도달. 게임 종료 연출을 시작합니다.");
        }

        /// <summary>
        /// [기능]: 기획 표에 지정된 시간대(24초/15초/10초/7초/3초)에 맞춰 족보 아이템 스폰을 트리거합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void CheckAndTriggerCheatSheet()
        {
            if (m_allCheatSheetsSpawned) return;

            float remaining = m_model.RemainingTime;

            // 1페이즈 족보: 24초, 15초
            if (!m_spawned24 && remaining <= 24f)
            {
                m_spawned24 = true;
                TriggerCheatSheetSpawn();
            }
            if (!m_spawned15 && remaining <= 15f)
            {
                m_spawned15 = true;
                TriggerCheatSheetSpawn();
            }

            // 2페이즈 족보: 10초, 7초, 3초
            if (!m_spawned10 && remaining <= 10f)
            {
                m_spawned10 = true;
                TriggerCheatSheetSpawn();
            }
            if (!m_spawned7 && remaining <= 7f)
            {
                m_spawned7 = true;
                TriggerCheatSheetSpawn();
            }
            if (!m_spawned3 && remaining <= 3f)
            {
                m_spawned3 = true;
                TriggerCheatSheetSpawn();
                m_allCheatSheetsSpawned = true; // 마지막 족보 스폰 시 조기종료 플래그 활성화
            }
        }

        private void TriggerCheatSheetSpawn()
        {
            float fallDuration = UnityEngine.Random.Range(m_config.FallDurationMin, m_config.FallDurationMax);
            OnSpawnFallingObject?.Invoke(FallingObjectType.CheatSheet, CodeColorType.Red, fallDuration);
        }

        /// <summary>
        /// [기능]: 게임 진행 상태를 결과 상태로 천이하고, 최종 성적 등 DTO를 구성해 이벤트를 방송합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void FinishGame()
        {
            // [추가]: 결과 화면 진입 전 일시정지 상태가 적용되어 있다면 확실하게 해제하여 씬 이동 및 물리 복구를 보장합니다.
            if (m_isPaused)
            {
                m_isPaused = false;
            }
            OnPauseStateChanged?.Invoke(false, true);

            StopGameTasks();

            float finalPoint = m_model.CurrentGradePoint;
            var gradeData = GetGradeData(finalPoint);
            float elapsedTime = m_config.GameDuration - m_model.RemainingTime;

            // PlayerSO에 영구 데이터 기록
            if (m_playerSO != null)
            {
                m_playerSO.SetMinigameGrade("GradeRunner", gradeData.Grade);
            }

            var resultDTO = new GradeRunnerResultDTO(finalPoint, gradeData.Letter, gradeData.Grade, elapsedTime);

            ChangeState(GradeRunnerState.Result);
            OnGameResult?.Invoke(resultDTO);

            Debug.Log($"[GradeRunnerViewModel] 피하기 미니게임이 정상 종료되었습니다. 최종 학점: {finalPoint} ({gradeData.Letter})");
        }

        /// <summary>
        /// [기능]: 페이즈별 기획 표 지정 간격(1P: 0.5~0.7초 / 2P: 0.3~0.5초)에 맞춰 코드를 스폰합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private async UniTaskVoid RunCodeSpawnerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                float interval;
                if (m_currentPhase == GradeRunnerPhase.Phase2)
                {
                    interval = UnityEngine.Random.Range(m_config.CodeSpawnIntervalMinP2, m_config.CodeSpawnIntervalMaxP2);
                }
                else
                {
                    interval = UnityEngine.Random.Range(m_config.CodeSpawnIntervalMinP1, m_config.CodeSpawnIntervalMaxP1);
                }

                bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled || token.IsCancellationRequested)
                {
                    return;
                }

                await WaitWhilePausedAsync(token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                // 색상 가중치에 따른 랜덤 색상 추첨 (빨강 8%, 보라 23%, 노랑 23%, 하늘 23%, 녹색 23%)
                CodeColorType chosenColor = ChooseRandomCodeColor();
                
                // 낙하 속도 무작위 계산 (낙하 소요시간 6초~8초 사이)
                float fallDuration = UnityEngine.Random.Range(m_config.FallDurationMin, m_config.FallDurationMax);

                OnSpawnFallingObject?.Invoke(FallingObjectType.Code, chosenColor, fallDuration);
            }
        }

        /// <summary>
        /// [기능]: 지정 가중치(8%, 23%, 23%, 23%, 23%)에 따라 코드 색상을 결정합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private CodeColorType ChooseRandomCodeColor()
        {
            int rand = UnityEngine.Random.Range(0, 100);
            if (rand < 8)
            {
                return CodeColorType.Red; // 8%
            }
            if (rand < 31)
            {
                return CodeColorType.Purple; // 23%
            }
            if (rand < 54)
            {
                return CodeColorType.Yellow; // 23%
            }
            if (rand < 77)
            {
                return CodeColorType.SkyBlue; // 23%
            }
            return CodeColorType.Green; // 23%
        }

        /// <summary>
        /// [기능]: 학점 값이 변경되었을 때 화면에 노출시킬 데이터와 이벤트를 총괄 갱신합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void NotifyGradeChanged()
        {
            var gradeData = GetGradeData(m_model.CurrentGradePoint);
            OnGradePointChanged?.Invoke(m_model.CurrentGradePoint);
            OnGradeLetterChanged?.Invoke(gradeData.Letter);
        }

        /// <summary>
        /// [기능]: 학점 값을 통해 저장용 Enum 데이터와 화면 노출용 문자열을 통합 산출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private (MinigameGrade Grade, string Letter) GetGradeData(float point)
        {
            if (point >= 5f)
            {
                return (MinigameGrade.A, "A");
            }
            if (point >= 3f)
            {
                return (MinigameGrade.B, "B");
            }
            if (point >= 2f)
            {
                return (MinigameGrade.C, "C");
            }
            if (point >= 1f)
            {
                return (MinigameGrade.D, "D");
            }
            return (MinigameGrade.F, "F");
        }

        /// <summary>
        /// [기능]: 일시정지 활성화 상태 동안 비동기적으로 대기 루프를 유지하는 공통 헬퍼 메서드입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private async UniTask WaitWhilePausedAsync(CancellationToken token)
        {
            // 수동 일시정지 상태이거나, 게임 상태가 Playing이 아닌 컷씬 상태인 동안 대기합니다.
            while (m_isPaused || (m_currentState != GradeRunnerState.Playing && m_currentState != GradeRunnerState.Idle && m_currentState != GradeRunnerState.Tutorial))
            {
                bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
                if (isCanceled || token.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        #endregion
    }
}
