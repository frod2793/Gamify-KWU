using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using GameArifiction.Player;
using GameArifiction.Core.Audio;
using GameArifiction.UI.Common;
using VContainer;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 인게임 화면의 전체 UI를 관리하는 View 클래스입니다.
    ///         카드 그리드 생성, 뒤집기 횟수/맞춘 짝 수 표시, ViewModel 이벤트 구독 등을 담당합니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardMatchView : MonoBehaviour
    {
        #region SerializeField
        [Header("인게임 UI 요소")]
        [SerializeField] private GameObject m_inGamePanel;
        [SerializeField] private TextMeshProUGUI m_flipCountText;
        [SerializeField] private TextMeshProUGUI m_matchedPairsText;

        [Header("카드 그리드 설정")]
        [SerializeField] private Transform m_cardGridParent;
        [SerializeField] private CardView m_cardPrefab;

        [Header("카드 로고 스프라이트 (12종, PairId 순서대로 할당)")]
        [SerializeField] private Sprite[] m_logoSprites;

        [Header("일시정지 및 게임방법 팝업")]
        [SerializeField] private Button m_pauseButton;
        [SerializeField] private GameObject m_howToPlayPopup;
        [SerializeField] private Button m_closePopupButton;

        [Header("설정 UI 요소")]
        [SerializeField] private Button m_settingsButton;
        #endregion

        #region Private Fields
        private CardMatchViewModel m_viewModel;
        private List<CardView> m_cardViews;
        private bool m_isPaused;
        private System.Action m_onStartPopupClosed;

        // 사운드 관련 필드
        private ISoundService m_soundService;
        private CommonSettingsPopupView m_settingsPopupView;
        #endregion

        #region 의존성 주입 (Dependency Injection)
        /// <summary>
        /// [기능]: VContainer를 통해 전역 사운드 서비스 및 설정 팝업 뷰를 주입받습니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 신규 의존성 주입 설정 및 설정 팝업 뷰 주입 추가
        /// </summary>
        [Inject]
        public void Construct(ISoundService soundService, CommonSettingsPopupView settingsPopupView)
        {
            m_soundService = soundService;
            m_settingsPopupView = settingsPopupView;
        }
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            if (m_pauseButton != null)
            {
                m_pauseButton.onClick.AddListener(func_OnPauseButtonClick);
            }
            if (m_closePopupButton != null)
            {
                m_closePopupButton.onClick.AddListener(func_OnClosePopupButtonClick);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (m_pauseButton != null)
            {
                m_pauseButton.onClick.RemoveListener(func_OnPauseButtonClick);
            }
            if (m_closePopupButton != null)
            {
                m_closePopupButton.onClick.RemoveListener(func_OnClosePopupButtonClick);
            }
            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.RemoveListener(func_OnSettingsButtonClick);
            }
            if (m_settingsPopupView != null)
            {
                m_settingsPopupView.OnClosePopup -= func_OnSettingsClose;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// [기능]: 인게임 뷰를 초기화합니다. ViewModel 이벤트를 구독하고 카드 그리드를 생성합니다.
        /// [작성자]: 김지연
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 공통 설정 팝업 버튼 및 이벤트 리스너 연동 추가
        /// </summary>
        /// <param name="viewModel">카드 맞추기 ViewModel</param>
        public void Initialize(CardMatchViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_cardViews = new List<CardView>();

            SubscribeEvents();
            SpawnCards();
            UpdateFlipCountUI(0);
            UpdateMatchedPairsUI(0);

            if (m_inGamePanel != null)
            {
                m_inGamePanel.SetActive(false);
            }

            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.AddListener(func_OnSettingsButtonClick);
            }

            if (m_settingsPopupView != null)
            {
                m_settingsPopupView.OnClosePopup += func_OnSettingsClose;
                m_settingsPopupView.gameObject.SetActive(false);
            }

            Debug.Log("[CardMatchView] 인게임 뷰 초기화 완료");
        }

        /// <summary>
        /// [기능]: 인게임 패널을 표시하고 게임 BGM을 재생합니다.
        /// [작성자]: 김지연
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 로컬 오디오 소스 대신 전역 ISoundService를 활용하도록 수정
        /// </summary>
        public void ShowInGame()
        {
            if (m_inGamePanel != null)
            {
                m_inGamePanel.SetActive(true);
            }

            if (m_soundService != null)
            {
                m_soundService.PlayBGM(SoundDefine.Bgm_cardmatch);
            }
        }

        /// <summary>
        /// [기능]: 인게임 패널을 숨기고 재생 중이던 BGM을 정지합니다.
        /// [작성자]: 김지연
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: BGM 정지 로직 연동
        /// </summary>
        public void HideInGame()
        {
            if (m_inGamePanel != null)
            {
                m_inGamePanel.SetActive(false);
            }

            if (m_soundService != null)
            {
                m_soundService.StopBGM();
            }
        }

        /// <summary>
        /// [기능]: 게임 시작 시 게임방법 팝업을 띄웁니다.
        /// [작성자]: 김지연
        /// </summary>
        public void ShowHowToPlayPopupAtStart(System.Action onPopupClosed)
        {
            m_onStartPopupClosed = onPopupClosed;
            
            m_isPaused = true;

            if (m_howToPlayPopup != null)
            {
                CanvasGroup canvasGroup = m_howToPlayPopup.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = m_howToPlayPopup.AddComponent<CanvasGroup>();
                }

                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
                m_howToPlayPopup.SetActive(true);
            }
            else
            {
                m_onStartPopupClosed?.Invoke();
                m_onStartPopupClosed = null;
                Time.timeScale = 1f;
                m_isPaused = false;
            }
        }
        #endregion

        #region Private Methods - 이벤트 구독 관리 (Event Subscription)
        private void SubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnCardFlipped += HandleCardFlipped;
                m_viewModel.OnMatchSuccess += HandleMatchSuccess;
                m_viewModel.OnMatchFailed += HandleMatchFailed;
                m_viewModel.OnAllCardsRevealed += HandleAllCardsRevealed;
                m_viewModel.OnAllCardsHidden += HandleAllCardsHidden;
                m_viewModel.OnFlipCountChanged += UpdateFlipCountUI;
                m_viewModel.OnMatchedPairsChanged += UpdateMatchedPairsUI;
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnCardFlipped -= HandleCardFlipped;
                m_viewModel.OnMatchSuccess -= HandleMatchSuccess;
                m_viewModel.OnMatchFailed -= HandleMatchFailed;
                m_viewModel.OnAllCardsRevealed -= HandleAllCardsRevealed;
                m_viewModel.OnAllCardsHidden -= HandleAllCardsHidden;
                m_viewModel.OnFlipCountChanged -= UpdateFlipCountUI;
                m_viewModel.OnMatchedPairsChanged -= UpdateMatchedPairsUI;
            }
        }
        #endregion

        #region Private Methods - 카드 생성 (Card Spawning)
        /// <summary>
        /// [기능]: ViewModel의 카드 데이터를 기반으로 CardView 프리팹을 인스턴스화하여 그리드에 배치합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void SpawnCards()
        {
            if (m_cardPrefab == null || m_cardGridParent == null)
            {
                Debug.LogError("[CardMatchView] 카드 프리팹 또는 그리드 부모 오브젝트가 할당되지 않았습니다.");
                return;
            }

            List<CardData> cards = m_viewModel.Cards;

            for (int i = 0; i < cards.Count; i++)
            {
                CardView cardView = Instantiate(m_cardPrefab, m_cardGridParent);

                int pairId = cards[i].PairId;
                Sprite logoSprite = null;

                if (m_logoSprites != null && pairId >= 0 && pairId < m_logoSprites.Length)
                {
                    logoSprite = m_logoSprites[pairId];
                }

                cardView.Initialize(i, logoSprite, OnCardClicked);
                m_cardViews.Add(cardView);
            }

            Debug.Log($"[CardMatchView] 카드 {cards.Count}장 생성 완료");
        }

        /// <summary>
        /// [기능]: CardView에서 전달된 카드 클릭을 ViewModel로 라우팅합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void OnCardClicked(int cardIndex)
        {
            if (m_viewModel != null)
            {
                m_viewModel.SelectCard(cardIndex);
            }
        }
        #endregion

        #region Private Methods - 이벤트 핸들러 (Event Handlers)
        /// <summary>
        /// [기능]: 카드 뒤집힘 이벤트를 처리하여 해당 CardView의 뒤집기 연출을 실행합니다.
        /// [작성자]: 김지연
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 전역 ISoundService를 활용한 카드 뒤집기 SFX 재생 연동
        /// </summary>
        private void HandleCardFlipped(int cardIndex, bool isFaceUp)
        {
            if (cardIndex < 0 || cardIndex >= m_cardViews.Count)
            {
                return;
            }

            if (isFaceUp)
            {
                m_cardViews[cardIndex].FlipToFront();
                
                // 카드 뒤집는 사운드 재생
                if (m_soundService != null)
                {
                    m_soundService.PlaySFX(SoundDefine.Sfx_cardmatch_flip);
                }
            }
            else
            {
                m_cardViews[cardIndex].FlipToBack();
            }
        }

        /// <summary>
        /// [기능]: 매칭 성공 시 두 카드에 성공 이펙트를 재생합니다.
        /// [작성자]: 김지연
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 전역 ISoundService를 활용한 매칭 성공 SFX 재생 연동
        /// </summary>
        private void HandleMatchSuccess(int firstIndex, int secondIndex)
        {
            Debug.Log($"[CardMatchView] 매칭 성공 연출: 카드 {firstIndex}, {secondIndex}");

            // 두 번째 카드의 뒤집기 애니메이션(0.3초)이 끝난 직후 성공 이펙트가 재생되도록 딜레이 적용
            DOVirtual.DelayedCall(0.3f, () => 
            {
                // 매칭 성공 사운드 재생
                if (m_soundService != null)
                {
                    m_soundService.PlaySFX(SoundDefine.Sfx_cardmatch_correct);
                }

                if (firstIndex >= 0 && firstIndex < m_cardViews.Count)
                {
                    m_cardViews[firstIndex].PlayMatchSuccessEffect();
                }
                if (secondIndex >= 0 && secondIndex < m_cardViews.Count)
                {
                    m_cardViews[secondIndex].PlayMatchSuccessEffect();
                }
            });
        }

        /// <summary>
        /// [기능]: 매칭 실패 시 호출됩니다. 뒷면 복귀는 ViewModel의 OnCardFlipped 이벤트에서 처리합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void HandleMatchFailed(int firstIndex, int secondIndex)
        {
            Debug.Log($"[CardMatchView] 매칭 실패 연출: 카드 {firstIndex}, {secondIndex}");
            // 뒷면 복귀는 MatchFailDelay 후 ViewModel이 OnCardFlipped(index, false) 발행
        }

        /// <summary>
        /// [기능]: 미리보기 시작 시 모든 카드를 즉시 앞면으로 표시합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void HandleAllCardsRevealed()
        {
            Debug.Log("[CardMatchView] 미리보기: 모든 카드 앞면 공개");
            for (int i = 0; i < m_cardViews.Count; i++)
            {
                m_cardViews[i].FlipToFront();
            }
        }

        /// <summary>
        /// [기능]: 미리보기 종료 시 모든 카드를 뒤집기 애니메이션과 함께 뒷면으로 전환합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void HandleAllCardsHidden()
        {
            Debug.Log("[CardMatchView] 미리보기 종료: 모든 카드 뒷면 전환");
            for (int i = 0; i < m_cardViews.Count; i++)
            {
                m_cardViews[i].FlipToBack();
            }
        }
        #endregion

        #region Private Methods - UI 갱신 (UI Update)
        /// <summary>
        /// [기능]: 뒤집기 횟수 텍스트를 갱신합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void UpdateFlipCountUI(int flipCount)
        {
            if (m_flipCountText != null)
            {
                m_flipCountText.text = flipCount.ToString();
            }
        }

        /// <summary>
        /// [기능]: 맞춘 짝 수 텍스트를 갱신합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void UpdateMatchedPairsUI(int matchedPairs)
        {
            if (m_matchedPairsText != null)
            {
                m_matchedPairsText.text = $"{matchedPairs} / {m_viewModel.TotalPairs}";
            }
        }
        #endregion

        #region UI Event Callbacks
        /// <summary>
        /// [기능]: [일시정지] 버튼 클릭 시 호출됩니다. 게임을 일시정지하고 게임방법 팝업을 표시합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void func_OnPauseButtonClick()
        {
            Debug.Log("[CardMatchView] 일시정지 버튼 클릭");

            if (m_isPaused)
            {
                return;
            }

            m_isPaused = true;
            Time.timeScale = 0f;

            if (m_howToPlayPopup != null)
            {
                CanvasGroup canvasGroup = m_howToPlayPopup.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = m_howToPlayPopup.AddComponent<CanvasGroup>();
                }

                canvasGroup.alpha = 0f;
                m_howToPlayPopup.SetActive(true);
                canvasGroup.DOFade(1f, 0.4f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// [기능]: 게임방법 팝업의 닫기 버튼 클릭 시 호출됩니다. 팝업을 닫고 게임을 재개합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void func_OnClosePopupButtonClick()
        {
            Debug.Log("[CardMatchView] 게임방법 팝업 닫기 클릭");

            if (m_howToPlayPopup != null)
            {
                CanvasGroup canvasGroup = m_howToPlayPopup.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0f, 0.35f)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true)
                        .OnComplete(() =>
                        {
                            m_howToPlayPopup.SetActive(false);

                            if (m_onStartPopupClosed != null)
                            {
                                m_isPaused = false;
                                DOVirtual.DelayedCall(0.2f, () => 
                                {
                                    m_onStartPopupClosed.Invoke();
                                    m_onStartPopupClosed = null;
                                });
                            }
                            else
                            {
                                Time.timeScale = 1f;
                                m_isPaused = false;
                            }
                        });
                }
            }
        }

        /// <summary>
        /// [기능]: [설정] 버튼 클릭 시 호출됩니다. 게임을 일시정지하고 설정 팝업을 표시합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 신규 구현
        /// </summary>
        public void func_OnSettingsButtonClick()
        {
            Debug.Log("[CardMatchView] 설정 버튼 클릭");

            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_cardmatch_flip);
            }

            if (m_settingsPopupView != null)
            {
                // [부모 레이어 렌더 가드 최적화]: 비활성 상태인 최상위 부모 노드 단 1개만 활성화하여 렉 방지
                Transform parentNode = m_settingsPopupView.transform.parent;
                Transform deepestInactiveParent = null;

                while (parentNode != null)
                {
                    if (!parentNode.gameObject.activeSelf)
                    {
                        deepestInactiveParent = parentNode;
                    }
                    parentNode = parentNode.parent;
                }

                if (deepestInactiveParent != null)
                {
                    deepestInactiveParent.gameObject.SetActive(true);
                    Debug.Log($"[CardMatchView] 가장 상위의 비활성 부모 UI 오브젝트를 활성화하여 계층 구조를 켰습니다: {deepestInactiveParent.name}");
                }

                m_settingsPopupView.ShowPopup();

                // 팝업이 활성화된 즉시 UI 레이아웃과 캔버스 버퍼의 강제 업데이트 동기화 수행
                Canvas.ForceUpdateCanvases();

                Debug.Log("[CardMatchView] 게임을 일시정지하고 공통 설정 팝업을 즉시 동기화하여 활성화했습니다.");
            }
            else
            {
                Debug.LogWarning("[CardMatchView] CommonSettingsPopupView 의존성이 주입되지 않아 설정 팝업을 표시할 수 없습니다.");
            }

            // 타임 스케일을 최종 정지
            Time.timeScale = 0f;
        }

        /// <summary>
        /// [기능]: 설정 팝업이 닫힐 때 호출되며, 게임 일시정지를 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 신규 구현
        /// </summary>
        private void func_OnSettingsClose()
        {
            Time.timeScale = 1f;
            Debug.Log("[CardMatchView] 공통 설정 팝업이 닫혀 게임 일시정지를 해제했습니다.");
        }
        #endregion
    }
}
