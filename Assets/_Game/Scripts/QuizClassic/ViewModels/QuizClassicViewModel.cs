using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameArifiction.Player;
using UnityEngine;
using GamifyKWU.CraneGame.Data;

namespace GameArifiction.QuizClassic
{
    /// <summary>
    /// 클래식 4지선다 퀴즈 게임의 비즈니스 논리적 흐름과 타이머 루프를 통제하는 뷰모델 클래스 (POCO)
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-06
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 타이머 미사용 요구 사항에 맞추어 시간 초과에 따른 실패 분기를 제거하고 단순 경과 누적으로 변경함
    /// </summary>
    public class QuizClassicViewModel : GameArifiction.ClawMachine.IQuizGameViewModel, IDisposable
    {
        #region 내부 필드 (Private Fields)

        private readonly QuizClassicModel m_model;
        private readonly PlayerSO m_playerSO;
        private QuizStateType m_currentState;
        private CancellationTokenSource m_timerCts;
        private CancellationTokenSource m_nextQuizCts;
        private int m_reTakeCount; // 리플레이 재수강 시도 횟수 트래킹용

        // [신규]: 퀴즈 정답 추적 및 캡슐 퀴즈 데이터 매핑 딕셔너리
        private readonly List<string> m_currentChoiceTexts = new List<string>(4);
        private QuizData m_currentQuiz;

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        public event Action<QuizStateType> OnStateChanged;
        public event Action<QuizData, List<string>> OnNextQuizLoaded; // 퀴즈 및 셔플된 4지선다 목록
        public event Action<int> OnScoreChanged;

        public event Action OnQuizSuccess;
        public event Action OnQuizFailed;
        public event Action<int> OnWrongAnswerSelected; // [신규]: 잘못 선택된 선택지 인덱스 브로드캐스트
        public event Action OnReTakeRequested = delegate { }; // IQuizGameViewModel 상속 호환용

        #endregion

        #region 프로퍼티 (Properties)

        public QuizStateType CurrentState => m_currentState;
        public QuizData CurrentQuiz => m_currentQuiz;
        public List<string> CurrentChoiceTexts => m_currentChoiceTexts;
        public int Score => m_model.Score;
        public int ReTakeCount => m_reTakeCount;
        public bool IsLastQuiz => m_model != null && m_model.CurrentQuizIndex >= m_model.QuizList.Count - 1;
        public float TimeLeft
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.RemainingTime;
                }
                return 30f;
            }
        }

        /// <summary>
        /// [기능]: 클래식 퀴즈 뷰모델에서는 성적 학점을 개별 사용하지 않으므로 호환성 규격만 충족합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public MinigameGrade QuizGrade => MinigameGrade.F;

        /// <summary>
        /// [기능]: 인형뽑기부터 퀴즈까지 누적된 미니게임 총 소요 시간(초)
        /// [작성자]: 윤승종
        /// </summary>
        public float TotalPlayTime => m_playerSO != null ? m_playerSO.TotalMinigamePlayTime : 0f;

        #endregion


        #region 초기화 (Initialization)

        public QuizClassicViewModel(QuizClassicModel model, PlayerSO playerSO)
        {
            m_model = model;
            m_playerSO = playerSO;
            ChangeState(QuizStateType.Idle);
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 미니게임을 정식 개시하고 첫 퀴즈를 출제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 게임 시작 시 지연 로딩 태스크(CancellationToken)를 초기화하도록 보완
        /// </summary>
        public void StartGame()
        {
            StopNextQuizDeferred();
            if (m_model.QuizList.Count == 0)
            {
                Debug.LogWarning("[QuizClassicViewModel] 출제할 퀴즈 목록이 비어있습니다.");
                return;
            }

            m_model.CurrentQuizIndex = 0;
            m_model.Score = 0;
            OnScoreChanged?.Invoke(m_model.Score);

            LoadCurrentQuiz();
        }

        /// <summary>
        /// [기능]: 사용자가 4지선다 중 하나를 클릭했을 때 호출되어 채점을 돌리고 다음 흐름을 연동합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_SelectAnswer(int choiceIndex)
        {
            if (m_currentState != QuizStateType.Playing)
            {
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= m_currentChoiceTexts.Count)
            {
                return;
            }

            string selectedText = m_currentChoiceTexts[choiceIndex];
            bool isCorrect = (selectedText == m_currentQuiz.CorrectAnswer);

            if (isCorrect)
            {
                // 정답 점수 가산
                m_model.Score += 100;
                OnScoreChanged?.Invoke(m_model.Score);
                
                Debug.Log($"[QuizClassicViewModel] 정답 골인! 현재 점수: {m_model.Score}");
                
                // 마지막 문제 클리어 여부 확인
                if (m_model.CurrentQuizIndex >= m_model.QuizList.Count - 1)
                {
                    OnQuizSuccess?.Invoke();
                    ChangeState(QuizStateType.Result);
                }
                else
                {
                    // 중간 문제인 경우 타이머만 중지하고 정답 이벤트 전송 (팝업이 다음 문제 로드 대기)
                    StopTimer();
                    OnQuizSuccess?.Invoke();
                }
            }
            else
            {
                Debug.Log($"[QuizClassicViewModel] 오답 선택됨! 선택지 인덱스: {choiceIndex}");
                OnWrongAnswerSelected?.Invoke(choiceIndex);
                OnQuizFailed?.Invoke();
                // [기획 연동]: 틀렸을 시 ReTakeRequest로 전이하지 않고, 최종 퀴즈 화면을 유지하며 정답이 아닌 보기가 비활성화되도록 함
            }
        }

        /// <summary>
        /// [기능]: 실패 후 재수강(리플레이) 수락 시 호출되어 상태를 복구하고 게임을 리플레이합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void AcceptReTake()
        {
            m_reTakeCount++;
            Debug.Log($"[QuizClassicViewModel] 플레이어가 재수강을 수락하여 클래식 퀴즈 리플레이를 개시합니다. 시도 횟수: {m_reTakeCount}회");
            StartGame();
        }

        /// <summary>
        /// [기능]: 재수강을 거부(종료)했을 때의 마무리 처리
        /// [작성자]: 윤승종
        /// </summary>
        public void RejectReTake()
        {
            Debug.Log("[QuizClassicViewModel] 플레이어가 재수강을 거부하여 결과 종료 처리합니다.");
            ChangeState(QuizStateType.Result);
        }

        /// <summary>
        /// [기능]: 오답 제출 후 문제 전환이나 초기화 없이 남은 시간 타이머를 재개하여 계속해서 정답을 고를 수 있도록 합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ContinueAfterWrongAnswer()
        {
            m_currentState = QuizStateType.Playing;
            OnStateChanged?.Invoke(m_currentState);
            
            // 타이머 재개 (Resume)
            StopTimer();
            float remaining = m_model.RemainingTime;
            if (remaining <= 0f)
            {
                remaining = 1f; // 안전 마진
            }
            m_timerCts = new CancellationTokenSource();
            StartTimerAsync(m_timerCts.Token).Forget();
            
            Debug.Log("[QuizClassicViewModel] 오답 확인 완료 -> 현재 문제를 이어서 진행합니다.");
        }

        /// <summary>
        /// [기능]: 중간 퀴즈 정답 팝업 확인 후 다음 문제를 출제하고 타이머를 재개합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ContinueAfterCorrectAnswer()
        {
            m_model.CurrentQuizIndex++;
            LoadCurrentQuiz();
        }


        /// <summary>
        /// [기능]: 객체 해제 시 타이머 및 지연 퀴즈 출제 비동기 태스크를 안전하게 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 지연 퀴즈 출제 태스크 취소를 위한 StopNextQuizDeferred 추가
        /// </summary>
        public void Dispose()
        {
            StopTimer();
            StopNextQuizDeferred();
        }

        /// <summary>
        /// [기능]: 미니게임 최종 학점 성적을 데이터 모델(PlayerSO)에 기록합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 성적 저장 미니게임 ID를 "ClawMachineQuiz"에서 "ClawMachine"으로 변경
        /// </summary>
        public void SaveFinalGrade(MinigameGrade grade)
        {
            // [수정]: 클래식 퀴즈의 성적은 무시하고 오직 뽑기 게임(ClawMachine) 결과만 최종 성적으로 사용합니다.
            Debug.Log($"[QuizClassicViewModel] 미니게임 최종 학점 저장을 스킵합니다. (오직 뽑기 게임 결과만 반영됨)");
        }

        /// <summary>
        /// [기능]: 인형뽑기(CraneGame) 단계에서 최종 기록된 성적 등급을 조회합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// </summary>
        public MinigameGrade GetClawGameGrade()
        {
            if (m_playerSO != null)
            {
                return m_playerSO.GetMinigameGrade("CraneGame");
            }
            return MinigameGrade.None;
        }

        /// <summary>
        /// [기능]: 메인 로비로 복귀할 때 플레이어의 좌표 보존 플래그를 세팅합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// </summary>
        public void SaveExitLobbyPosition()
        {
            if (m_playerSO != null)
            {
                m_playerSO.HasSavedPosition = true;
                m_playerSO.IsReturnedFromMinigame = true;
                Debug.Log($"[QuizClassicViewModel] 로비 복귀 상태 세팅 완료 (HasSavedPosition = true). 복귀 좌표: {m_playerSO.LastPosition}");
            }
        }

        #endregion

        #region 내부 메서드 (Private Methods)

        private void LoadCurrentQuiz()
        {
            if (m_model.CurrentQuizIndex < 0 || m_model.CurrentQuizIndex >= m_model.QuizList.Count)
            {
                return;
            }

            m_currentQuiz = m_model.QuizList[m_model.CurrentQuizIndex];

            // 4지선다 리스트 생성 (정답 1개 + 오답 3개)
            m_currentChoiceTexts.Clear();
            m_currentChoiceTexts.Add(m_currentQuiz.CorrectAnswer);

            int wrongCount = Mathf.Min(m_currentQuiz.WrongAnswers.Count, 3);
            for (int i = 0; i < wrongCount; i++)
            {
                m_currentChoiceTexts.Add(m_currentQuiz.WrongAnswers[i]);
            }

            // 피셔-예이츠 객관식 셔플링 연산
            for (int i = m_currentChoiceTexts.Count - 1; i > 0; i--)
            {
                int r = UnityEngine.Random.Range(0, i + 1);
                string temp = m_currentChoiceTexts[i];
                m_currentChoiceTexts[i] = m_currentChoiceTexts[r];
                m_currentChoiceTexts[r] = temp;
            }

            OnNextQuizLoaded?.Invoke(m_currentQuiz, m_currentChoiceTexts);
            ChangeState(QuizStateType.Playing);
        }

        /// <summary>
        /// [기능]: 정답 처리 후 연출 마진을 준 뒤 다음 퀴즈를 지연 출제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 유령 타이머 방지를 위해 CancellationToken을 연동한 안전 비동기 대기 구현
        /// </summary>
        private async UniTaskVoid LoadNextQuizDeferred()
        {
            StopNextQuizDeferred();
            m_nextQuizCts = new CancellationTokenSource();
            CancellationToken token = m_nextQuizCts.Token;

            // 정답 애니메이션이 화면에 출력되는 짧은 안착 마진 대기 (1.2초)
            bool isCanceled = await UniTask.Delay(1200, cancellationToken: token).SuppressCancellationThrow();
            if (isCanceled || token.IsCancellationRequested)
            {
                return;
            }
            
            m_model.CurrentQuizIndex++;
            LoadCurrentQuiz();
        }

        /// <summary>
        /// [기능]: 지연 퀴즈 출제용 비동기 태스크를 취소하고 CTS를 정리합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void StopNextQuizDeferred()
        {
            if (m_nextQuizCts != null)
            {
                m_nextQuizCts.Cancel();
                m_nextQuizCts.Dispose();
                m_nextQuizCts = null;
            }
        }

        private void ChangeState(QuizStateType newState)
        {
            if (m_currentState == newState)
            {
                return;
            }

            QuizStateType prevState = m_currentState;
            m_currentState = newState;

            bool wasPlaying = prevState == QuizStateType.Playing;
            bool isPlaying = newState == QuizStateType.Playing;

            if (!wasPlaying && isPlaying)
            {
                ResetAndStartTimer();
            }
            else if (wasPlaying && !isPlaying)
            {
                StopTimer();
            }

            OnStateChanged?.Invoke(m_currentState);
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
            m_model.RemainingTime = m_model.TimeLimitPerQuestion;
            m_timerCts = new CancellationTokenSource();
            StartTimerAsync(m_timerCts.Token).Forget();
        }

        /// <summary>
        /// [기능]: 제한 시간 초과에 따른 실패 판정 없이, 플레이어가 문제를 푸는 동안 실시간으로 소요 시간만 누적하는 비동기 루프입니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-06
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 제한 시간 초과 실패 전이 로직 제거 및 순수 소요 시간 누적으로 전환 (타이머 미사용 적용)
        /// </summary>
        private async UniTaskVoid StartTimerAsync(CancellationToken token)
        {
            while (true)
            {
                bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
                if (isCanceled || token.IsCancellationRequested)
                {
                    return;
                }

                float dt = Time.deltaTime;

                // [시간 누적]: 클래식 퀴즈 풀이 중 흘러간 시간을 PlayerSO에 실시간 누적합니다.
                if (m_playerSO != null)
                {
                    m_playerSO.TotalMinigamePlayTime += dt;
                }
            }
        }

        #endregion
    }

    #region 퀴즈 게임 상태 타입 구조 (Enum)

    public enum QuizStateType
    {
        Idle,
        Playing,
        Result,
        ReTakeRequest
    }

    #endregion
}
