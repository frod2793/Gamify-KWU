/// <summary>
/// [기능]: 미니게임 완료 후 로비 복귀 세션 시의 인트로 스킵, 플레이어 활성화 및 좌표 복원 흐름을 검증하는 자동화 플레이모드 테스트 클래스
/// [작성자]: 윤승종
/// </summary>

using NUnit.Framework;
using UnityEngine;
using GameArifiction.Player;
using GamifyKWU.Lobby;
using GamifyKWU.UI.Title;
using GameArifiction.Interaction;

namespace GameArifiction.Tests
{
    [TestFixture]
    public class LobbyFlowIntegrationTests
    {
        private GameObject m_testContainer;
        private PlayerSO m_playerSO;
        private PlayerView m_playerView;
        private TitleView m_titleView;
        private IntroCutsceneController m_introController;
        private UIManager m_uiManager;

        [SetUp]
        public void SetUp()
        {
            m_testContainer = new GameObject("TestContainer");

            // 1. 임시 테스트용 PlayerSO 인스턴스 생성 및 리셋
            m_playerSO = ScriptableObject.CreateInstance<PlayerSO>();
            m_playerSO.ResetData();

            // 2. Mock용 GameObject 구성 및 컴포넌트 추가
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(m_testContainer.transform);
            playerGo.AddComponent<Rigidbody2D>();
            m_playerView = playerGo.AddComponent<PlayerView>();
            m_playerView.Construct(new PlayerViewModelFactory());

            var titleGo = new GameObject("TitleView");
            titleGo.transform.SetParent(m_testContainer.transform);
            m_titleView = titleGo.AddComponent<TitleView>();

            var introGo = new GameObject("IntroCutsceneController");
            introGo.transform.SetParent(m_testContainer.transform);
            m_introController = introGo.AddComponent<IntroCutsceneController>();

            var uiGo = new GameObject("UIManager");
            uiGo.transform.SetParent(m_testContainer.transform);
            m_uiManager = uiGo.AddComponent<UIManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_testContainer != null)
            {
                Object.DestroyImmediate(m_testContainer);
            }
            if (m_playerSO != null)
            {
                Object.DestroyImmediate(m_playerSO);
            }
        }

        /// <summary>
        /// [기능]: 미니게임 복귀 데이터(HasSavedPosition = true)가 유효할 때, 로비 진입 시 타이틀이 꺼지고 플레이어 복귀 시도가 연쇄 트리거되는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-27
        /// </summary>
        [Test]
        public void Test_DetermineSessionState_WhenReturningFromMinigame_DisablesTitle()
        {
            // Arrange (복귀 상태 세팅)
            Vector2 mockReturnPosition = new Vector2(10f, -5f);
            m_playerSO.LastPosition = mockReturnPosition;
            m_playerSO.HasSavedPosition = true;
            m_playerSO.IsReturnedFromMinigame = true;

            // Mock View들의 활성 상태 초기화
            m_titleView.gameObject.SetActive(true);
            m_introController.Construct(null, m_playerSO, m_playerView);

            // LobbyFlowController 수동 생성 (테스트 환경이므로 미사용 뷰모델 등은 null 주입)
            var controller = new LobbyFlowController(
                m_playerSO,
                m_uiManager,
                m_titleView,
                m_introController,
                m_playerView,
                null,
                null,
                null
            );

            // Act
            controller.Start();

            // Assert
            Assert.IsFalse(m_titleView.gameObject.activeSelf, "[LobbyFlowTests] 복귀 세션임에도 타이틀 뷰가 꺼지지 않고 활성화되어 있습니다.");
            Assert.IsTrue(m_playerSO.IsIntroPlayed, "[LobbyFlowTests] 복귀 세션 진입 후 IsIntroPlayed 플래그가 자동 승인되지 않았습니다.");
        }

        /// <summary>
        /// [기능]: 디버그 옵션(m_forcePlayIntro = true)이 켜져 있고 복귀 상태가 아닐 때(IsReturnedFromMinigame = false),
        ///         인트로 컷씬 연출이 생략되지 않고 강제로 재생 모드로 이행하는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-27
        /// </summary>
        [Test]
        public void Test_StartIntroCutscene_WhenForcePlayIntroIsTrue_StartsIntroCutscene()
        {
            // Arrange
            m_playerSO.IsReturnedFromMinigame = false;
            m_playerSO.IsIntroPlayed = true;

            m_introController.Construct(null, m_playerSO, m_playerView);

            var forcePlayIntroField = typeof(IntroCutsceneController).GetField("m_forcePlayIntro", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            forcePlayIntroField.SetValue(m_introController, true);

            // Act
            m_introController.StartIntroCutscene();

            // Assert
            var isIntroRunningField = typeof(IntroCutsceneController).GetField("m_isIntroRunning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isRunning = (bool)isIntroRunningField.GetValue(m_introController);

            Assert.IsTrue(isRunning, "[LobbyFlowTests] 디버그 강제 인트로 옵션이 활성화되었음에도 인트로가 재생되지 않고 생략되었습니다.");
        }

        /// <summary>
        /// [기능]: 미니게임 클리어 후 로비로 씬 복귀(IsReturnedFromMinigame = true) 시,
        ///         인트로 컷씬 연출이 안전하게 스킵되며 플레이어가 최종 저장 위치(LastPosition)로 텔레포트 이동하는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-27
        /// </summary>
        [Test]
        public void Test_StartIntroCutscene_WhenReturnedFromMinigame_SkipsIntroAndTeleportsToLastPosition()
        {
            // Arrange
            Vector2 mockReturnPosition = new Vector2(50f, -80f);
            m_playerSO.LastPosition = mockReturnPosition;
            m_playerSO.HasSavedPosition = true;
            m_playerSO.IsReturnedFromMinigame = true;

            m_introController.Construct(null, m_playerSO, m_playerView);
            m_playerView.SendMessage("Start");

            var forcePlayIntroField = typeof(IntroCutsceneController).GetField("m_forcePlayIntro", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            forcePlayIntroField.SetValue(m_introController, true);

            // Act
            m_introController.StartIntroCutscene();

            // Assert
            var isIntroRunningField = typeof(IntroCutsceneController).GetField("m_isIntroRunning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isRunning = (bool)isIntroRunningField.GetValue(m_introController);
            Assert.IsFalse(isRunning, "[LobbyFlowTests] 미니게임 복귀 세션임에도 인트로가 스킵되지 않고 실행 상태로 이행했습니다.");

            Assert.AreEqual(mockReturnPosition, (Vector2)m_playerView.transform.position, "[LobbyFlowTests] 복귀 플레이어 텔레포트 스폰 위치가 올바르게 복원되지 않았습니다.");
            Assert.IsFalse(m_playerSO.HasSavedPosition, "[LobbyFlowTests] 텔레포트 복원 완료 후 HasSavedPosition 플래그가 해제되지 않았습니다.");
            Assert.IsFalse(m_playerSO.IsReturnedFromMinigame, "[LobbyFlowTests] 텔레포트 복원 완료 후 IsReturnedFromMinigame 플래그가 해제되지 않았습니다.");
        }

        /// <summary>
        /// [기능]: 미니게임 복귀 후 인트로 스킵 절차가 완료되었을 때, 
        ///         플레이어의 입력 잠금 해제 및 오브젝트 활성화 상태가 정상 보존되는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-27
        /// </summary>
        [Test]
        public void Test_StartIntroCutscene_WhenReturnedFromMinigame_UnlocksPlayerInputAndKeepsObjectActive()
        {
            // Arrange
            m_playerSO.LastPosition = new Vector2(1f, 1f);
            m_playerSO.HasSavedPosition = true;
            m_playerSO.IsReturnedFromMinigame = true;

            m_introController.Construct(null, m_playerSO, m_playerView);
            m_playerView.SendMessage("Start");

            PlayerViewModel playerVM = m_playerView.GetViewModel();
            Assert.IsNotNull(playerVM, "[LobbyFlowTests] 테스트용 PlayerViewModel이 생성되지 않았습니다.");
            playerVM.SetInputLocked(true);

            // Act
            m_introController.StartIntroCutscene();

            // Assert
            // 1. 캐릭터 오브젝트 자체가 활성화(Active) 상태로 보존되는지 검사
            Assert.IsTrue(m_playerView.gameObject.activeSelf, "[LobbyFlowTests] 복귀 완료 후 플레이어 캐릭터 오브젝트가 활성화 상태로 유지되지 못하고 비활성화되어 있습니다.");

            // 2. 조작 잠금이 해제(false)되었는지 검사
            Assert.IsFalse(playerVM.IsInputLocked, "[LobbyFlowTests] 복귀 및 인트로 스킵 처리 완료 후 플레이어 조작 입력 잠금이 해제되지 않았습니다.");
        }
    }
}
