using UnityEngine;
using GameArifiction.Core.Audio;
using VContainer;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 게임 진입 시 플레이 조작법을 설명하는 튜토리얼 팝업 UI View
    ///         (에디터 인스펙터 Button OnClick 이벤트를 통해 직접 func_OnStartButtonClick 메서드를 연결하여 작동하도록 설계됨)
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-06
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: ISoundService를 주입받아 시작 버튼 클릭 시 터치음 연동 및 네임스페이스 정리
    /// </summary>
    public class ClawGameTutorialPopupView : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private ISoundService m_soundService;
        #endregion

        #region 의존성 주입 (Dependency Injection)
        /// <summary>
        /// [기능]: VContainer를 통해 공통 사운드 서비스를 주입받습니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public void Construct(ISoundService soundService)
        {
            m_soundService = soundService;
        }
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: 뷰모델을 주입받고 초기 상태를 설정합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 게임 진입 시 튜토리얼 팝업 자동 활성화
            func_ShowTutorial();
        }
        #endregion

        #region UI 이벤트 콜백 (Public Methods)
        /// <summary>
        /// [기능]: 시작하기 버튼 클릭 시 뷰모델에 튜토리얼 종료를 알리고 팝업을 닫습니다.
        ///         (유니티 인스펙터 버튼 OnClick 이벤트에 직접 연결하여 사용하는 콜백 메서드)
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnStartButtonClick()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }

            if (m_viewModel != null)
            {
                Debug.Log("[ClawGameTutorialPopupView] 플레이어가 시작하기 버튼을 클릭하여 튜토리얼을 종료합니다.");
                m_viewModel.func_CompleteTutorial();
            }

            func_HideTutorial();
        }
        #endregion

        #region 내부 연출 및 상태 제어 (Public Methods)
        /// <summary>
        /// [기능]: 튜토리얼 팝업을 화면에 표시합니다. (다시보기 등 외부 제어를 위해 public 선언)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-06
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 외부 연동을 위해 private에서 public으로 가시성 수준을 확장함
        /// </summary>
        public void func_ShowTutorial()
        {
            gameObject.SetActive(true);
            Debug.Log("[ClawGameTutorialPopupView] 튜토리얼 팝업이 활성화되었습니다.");
        }

        /// <summary>
        /// [기능]: 튜토리얼 팝업을 화면에서 감춥니다. (다시보기 등 외부 제어를 위해 public 선언)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-06
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 외부 연동을 위해 private에서 public으로 가시성 수준을 확장함
        /// </summary>
        public void func_HideTutorial()
        {
            gameObject.SetActive(false);
            Debug.Log("[ClawGameTutorialPopupView] 튜토리얼 팝업이 비활성화되었습니다.");
        }
        #endregion
    }
}
