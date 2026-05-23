using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 게임의 View와 Model을 연결하는 ViewModel
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-22
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 집게가 오므려져 있는 상태(IsClawClosed) 관리 추가 및 스페이스바 입력 조건 고도화
    /// </summary>
    public class ClawGameViewModel : IDisposable
    {
        #region 내부 필드 (Private Fields)
        private readonly ClawMachineModel m_model;
        private ClawStateType m_currentState;
        private CancellationTokenSource m_timerCts;
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        public event Action<ClawStateType> OnStateChanged;
        public event Action<int> OnPlayCountChanged;
        public event Action<float> OnTimeChanged;
        
        // 이동 관련 브로드캐스트 (View에서 구독하여 이동 처리)
        public event Action<bool> OnMoveRequested; // true: Right, false: Left
        public event Action OnStopRequested;
        public event Action OnDropRequested; // 도중 강제 놓기 이벤트
        #endregion

        #region 속성 (Properties)
        public ClawStateType CurrentState => m_currentState;
        public bool IsHoldingDoll { get; private set; }
        public bool IsClawClosed { get; private set; }
        #endregion

        #region 초기화 (Initialization)
        public ClawGameViewModel(ClawMachineModel model)
        {
            m_model = model;
            ChangeState(ClawStateType.Idle);
        }
        #endregion

        #region 공개 메서드 (Public Methods)
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

        public void DescendClaw()
        {
            // [수정]: 이동 중 하강 시 맵 이탈 방지를 위해 오직 Idle(정지) 상태에서만 하강 허용
            if (m_currentState != ClawStateType.Idle)
            {
                return;
            }
            
            // 횟수 차감
            if (m_model.RemainingPlayCount <= 0)
            {
                return;
            }
            
            m_model.RemainingPlayCount--;
            OnPlayCountChanged?.Invoke(m_model.RemainingPlayCount);

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
            ChangeState(ClawStateType.Result);
        }

        public void NotifyResultCompleted()
        {
            ChangeState(ClawStateType.Idle);
        }

        public void Dispose()
        {
            StopTimer();
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        private void ChangeState(ClawStateType newState)
        {
            if (m_currentState == newState) return;
            
            ClawStateType prevState = m_currentState;
            m_currentState = newState;
            
            bool wasPlayable = IsPlayableState(prevState);
            bool isPlayable = IsPlayableState(newState);

            if (!wasPlayable && isPlayable)
            {
                ResetAndStartTimer();
            }
            else if (wasPlayable && !isPlayable)
            {
                StopTimer();
            }

            OnStateChanged?.Invoke(m_currentState);
        }

        private bool IsPlayableState(ClawStateType state)
        {
            return state == ClawStateType.Idle || 
                   state == ClawStateType.MovingLeft || 
                   state == ClawStateType.MovingRight;
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

        private void ResetAndStartTimer()
        {
            StopTimer();
            if (m_model != null)
            {
                m_model.RemainingTime = m_model.TimeLimitPerPlay;
            }
            m_timerCts = new CancellationTokenSource();
            StartTimerAsync(m_timerCts.Token).Forget();
        }

        private async UniTaskVoid StartTimerAsync(CancellationToken token)
        {
            float limit = m_model != null ? m_model.TimeLimitPerPlay : 30f;
            float elapsed = 0f;

            OnTimeChanged?.Invoke(limit);

            while (elapsed < limit)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
                
                float timeLeft = Mathf.Max(0f, limit - elapsed);
                if (m_model != null)
                {
                    m_model.RemainingTime = timeLeft;
                }
                OnTimeChanged?.Invoke(timeLeft);
            }

            // 시간 초과 시 자동 하강
            if (IsPlayableState(m_currentState))
            {
                DescendClaw();
            }
        }
        #endregion
    }
}
