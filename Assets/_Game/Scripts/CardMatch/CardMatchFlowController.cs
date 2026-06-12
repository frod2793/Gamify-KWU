using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using GameArifiction.Player;
using GameArifiction.UI.Common;

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
        private readonly CommonResultPopupView m_resultPopupView;
        private readonly CardMatchSettingsSO m_settings;
        private readonly PlayerSO m_playerSO;
        private readonly EasyTransition.TransitionSettings m_transitionSettings;

        private CardMatchViewModel m_viewModel;
        #endregion

        #region 생성자 의존성 주입 (Constructor DI)
        /// <summary>
        /// [기능]: VContainer를 통해 의존성을 주입받아 초기화합니다.
        /// [작성자]: 김지연
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: CardMatchResultPopupView에서 CommonResultPopupView로 의존성 주입 변경 및 TransitionSettings 추가
        /// </summary>
        [Inject]
        public CardMatchFlowController(
            CardMatchView gameView,
            CommonResultPopupView resultPopupView,
            CardMatchSettingsSO settings,
            PlayerSO playerSO,
            IObjectResolver resolver)
        {
            m_gameView = gameView;
            m_resultPopupView = resultPopupView;
            m_settings = settings;
            m_playerSO = playerSO;

            // Optional injection for TransitionSettings
            if (resolver.TryResolve<EasyTransition.TransitionSettings>(out var ts))
            {
                m_transitionSettings = ts;
            }
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
                Debug.LogWarning("[CardMatchFlowController] CommonResultPopupView가 하이어라키 내에 탐색되지 않았습니다.");
            }

            // 6. 게임방법 팝업 띄우기
            m_gameView.ShowInGame();
            m_gameView.ShowHowToPlayPopupAtStart(OnGameStartRequested);

            Debug.Log("[CardMatchFlowController] 카드 맞추기 미니게임 초기화 완료. (팝업 표시)");
        }
        #endregion

        #region Private Methods - 게임 흐름 콜백 (Game Flow Callbacks)
        /// <summary>
        /// [기능]: 시작 팝업의 닫기 버튼 클릭 시 호출됩니다.
        ///         팝업이 닫힌 직후 미리보기를 포함한 게임을 개시합니다.
        /// [작성자]: 김지연
        /// </summary>
        private void OnGameStartRequested()
        {
            Debug.Log("[CardMatchFlowController] 게임 시작");

            m_viewModel.StartGame();
        }

        /// <summary>
        /// [기능]: 12쌍 전부 매칭 완료 시 호출됩니다. 공용 결과 팝업을 설정하고 표시합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 전용 팝업에서 공용 결과 팝업(CommonResultPopupView) 적용으로 교체 및 DTO 생성 바인딩
        /// </summary>
        private void OnGameComplete(MinigameGrade grade, string message, int flipCount)
        {
            Debug.Log($"[CardMatchFlowController] 게임 완료. 학점: {grade}, 뒤집기 횟수: {flipCount}");

            if (m_resultPopupView != null)
            {
                string titleText = "게임 결과";
                string descriptionText = $"뒤집기 횟수: {flipCount}회\n\n{message}";
                string lectureName = m_settings != null ? m_settings.LectureName : "게임학의이해";

                CommonPopupDataDTO popupData = new CommonPopupDataDTO(
                    titleText,
                    descriptionText,
                    lectureName,
                    grade,
                    "로비로 이동",
                    func_OnExitConfirm,
                    "CardMatch"
                );

                m_resultPopupView.Setup(popupData);
            }
        }

        /// <summary>
        /// [기능]: 공용 결과 팝업 확인 클릭 시 실행될 콜백으로, 로비 씬 복원 처리를 수행하고 화면을 전환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// </summary>
        private void func_OnExitConfirm()
        {
            Debug.Log("[CardMatchFlowController] 플레이어가 확인 버튼을 클릭하여 로비로 복귀합니다.");

            // 로비 씬 복원 활성화 플래그 주입
            if (m_playerSO != null)
            {
                m_playerSO.HasSavedPosition = true;
            }

            // EasyTransition 적용 검출
            if (m_transitionSettings != null)
            {
                EasyTransition.TransitionManager manager = Object.FindFirstObjectByType<EasyTransition.TransitionManager>();
                if (manager != null)
                {
                    EasyTransition.TransitionManager.Instance().Transition("Lobby", m_transitionSettings, 0.1f);
                    return;
                }
            }

            // 트랜지션 유실 시 일반 씬 매니저 다이렉트 전이 폴백
            SceneManager.LoadScene("Lobby");
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
