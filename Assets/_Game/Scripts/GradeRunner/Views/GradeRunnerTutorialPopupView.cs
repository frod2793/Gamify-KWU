using UnityEngine;
using GameArifiction.Core.Audio;
using VContainer;

namespace GameArifiction.GradeRunner
{
    /// <summary>
    /// [기능]: GradeRunner 게임 진입 시 플레이 조작법을 설명하는 튜토리얼 팝업 UI View입니다.
    ///         VContainer를 통해 의존성을 주입받으며, 시작 버튼을 클릭하여 인게임 컷씬으로 넘어갑니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class GradeRunnerTutorialPopupView : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;
        private ISoundService m_soundService;

        #endregion

        #region 의존성 주입 (Dependency Injection)

        /// <summary>
        /// [기능]: VContainer를 통해 전역 사운드 서비스 및 뷰모델 의존성을 주입받습니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public void Construct(GradeRunnerViewModel viewModel, ISoundService soundService)
        {
            m_viewModel = viewModel;
            m_soundService = soundService;

            // 게임 진입 시 튜토리얼 팝업 자동 활성화 및 초기화
            Initialize();
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: 팝업이 활성화될 때 조작 튜토리얼을 화면에 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void Initialize()
        {
            func_ShowTutorial();
        }

        #endregion

        #region UI 이벤트 콜백 (Public Methods)

        /// <summary>
        /// [기능]: 시작하기 버튼 클릭 시 뷰모델에 튜토리얼 완료를 알리고 팝업을 닫습니다.
        ///         (유니티 인스펙터 버튼 OnClick 이벤트에 직접 연결하여 사용하는 콜백 메서드)
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnStartButtonClick()
        {
            if (m_soundService != null)
            {
                // 터치 효과음 재생
                m_soundService.PlaySFX(SoundDefine.Sfx_claw_touch);
            }

            if (m_viewModel != null)
            {
                Debug.Log("[GradeRunnerTutorialPopupView] 플레이어가 시작 버튼을 클릭하여 튜토리얼을 마칩니다.");
                m_viewModel.func_CompleteTutorial();
            }

            func_HideTutorial();
        }

        #endregion

        #region 내부 연출 및 상태 제어 (Public Methods)

        /// <summary>
        /// [기능]: 튜토리얼 팝업을 화면에 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_ShowTutorial()
        {
            gameObject.SetActive(true);
            Debug.Log("[GradeRunnerTutorialPopupView] 튜토리얼 팝업 활성화.");
        }

        /// <summary>
        /// [기능]: 튜토리얼 팝업을 화면에서 감춥니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_HideTutorial()
        {
            gameObject.SetActive(false);
            Debug.Log("[GradeRunnerTutorialPopupView] 튜토리얼 팝업 비활성화.");
        }

        #endregion
    }
}
