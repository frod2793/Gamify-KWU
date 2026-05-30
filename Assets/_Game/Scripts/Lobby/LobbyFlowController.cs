using UnityEngine;
using VContainer;
using VContainer.Unity;
using GamifyKWU.UI.Title;
using GameArifiction.Player;
using GameArifiction.Interaction;

/// <summary>
/// [기능]: 로비 씬의 진입점 역할을 수행하며, 세션 상태를 판단하고 UI 패널 및 인트로 컷씬 개시를 관리하는 흐름 제어 클래스
/// [작성자]: 윤승종
/// </summary>
namespace GamifyKWU.Lobby
{
    public class LobbyFlowController : IStartable
    {
        #region 내부 필드 (Private Fields)

        private readonly PlayerSO m_playerSO;
        private readonly UIManager m_uiManager;
        private readonly TitleView m_titleView;
        private readonly IntroCutsceneController m_introController;
        private readonly PlayerView m_playerView;

        #endregion

        #region 초기화 (Constructor)

        /// <summary>
        /// [기능]: VContainer로부터 필요한 의존성 주입을 받아 컨트롤러를 생성합니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public LobbyFlowController(
            PlayerSO playerSO,
            UIManager uiManager,
            TitleView titleView,
            IntroCutsceneController introController,
            PlayerView playerView)
        {
            m_playerSO = playerSO;
            m_uiManager = uiManager;
            m_titleView = titleView;
            m_introController = introController;
            m_playerView = playerView;
        }

        #endregion

        #region 인터페이스 구현 (IStartable)

        /// <summary>
        /// [기능]: VContainer 컨테이너 빌드가 완료된 직후 실행되는 시작 진입점 메서드
        /// [작성자]: 윤승종
        /// </summary>
        public void Start()
        {
            Debug.Log("[LobbyFlowController] 로비 흐름 제어 프로세스를 개시합니다.");
            DetermineSessionState();
        }

        #endregion

        #region 내부 로직 (Private Methods)

        /// <summary>
        /// [기능]: PlayerSO의 세션 데이터를 확인하여 미니게임 복귀인지 최초 타이틀 진입인지 판별하고 씬의 초기 상태를 설정합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void DetermineSessionState()
        {
            if (m_playerSO != null && m_playerSO.HasSavedPosition)
            {
                Debug.Log("[LobbyFlowController] 미니게임 클리어 후 로비로 복귀한 상태가 감지되었습니다. 타이틀을 건너뛰고 인게임 자유 모드로 진입합니다.");

                // 1. 타이틀 비활성화
                if (m_titleView != null)
                {
                    m_titleView.gameObject.SetActive(false);
                }

                // 2. 상호작용 UI 활성화
                if (m_uiManager != null)
                {
                    m_uiManager.SetInteractionUIActive(true);
                }

                // 3. 인트로 연출은 실행하지 않음
                m_playerSO.IsIntroPlayed = true;

                // 4. 플레이어 입력 락 해제
                SetPlayerInputLocked(false);
            }
            else
            {
                Debug.Log("[LobbyFlowController] 최초 진입 세션으로 감지되었습니다. 타이틀 패널을 활성화하고 흐름 대기 상태로 이행합니다.");

                // 1. 타이틀 패널 활성화
                if (m_titleView != null)
                {
                    m_titleView.gameObject.SetActive(true);
                }

                // 2. 상호작용 UI 비활성화 (타이틀/인트로 도중에는 가려야 함)
                if (m_uiManager != null)
                {
                    m_uiManager.SetInteractionUIActive(false);
                }

                // 3. 플레이어 조작 잠금
                SetPlayerInputLocked(true);
            }
        }

        /// <summary>
        /// [기능]: 플레이어 캐릭터의 조작 입력 잠금 상태를 변경합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void SetPlayerInputLocked(bool isLocked)
        {
            if (m_playerView != null)
            {
                // 리플렉션 없이 안전하게 획득하기 위해 PlayerView에 추가할 공개 Get/Set 메서드를 활용할 예정
                PlayerViewModel playerVM = m_playerView.GetViewModel();
                if (playerVM != null)
                {
                    playerVM.SetInputLocked(isLocked);
                    Debug.Log($"[LobbyFlowController] 플레이어 입력 잠금 상태를 {isLocked}(으)로 싱크 세팅했습니다.");
                }
            }
        }

        #endregion
    }
}
