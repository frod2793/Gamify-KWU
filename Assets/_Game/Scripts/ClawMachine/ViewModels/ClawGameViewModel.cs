using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using GamifyKWU.CraneGame.Data;
using GameArifiction.Player;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 게임의 View와 Model을 연결하는 ViewModel
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-27
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 오답 제출 후 초기화 없이 타이머를 이어서 진행하도록 하는 ContinueAfterWrongAnswer 구현 추가
    /// </summary>
    public class ClawGameViewModel : IQuizGameViewModel, IDisposable
    {
        #region 내부 필드 (Private Fields)
        private readonly ClawMachineModel m_model;
        private readonly PlayerSO m_playerSO;
        private ClawStateType m_currentState;
        private CancellationTokenSource m_timerCts;

        // [신규]: 퀴즈 정답 추적 및 캡슐 퀴즈 데이터 매핑 딕셔너리
        private readonly Dictionary<string, bool> m_dollAnswers = new Dictionary<string, bool>();
        private QuizData m_currentQuiz;

        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        public event Action<ClawStateType> OnStateChanged;
        public event Action<int> OnPlayCountChanged;
        public event Action<float> OnTimeChanged;
        
        // 이동 관련 브로드캐스트 (View에서 구독하여 이동 처리)
        public event Action<bool> OnMoveRequested; // true: Right, false: Left
        public event Action OnStopRequested;
        public event Action OnDropRequested; // 도중 강제 놓기 이벤트

        // [신규]: 재수강 시스템 관련 이벤트 정의
        public event Action OnReTakeRequested;
        public event Action OnRemoveDisagreeDollRequested;

        // [신규]: 퀴즈 성공 및 실패 브로드캐스트 이벤트
        public event Action OnQuizSuccess;
        public event Action OnQuizFailed;
        #endregion

        #region 속성 (Properties)
        public ClawStateType CurrentState => m_currentState;
        public bool IsHoldingDoll { get; private set; }
        public bool IsClawClosed { get; private set; }
        public QuizData CurrentQuiz => m_currentQuiz;


        public int ReTakeCount
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.ReTakeCount;
                }
                return 0;
            }
        }

        public float TimeLeft
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.RemainingTime;
                }
                return 120f;
            }
        }
        #endregion

        #region 초기화 (Initialization)
        public ClawGameViewModel(ClawMachineModel model, PlayerSO playerSO)
        {
            m_model = model;
            m_playerSO = playerSO;
            m_currentState = ClawStateType.Idle;
        }

        /// <summary>
        /// [기능]: 인형뽑기 게임을 공식적으로 개시하고 실시간 잔여 제한시간 타이머 카운트다운을 시동합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void StartGame()
        {
            ResetAndStartTimer();
            Debug.Log("[ClawGameViewModel] 인형뽑기 게임 공식 시작 -> 실시간 타이머 작동 개시.");
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 출제된 퀴즈의 정보를 세팅합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void SetQuiz(QuizData quiz)

        {
            m_currentQuiz = quiz;
        }

        /// <summary>
        /// [기능]: 등록되어 있던 캡슐들의 정답 매핑 딕셔너리를 초기화합니다. (새 게임 시작 시 호출)
        /// [작성자]: 윤승종
        /// </summary>
        public void ClearDollAnswers()
        {
            m_dollAnswers.Clear();
        }

        /// <summary>
        /// [기능]: 캡슐의 고유 ID와 정답 여부를 등록합니다. (캡슐 스폰 시 Initializer에서 호출)
        /// [작성자]: 윤승종
        /// </summary>
        public void RegisterDollAnswer(string dollId, bool isCorrect)
        {
            if (!m_dollAnswers.ContainsKey(dollId))
            {
                m_dollAnswers.Add(dollId, isCorrect);
            }
        }

        /// <summary>
        /// [기능]: 플레이어가 인형을 퇴출구에 빠뜨렸을 때 호출되어 정답 여부를 체킹하는 핵심 비즈니스 메서드
        /// [작성자]: 윤승종
        /// </summary>
        public void func_SubmitAnswer(string dollId)
        {
            if (m_dollAnswers.TryGetValue(dollId, out bool isCorrect))
            {
                if (isCorrect)
                {
                    Debug.Log($"[ClawGameViewModel] 정답 골인 감지! 축하합니다. 정답입니다. (DollId: {dollId})");
                    OnQuizSuccess?.Invoke();
                    
                    // 게임 클리어 상태(Result)로 전이
                    ChangeState(ClawStateType.Result);
                }
                else
                {
                    Debug.Log($"[ClawGameViewModel] 오답 골인 감지! 오답입니다. (DollId: {dollId})");
                    OnQuizFailed?.Invoke();
                    
                    // [버그 수정]: 오답 시 OnReTakeRequested 중복 발사 제거.
                    // OnReTakeRequested는 시간 초과 전용 이벤트이며, 오답 실패는 OnQuizFailed 이벤트로 분리 처리합니다.
                    // ClawGameResultPopupView는 OnQuizFailed 구독으로 실패 팝업을 표시합니다.
                    ChangeState(ClawStateType.ReTakeRequest);
                }
            }
            else
            {
                Debug.LogWarning($"[ClawGameViewModel] 등록되지 않은 캡슐 ID 정답 체킹 시도 감지: {dollId}");
            }
        }

        public void StartMoveLeft()
        {
            if (m_currentState != ClawStateType.Idle && m_currentState != ClawStateType.MovingRight) return;
            ChangeState(ClawStateType.MovingLeft);
            OnMoveRequested?.Invoke(false);
        }

        public void StartMoveRight()
        {
            if (m_currentState != ClawStateType.Idle && m_currentState != ClawStateType.MovingLeft) return;
            ChangeState(ClawStateType.MovingRight);
            OnMoveRequested?.Invoke(true);
        }

        public void StopMove()
        {
            if (m_currentState == ClawStateType.MovingLeft || m_currentState == ClawStateType.MovingRight)
            {
                ChangeState(ClawStateType.Idle);
                OnStopRequested?.Invoke();
            }
        }

        /// <summary>
        /// [기능]: 캐치 시도 하강 명령 개시 (시간 제한이 도는 동안 무제한 시도 가능하도록 하강 제한 차단 해제)
        /// [작성자]: 윤승종
        /// </summary>
        public void DescendClaw()
        {
            // [수정]: 이동 중에도 하강 가능하도록 가드 조건 완화 (View에서 StopMove 선행 호출 보장)
            if (m_currentState != ClawStateType.Idle &&
                m_currentState != ClawStateType.MovingLeft &&
                m_currentState != ClawStateType.MovingRight)
            {
                return;
            }
            
            // [하강 제한 전면 해제]: RemainingPlayCount가 0 이하가 되더라도 120초 제한 시간 내라면 계속 하강을 허용합니다.
            // 플레이 횟수는 0까지만 깎이며 마이너스로 깨져 내려가지 않게 마진 방어합니다.
            if (m_model.RemainingPlayCount > 0)
            {
                m_model.RemainingPlayCount--;
                OnPlayCountChanged?.Invoke(m_model.RemainingPlayCount);
            }

            ChangeState(ClawStateType.Descending);
        }

        /// <summary>
        /// [기능]: 인형 릴리즈(놓기) 및 집게 펴기 처리
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 자동 복귀 후 자동 드랍이 아닌, 사용자가 원하는 시점에 릴리즈 버튼을 눌러 실행하도록 변경
        /// </summary>
        public void DropDoll()
        {
            if (IsClawClosed)
            {
                IsClawClosed = false;
                IsHoldingDoll = false;
                Debug.Log("[ClawGameViewModel] 플레이어 조작에 의한 집게 릴리즈 실행.");
                OnDropRequested?.Invoke();
                
                // 릴리즈 후 다시 캐치(하강) 활성화를 위해 플레이 가능 상태 유지
                if (m_currentState == ClawStateType.Idle)
                {
                    ChangeState(ClawStateType.Idle);
                }
            }
        }

        /// <summary>
        /// [기능]: 물리 조인트가 끊어졌을 때의 상태 동기화 처리
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// </summary>
        public void NotifyJointBroken()
        {
            if (IsHoldingDoll)
            {
                IsHoldingDoll = false;
                Debug.Log("[ClawGameViewModel] 물리 조인트가 파괴되어 인형을 놓쳤습니다.");
                
                // 기존 상승/복귀 애니메이션의 흐름을 방해하지 않고 인형 상태만 리셋합니다.
            }
        }

        // View에서 각 연출(트윈)이 끝났을 때 호출하여 상태 전환
        public void NotifyDescendCompleted()
        {
            if (m_currentState == ClawStateType.ReTakeRequest || m_currentState == ClawStateType.Result) return;
            ChangeState(ClawStateType.Grabbing);
        }

        /// <summary>
        /// [기능]: 집게 닫기 및 인형 획득 결과 통보
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-22
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 인형 획득 여부에 상관없이 집게가 닫혔으므로 IsClawClosed = true 인가
        /// </summary>
        public void NotifyGrabCompleted(bool isGrabbed)
        {
            IsHoldingDoll = isGrabbed;
            IsClawClosed = true;
            if (m_currentState == ClawStateType.ReTakeRequest || m_currentState == ClawStateType.Result) return;
            ChangeState(ClawStateType.Ascending);
        }
/// <summary>
/// [기능]: 상승 완료 처리 및 조작 권한 반환
/// [작성자]: 윤승종
/// [수정 날짜]: 2026-05-24
/// [마지막 수정 작성자]: 윤승종
/// [수정 내용]: 자동 복귀(Returning)를 제거하고 무조건 Idle 상태로 복귀하여 사용자가 직접 릴리즈 위치를 잡을 수 있게 함
/// </summary>
public void NotifyAscendCompleted()
{
    if (m_currentState == ClawStateType.ReTakeRequest || m_currentState == ClawStateType.Result) return;
    // [규칙 변경]: 자동 복귀 없이 제자리에서 정지하여 사용자의 추가 이동 및 릴리즈 입력을 대기함
    ChangeState(ClawStateType.Idle);
}

        /// <summary>
        /// [기능]: 초기 복귀 완료 상태 통보
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-22
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 복귀 완료 시 집게 상태 리셋 (IsClawClosed = false)
        /// </summary>
        public void NotifyReturnCompleted()
        {
            IsClawClosed = false;
            if (m_currentState == ClawStateType.ReTakeRequest || m_currentState == ClawStateType.Result) return;
            ChangeState(ClawStateType.Result);
        }

        public void NotifyResultCompleted()
        {
            ChangeState(ClawStateType.Idle);
        }

        /// <summary>
        /// [기능]: 플레이어가 재수강 요청을 수락했을 때의 비즈니스 로직 처리
        /// [작성자]: 윤승종
        /// </summary>
        public void AcceptReTake()
        {
            if (m_model != null)
            {
                m_model.ReTakeCount++;
                // [버그 수정]: 재수강 성공 시 플레이어에게 다시 5회의 도전 기회를 원복 충전하여 하강 불가 현상을 종결합니다.
                m_model.RemainingPlayCount = 5;
                
                Debug.Log($"[ClawGameViewModel] 플레이어가 재수강을 수락했습니다. 재수강 횟수: {m_model.ReTakeCount}회, 잔여 기회 복원: {m_model.RemainingPlayCount}회, 다음 플레이 제한시간: {m_model.GetTimeLimitForCurrentPlay()}초");
                
                // UI 횟수 동기화 브로드캐스트 트리거
                OnPlayCountChanged?.Invoke(m_model.RemainingPlayCount);
            }

            // 뷰단에 '동의 안 함' 방해 캡슐 1개 파괴 제거 이벤트 전송
            if (OnRemoveDisagreeDollRequested != null)
            {
                OnRemoveDisagreeDollRequested.Invoke();
            }

            // [수정]: 재수강 시에는 패널티가 적용된 새 제한시간으로 타이머를 완전히 리셋해야 하므로,
            // 상태를 직접 Idle로 변경한 뒤 ResetAndStartTimer를 명시적으로 호출합니다.
            // ChangeState를 통하면 ResumeTimer(기존 남은 시간 기반)가 호출되어 의도와 다르게 동작합니다.
            m_currentState = ClawStateType.Idle;
            OnStateChanged?.Invoke(m_currentState);
            ResetAndStartTimer();
        }

        /// <summary>
        /// [기능]: 플레이어가 재수강 요청을 거부(비동의)했을 때 게임 종료 처리
        /// [작성자]: 윤승종
        /// </summary>
        public void RejectReTake()
        {
            Debug.Log("[ClawGameViewModel] 플레이어가 재수강을 거부했습니다. 최종 결과 화면으로 전이합니다.");
            ChangeState(ClawStateType.Result);
        }

        /// <summary>
        /// [기능]: 오답 제출 후 게임판 초기화 없이 그 자리에서 남은 시간 타이머를 재개하여 계속 플레이를 허용합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ContinueAfterWrongAnswer()
        {
            m_currentState = ClawStateType.Idle;
            OnStateChanged?.Invoke(m_currentState);
            ResumeTimer();
            Debug.Log("[ClawGameViewModel] 오답 확인 완료 -> 현재 상태 유지하며 게임을 이어서 진행합니다.");
        }

        /// <summary>
        /// [기능]: 정답 입력 후 다음 단계로 이어서 진행 처리합니다 (인형뽑기는 팝업 버튼 분기로 자동 연동되므로 공백 유지).
        /// [작성자]: 윤승종
        /// </summary>
        public void ContinueAfterCorrectAnswer()
        {
            // 인형뽑기는 다음 단계인 클래식 퀴즈 활성화가 ResultPopupView의 Confirm 클릭 시 자동 처리되므로 빈 본문으로 둡니다.
        }

        public void Dispose()
        {
            StopTimer();
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        private void ChangeState(ClawStateType newState)
        {
            if (m_currentState == newState)
            {
                return;
            }
            
            ClawStateType prevState = m_currentState;
            m_currentState = newState;
            
            bool wasTimerActive = IsTimerActiveState(prevState);
            bool isTimerActive = IsTimerActiveState(newState);

            if (!wasTimerActive && isTimerActive)
            {
                // [버그 수정]: 비활성→활성 전이 시 타이머를 리셋하지 않고, 기존 남은 시간(m_model.RemainingTime)을 기준으로 재개(Resume)합니다.
                ResumeTimer();
            }
            else if (wasTimerActive && !isTimerActive)
            {
                // 활성→비활성 전이 시 타이머를 일시정지(Pause)합니다. 남은 시간은 m_model.RemainingTime에 보존됩니다.
                StopTimer();
            }

            OnStateChanged?.Invoke(m_currentState);
        }

        private bool IsTimerActiveState(ClawStateType state)
        {
            // 타이머는 성공 결과(Result) 및 재수강 요청(ReTakeRequest) 상태를 제외한 모든 인게임 진행 중에 멈추지 않고 흘러갑니다.
            return state != ClawStateType.Result && 
                   state != ClawStateType.ReTakeRequest;
        }

        private void StopTimer()
        {
            if (m_timerCts != null)
            {
                m_timerCts.Cancel();
                m_timerCts.Dispose();
                m_timerCts = null;
            }
        }

        /// <summary>
        /// [기능]: 타이머를 완전히 리셋하고 처음부터 새로 시작합니다. 게임 시작 및 재수강 수락 시에만 호출됩니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void ResetAndStartTimer()
        {
            StopTimer();
            float newLimit = 120f;
            if (m_model != null)
            {
                newLimit = m_model.GetTimeLimitForCurrentPlay();
                m_model.RemainingTime = newLimit;
            }
            m_timerCts = new CancellationTokenSource();
            StartTimerAsync(newLimit, m_timerCts.Token).Forget();
        }

        /// <summary>
        /// [기능]: 일시정지된 타이머를 현재 남은 시간(m_model.RemainingTime) 기준으로 재개합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void ResumeTimer()
        {
            StopTimer();
            float remaining = 120f;
            if (m_model != null)
            {
                remaining = m_model.RemainingTime;
            }

            // 남은 시간이 0 이하라면 즉시 시간 초과 처리
            if (remaining <= 0f)
            {
                Debug.Log("[ClawGameViewModel] 타이머 재개 시도 시 남은 시간이 0초 이하입니다. 즉시 시간 초과 처리합니다.");
                if (m_currentState != ClawStateType.ReTakeRequest)
                {
                    ChangeState(ClawStateType.ReTakeRequest);
                    if (OnReTakeRequested != null)
                    {
                        OnReTakeRequested.Invoke();
                    }
                }
                return;
            }

            m_timerCts = new CancellationTokenSource();
            StartTimerAsync(remaining, m_timerCts.Token).Forget();
        }

        /// <summary>
        /// [기능]: 지정된 남은 시간(remainingSeconds)을 기준으로 실시간 카운트다운을 수행하는 비동기 타이머 코루틴입니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-26
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: elapsed 기반 로직을 remainingSeconds 감산 방식으로 전환하여 일시정지/재개 시 정확한 잔여시간을 보장
        /// </summary>
        private async UniTaskVoid StartTimerAsync(float remainingSeconds, CancellationToken token)
        {
            // 초기 UI 동기화
            if (OnTimeChanged != null)
            {
                OnTimeChanged.Invoke(remainingSeconds);
            }

            while (remainingSeconds > 0f)
            {
                bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
                if (isCanceled) return;

                float dt = Time.deltaTime;
                remainingSeconds -= dt;

                // [시간 누적]: 뽑기 게임 진행 중에 흘러간 시간을 PlayerSO에 실시간 누적합니다.
                if (m_playerSO != null)
                {
                    m_playerSO.TotalMinigamePlayTime += dt;
                }
                
                float timeLeft = Mathf.Max(0f, remainingSeconds);
                if (m_model != null)
                {
                    m_model.RemainingTime = timeLeft;
                }
                
                if (OnTimeChanged != null)
                {
                    OnTimeChanged.Invoke(timeLeft);
                }
            }

            // 시간 초과 시 재수강 창 대기 상태로 전이
            if (IsTimerActiveState(m_currentState))
            {
                Debug.Log("[ClawGameViewModel] 제한 시간이 만료되었습니다. 재수강 요청 상태로 전이합니다.");
                ChangeState(ClawStateType.ReTakeRequest);
                if (OnReTakeRequested != null)
                {
                    OnReTakeRequested.Invoke();
                }
            }
        }
        #endregion
    }
}
