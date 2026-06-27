using UnityEngine;
using VContainer;
using VContainer.Unity;
using GamifyKWU.UI.Title;
using GameArifiction.Player;
using GameArifiction.Interaction;
using System;
using GameArifiction.Core.Audio;
using GameArifiction.UI.FinalResult; // 신규 추가

/// <summary>
/// [기능]: 로비 씬의 진입점 역할을 수행하며, 세션 상태를 판단하고 UI 패널 및 인트로 컷씬 개시를 관리하는 흐름 제어 클래스
/// [작성자]: 윤승종
/// </summary>
namespace GamifyKWU.Lobby
{
    public class LobbyFlowController : IStartable, IDisposable
    {
        #region 내부 필드 (Private Fields)

        private readonly PlayerSO m_playerSO;
        private readonly UIManager m_uiManager;
        private readonly TitleView m_titleView;
        private readonly IntroCutsceneController m_introController;
        private readonly PlayerView m_playerView;
        private readonly ISoundService m_soundService;

        // [신규]: 최종 성적 및 엔딩 뷰모델 필드
        private readonly FinalResultViewModel m_finalResultViewModel;
        private readonly GameEndingViewModel m_gameEndingViewModel;

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
            PlayerView playerView,
            ISoundService soundService,
            FinalResultViewModel finalResultViewModel,
            GameEndingViewModel gameEndingViewModel)
        {
            m_playerSO = playerSO;
            m_uiManager = uiManager;
            m_titleView = titleView;
            m_introController = introController;
            m_playerView = playerView;
            m_soundService = soundService;
            m_finalResultViewModel = finalResultViewModel;
            m_gameEndingViewModel = gameEndingViewModel;
        }

        #endregion

        #region 인터페이스 구현 (IStartable, IDisposable)

        /// <summary>
        /// [기능]: VContainer 컨테이너 빌드가 완료된 직후 실행되는 시작 진입점 메서드
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 결과 확인 시 엔딩 요청 이벤트 구독 연동
        /// </summary>
        public void Start()
        {
            Debug.Log("[LobbyFlowController] 로비 흐름 제어 프로세스를 개시합니다.");

            if (m_soundService != null)
            {
                m_soundService.PlayBGMWithFade(SoundDefine.Lobby_Bgm, 1f);
            }

            DetermineSessionState();

            if (m_finalResultViewModel != null)
            {
                m_finalResultViewModel.OnEndingRequested += func_OnEndingRequested;
            }
        }

        /// <summary>
        /// [기능]: 컨트롤러가 파괴되거나 씬 전환 시 호출되어 리소스를 해제하고 이벤트를 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 엔딩 요청 이벤트 안전 구독 해제
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[LobbyFlowController] 로비 흐름 제어 프로세스를 종료하고 리소스를 해제합니다.");

            if (m_soundService != null)
            {
                m_soundService.StopBGMWithFade(1f);
            }

            if (m_finalResultViewModel != null)
            {
                m_finalResultViewModel.OnEndingRequested -= func_OnEndingRequested;
            }
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

                // 2. 인트로 컨트롤러에게 미니게임 복귀에 따른 연출 바이패스 및 활성화 요청
                if (m_introController != null)
                {
                    m_introController.StartIntroCutscene();
                }
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

        /// <summary>
        /// [기능]: 최종 결과 팝업 확인 완료 수신 시 로비 BGM을 페이드아웃하고 엔딩 시퀀스를 실행합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// </summary>
        private void func_OnEndingRequested()
        {
            Debug.Log("[LobbyFlowController] 최종 결과 확인 이벤트를 감지했습니다. BGM 페이드아웃 및 엔딩 시퀀스를 트리거합니다.");

            if (m_soundService != null)
            {
                m_soundService.StopBGMWithFade(1.5f);
            }

            if (m_gameEndingViewModel != null)
            {
                m_gameEndingViewModel.StartEndingCommand();
            }
        }

        #endregion
    }
}
