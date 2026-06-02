using UnityEngine;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using GameArifiction.Player;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 미니게임 씬의 초기화 및 흐름을 제어하는 순수 C# EntryPoint 클래스입니다.
    ///         VContainer의 IStartable을 구현하여 DI 빌드 완료 후 자동 실행되며,
    ///         Model → ViewModel → View의 MVVM 조립을 수행합니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardMatchFlowController : IStartable
    {
        #region Private Fields (내부 의존성 필드)
        private readonly CardMatchView m_gameView;
        private readonly CardMatchTitleView m_titleView;
        private readonly CardMatchResultPopupView m_resultPopupView;
        private readonly CardMatchSettingsSO m_settings;
        private readonly PlayerSO m_playerSO;

        private CardMatchViewModel m_viewModel;
        #endregion

        #region 생성자 의존성 주입 (Constructor DI)
        /// <summary>
        /// [기능]: VContainer를 통해 의존성을 주입받아 초기화합니다.
        /// [작성자]: 김지연
        /// </summary>
        [Inject]
        public CardMatchFlowController(
            CardMatchView gameView,
            CardMatchTitleView titleView,
            CardMatchResultPopupView resultPopupView,
            CardMatchSettingsSO settings,
            PlayerSO playerSO)
        {
            m_gameView = gameView;
            m_titleView = titleView;
            m_resultPopupView = resultPopupView;
            m_settings = settings;
            m_playerSO = playerSO;
        }
        #endregion

        #region 진입점 인터페이스 구현 (IStartable)
        /// <summary>
        /// [기능]: VContainer 빌드가 완료된 직후 실행되어, MVVM 조립 및 게임 흐름을 개시합니다.
        /// [작성자]: 김지연
        /// </summary>
        public void Start()
        {
            Debug.Log("[CardMatchFlowController] 카드 맞추기 미니게임 흐름 제어를 개시합니다.");
            InitializeGame();
        }
        #endregion

        #region Private Methods - 초기화 (Initialization)
        /// <summary>
        /// [기능]: 카드 데이터를 생성하고 MVVM 단방향 의존성 주입 조립을 완료한 뒤 타이틀 화면을 표시합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void InitializeGame()
        {
            // 1. 카드 데이터 생성 및 셔플 (12쌍 = 24장)
            List<CardData> cards = CreateShuffledCards(m_settings.TotalPairs);

            // 2. Model 생성 (POCO)
            CardMatchModel model = new CardMatchModel(cards, m_settings.TotalPairs);

            // 3. ViewModel 생성 (POCO)
            m_viewModel = new CardMatchViewModel(model, m_settings, m_playerSO);

            // 4. 인게임 View 초기화 (카드 그리드 생성 + 이벤트 구독)
            if (m_gameView != null)
            {
                m_gameView.Initialize(m_viewModel);
            }
            else
            {
                Debug.LogError("[CardMatchFlowController] CardMatchView가 하이어라키 내에 탐색되지 않았습니다. 바인딩 조립이 불가능합니다.");
                return;
            }

            // 5. 결과 팝업 이벤트 연결
            if (m_resultPopupView != null)
            {
                m_viewModel.OnGameComplete += OnGameComplete;
            }
            else
            {
                Debug.LogWarning("[CardMatchFlowController] CardMatchResultPopupView가 하이어라키 내에 탐색되지 않았습니다.");
            }

            // 6. 타이틀 화면 초기화 및 표시
            if (m_titleView != null)
            {
                m_titleView.Initialize(OnGameStartRequested);
            }
            else
            {
                // 타이틀 뷰가 없으면 바로 게임 시작
                Debug.LogWarning("[CardMatchFlowController] CardMatchTitleView가 없어 바로 게임을 시작합니다.");
                OnGameStartRequested();
            }

            Debug.Log("[CardMatchFlowController] 카드 맞추기 미니게임 초기화 완료.");
        }
        #endregion

        #region Private Methods - 게임 흐름 콜백 (Game Flow Callbacks)
        /// <summary>
        /// [기능]: 타이틀 화면에서 [게임 시작] 버튼 클릭 시 호출됩니다.
        ///         인게임 화면을 표시하고 미리보기를 포함한 게임을 개시합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void OnGameStartRequested()
        {
            Debug.Log("[CardMatchFlowController] 게임 시작 요청됨. 인게임 화면으로 전환합니다.");

            if (m_gameView != null)
            {
                m_gameView.ShowInGame();
            }

            m_viewModel.StartGame();
        }

        /// <summary>
        /// [기능]: 12쌍 전부 매칭 완료 시 호출됩니다. 결과 팝업을 표시합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void OnGameComplete(MinigameGrade grade, string message, int flipCount)
        {
            Debug.Log($"[CardMatchFlowController] 게임 완료. 학점: {grade}, 뒤집기 횟수: {flipCount}");

            if (m_resultPopupView != null)
            {
                m_resultPopupView.Show(grade, message, flipCount);
            }
        }
        #endregion

        #region Private Methods - 카드 생성 (Card Generation)
        /// <summary>
        /// [기능]: 지정된 쌍 수만큼 카드 데이터를 생성하고 Fisher-Yates 셔플을 적용합니다.
        /// [작성자]: 김지연
        /// </summary>
        /// <param name="totalPairs">총 카드 쌍 수</param>
        /// <returns>셔플된 카드 데이터 목록</returns>
        private List<CardData> CreateShuffledCards(int totalPairs)
        {
            List<CardData> cards = new List<CardData>();

            // 각 쌍에 대해 2장의 카드 생성
            for (int pairId = 0; pairId < totalPairs; pairId++)
            {
                cards.Add(new CardData(pairId * 2, pairId));
                cards.Add(new CardData(pairId * 2 + 1, pairId));
            }

            // Fisher-Yates 셔플 알고리즘
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                CardData temp = cards[i];
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }

            Debug.Log($"[CardMatchFlowController] 카드 {cards.Count}장 생성 및 셔플 완료 (총 {totalPairs}쌍)");
            return cards;
        }
        #endregion
    }
}
