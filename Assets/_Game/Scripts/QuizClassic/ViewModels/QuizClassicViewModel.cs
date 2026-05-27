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
        public event Action<float> OnTimeChanged;
        public event Action<int> OnScoreChanged;

        public event Action OnQuizSuccess;
        public event Action OnQuizFailed;
        public event Action OnTimeOver;
        public event Action OnReTakeRequested; // IQuizGameViewModel 상속 호환용

        #endregion

        #region 프로퍼티 (Properties)

        public QuizStateType CurrentState => m_currentState;
        public QuizData CurrentQuiz => m_currentQuiz;
        public List<string> CurrentChoiceTexts => m_currentChoiceTexts;
        public int Score => m_model.Score;
        public int ReTakeCount => m_reTakeCount;
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
                OnQuizSuccess?.Invoke();
                
                // 마지막 문제 클리어 여부 확인
                if (m_model.CurrentQuizIndex >= m_model.QuizList.Count - 1)
                {
                    ChangeState(QuizStateType.Result);
                }
                else
                {
                    // 다음 문제로 지연 로딩을 위해 타이머만 명시적으로 중지 (상태는 Playing 유지)
                    StopTimer();
                    LoadNextQuizDeferred().Forget();
                }
            }
            else
            {
                Debug.Log("[QuizClassicViewModel] 오답입니다! 최종 실패 결과 패널을 트리거합니다.");
                OnQuizFailed?.Invoke();
                ChangeState(QuizStateType.ReTakeRequest);
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

        private async UniTaskVoid StartTimerAsync(CancellationToken token)
        {
            float limit = m_model.TimeLimitPerQuestion;
            float remainingSeconds = limit;

            OnTimeChanged?.Invoke(remainingSeconds);

            while (remainingSeconds > 0f)
            {
                bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
                if (isCanceled || token.IsCancellationRequested)
                {
                    return;
                }

                float dt = Time.deltaTime;
                remainingSeconds -= dt;

                // [시간 누적]: 클래식 퀴즈 풀이 중에도 흘러간 시간을 PlayerSO에 실시간 누적합니다.
                if (m_playerSO != null)
                {
                    m_playerSO.TotalMinigamePlayTime += dt;
                }

                float timeLeft = Mathf.Max(0f, remainingSeconds);
                m_model.RemainingTime = timeLeft;
                OnTimeChanged?.Invoke(timeLeft);
            }

            // [추가 가드]: 루프 완료 시점에 이미 취소된 좀비 토큰 세션이라면 즉시 기각 차단
            if (token.IsCancellationRequested)
            {
                return;
            }

            // 제한 시간 만료 시 실패(ReTakeRequest) 상태 전이
            if (m_currentState == QuizStateType.Playing)
            {
                Debug.Log("[QuizClassicViewModel] 제한 시간이 초과되어 퀴즈 최종 실패 판정 및 재수강 요청을 트리거합니다.");
                OnTimeOver?.Invoke();
                OnReTakeRequested?.Invoke(); // [연동 추가]: 결과 패널 성공/실패 감지 트리거
                ChangeState(QuizStateType.ReTakeRequest);
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
