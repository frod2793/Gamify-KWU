using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameArifiction.Player;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 미니게임의 핵심 비즈니스 로직을 담당하는 ViewModel 클래스입니다.
    ///         카드 선택 처리, 쌍 판정, 미리보기, 학점 산출 등 모든 게임 규칙을 통제합니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardMatchViewModel : IDisposable
    {
        #region Private Fields
        private readonly CardMatchModel m_model;
        private readonly CardMatchSettingsSO m_settings;
        private readonly PlayerSO m_playerSO;

        private bool m_isPreviewPhase;
        private bool m_isProcessing;
        private bool m_isGameActive;

        private CancellationTokenSource m_cts;
        #endregion

        #region Events
        /// <summary> 카드 뒤집힘 이벤트 (카드 인덱스, 앞면 여부) </summary>
        public event Action<int, bool> OnCardFlipped;

        /// <summary> 매칭 성공 이벤트 (첫 번째 카드 인덱스, 두 번째 카드 인덱스) </summary>
        public event Action<int, int> OnMatchSuccess;

        /// <summary> 매칭 실패 이벤트 (첫 번째 카드 인덱스, 두 번째 카드 인덱스) </summary>
        public event Action<int, int> OnMatchFailed;

        /// <summary> 미리보기 시작: 모든 카드 앞면 공개 </summary>
        public event Action OnAllCardsRevealed;

        /// <summary> 미리보기 종료: 모든 카드 뒷면 전환 </summary>
        public event Action OnAllCardsHidden;

        /// <summary> 뒤집기 횟수 변경 이벤트 </summary>
        public event Action<int> OnFlipCountChanged;

        /// <summary> 맞춘 짝 수 변경 이벤트 </summary>
        public event Action<int> OnMatchedPairsChanged;

        /// <summary> 게임 완료 이벤트 (학점, 멘트, 뒤집기 횟수) </summary>
        public event Action<MinigameGrade, string, int> OnGameComplete;
        #endregion

        #region Properties
        public int FlipCount => m_model.FlipCount;
        public int MatchedPairs => m_model.MatchedPairs;
        public int TotalPairs => m_model.TotalPairs;
        public bool IsGameActive => m_isGameActive;
        public List<CardData> Cards => m_model.Cards;
        #endregion

        #region 생성자 (Constructor)
        /// <summary>
        /// [기능]: ViewModel을 초기화합니다. Model과 설정 SO, PlayerSO를 주입받습니다.
        /// [작성자]: 김지연
        /// </summary>
        public CardMatchViewModel(CardMatchModel model, CardMatchSettingsSO settings, PlayerSO playerSO)
        {
            m_model = model;
            m_settings = settings;
            m_playerSO = playerSO;

            m_isPreviewPhase = false;
            m_isProcessing = false;
            m_isGameActive = false;

            m_cts = new CancellationTokenSource();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// [기능]: 게임을 시작합니다. 미리보기 단계부터 진행됩니다.
        /// [작성자]: 김지연
        /// </summary>
        public void StartGame()
        {
            Debug.Log("[CardMatchViewModel] 카드 맞추기 게임을 시작합니다.");
            StartPreviewAsync(m_cts.Token).Forget();
        }

        /// <summary>
        /// [기능]: 카드를 선택합니다. 홀수번째 선택은 대기, 짝수번째 선택은 비교 판정을 수행합니다.
        /// [작성자]: 김지연
        /// </summary>
        /// <param name="cardIndex">선택된 카드의 인덱스</param>
        public void SelectCard(int cardIndex)
        {
            // 게임이 활성 상태가 아니거나 처리 중이면 무시
            if (!m_isGameActive || m_isProcessing || m_isPreviewPhase)
            {
                return;
            }

            // 유효 범위 검증
            if (cardIndex < 0 || cardIndex >= m_model.Cards.Count)
            {
                Debug.LogWarning($"[CardMatchViewModel] 유효하지 않은 카드 인덱스: {cardIndex}");
                return;
            }

            CardData card = m_model.Cards[cardIndex];

            // 이미 뒤집혔거나 매칭된 카드는 무시
            if (card.IsFlipped || card.IsMatched)
            {
                return;
            }

            // 뒤집기 횟수 증가 (성공/실패 무관)
            m_model.FlipCount++;
            OnFlipCountChanged?.Invoke(m_model.FlipCount);

            // 카드 앞면 공개
            card.IsFlipped = true;
            OnCardFlipped?.Invoke(cardIndex, true);

            if (m_model.FirstSelectedIndex == null)
            {
                // 홀수번째 선택 (첫 번째 카드): 앞면 유지, 다음 선택 대기
                m_model.FirstSelectedIndex = cardIndex;
                Debug.Log($"[CardMatchViewModel] 첫 번째 카드 선택: 인덱스 {cardIndex}, PairId {card.PairId}");
            }
            else
            {
                // 짝수번째 선택 (두 번째 카드): 이전 카드와 비교 판정
                int firstIndex = m_model.FirstSelectedIndex.Value;
                Debug.Log($"[CardMatchViewModel] 두 번째 카드 선택: 인덱스 {cardIndex}, PairId {card.PairId}");
                ProcessMatchAsync(firstIndex, cardIndex, m_cts.Token).Forget();
            }
        }

        /// <summary>
        /// [기능]: ViewModel의 리소스를 정리합니다. CancellationTokenSource를 해제합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void Dispose()
        {
            if (m_cts != null)
            {
                m_cts.Cancel();
                m_cts.Dispose();
                m_cts = null;
            }
        }
        #endregion

        #region Private Methods - 미리보기 (Preview)
        /// <summary>
        /// [기능]: 3초간 모든 카드 앞면을 공개한 뒤 뒷면으로 전환하는 미리보기를 수행합니다.
        /// [작성자]: 김지연
        /// </summary>
        private async UniTaskVoid StartPreviewAsync(CancellationToken ct)
        {
            m_isPreviewPhase = true;
            m_isGameActive = false;

            // 모든 카드 앞면 공개
            for (int i = 0; i < m_model.Cards.Count; i++)
            {
                m_model.Cards[i].IsFlipped = true;
            }
            OnAllCardsRevealed?.Invoke();
            Debug.Log($"[CardMatchViewModel] 미리보기 시작: {m_settings.PreviewDuration}초간 모든 카드 앞면 공개");

            // 미리보기 시간 대기
            await UniTask.Delay(
                TimeSpan.FromSeconds(m_settings.PreviewDuration),
                cancellationToken: ct
            );

            // 모든 카드 뒷면 전환
            for (int i = 0; i < m_model.Cards.Count; i++)
            {
                m_model.Cards[i].IsFlipped = false;
            }
            OnAllCardsHidden?.Invoke();
            Debug.Log("[CardMatchViewModel] 미리보기 종료: 모든 카드 뒷면 전환 완료. 게임 플레이 가능 상태로 전환합니다.");

            m_isPreviewPhase = false;
            m_isGameActive = true;
        }
        #endregion

        #region Private Methods - 매칭 판정 (Match Processing)
        /// <summary>
        /// [기능]: 두 카드의 짝 일치 여부를 판정하고 성공/실패에 따른 후처리를 수행합니다.
        /// [작성자]: 김지연
        /// </summary>
        private async UniTaskVoid ProcessMatchAsync(int firstIndex, int secondIndex, CancellationToken ct)
        {
            m_isProcessing = true;
            m_model.FirstSelectedIndex = null;

            CardData firstCard = m_model.Cards[firstIndex];
            CardData secondCard = m_model.Cards[secondIndex];

            if (firstCard.PairId == secondCard.PairId)
            {
                // ===== 매칭 성공 =====
                firstCard.IsMatched = true;
                secondCard.IsMatched = true;
                m_model.MatchedPairs++;

                Debug.Log($"[CardMatchViewModel] 매칭 성공! PairId: {firstCard.PairId}, 맞춘 짝: {m_model.MatchedPairs}/{m_model.TotalPairs}");
                OnMatchSuccess?.Invoke(firstIndex, secondIndex);
                OnMatchedPairsChanged?.Invoke(m_model.MatchedPairs);

                // 성공 이펙트 표시 대기
                await UniTask.Delay(
                    TimeSpan.FromSeconds(m_settings.MatchSuccessDelay),
                    cancellationToken: ct
                );

                // 게임 완료 확인
                if (m_model.MatchedPairs >= m_model.TotalPairs)
                {
                    CompleteGame();
                }
            }
            else
            {
                // ===== 매칭 실패 =====
                Debug.Log($"[CardMatchViewModel] 매칭 실패. 카드1 PairId: {firstCard.PairId}, 카드2 PairId: {secondCard.PairId}");
                OnMatchFailed?.Invoke(firstIndex, secondIndex);

                // 잠시 보여준 뒤 뒷면으로 복귀
                await UniTask.Delay(
                    TimeSpan.FromSeconds(m_settings.MatchFailDelay),
                    cancellationToken: ct
                );

                firstCard.IsFlipped = false;
                secondCard.IsFlipped = false;
                OnCardFlipped?.Invoke(firstIndex, false);
                OnCardFlipped?.Invoke(secondIndex, false);
            }

            m_isProcessing = false;
        }

        /// <summary>
        /// [기능]: 12쌍 전부 매칭 완료 시 학점을 산출하고 게임 완료 이벤트를 발행합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void CompleteGame()
        {
            m_isGameActive = false;

            var (grade, message) = m_settings.EvaluateGrade(m_model.FlipCount);

            // PlayerSO에 미니게임 결과 저장
            if (m_playerSO != null)
            {
                m_playerSO.SetMinigameGrade("CardMatch", grade);
            }

            Debug.Log($"[CardMatchViewModel] 게임 완료! 뒤집기 횟수: {m_model.FlipCount}, 학점: {grade}, 멘트: {message}");
            OnGameComplete?.Invoke(grade, message, m_model.FlipCount);
        }
        #endregion
    }
}
