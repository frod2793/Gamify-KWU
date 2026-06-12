using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using GameArifiction.Player;
using EasyTransition;
using GameArifiction.UI.Common;

namespace GameArifiction.GradeRunner
{
    /// <summary>
    /// [기능]: GradeRunner 게임 결과를 수신하여 공통 결과 팝업(CommonResultPopupView)에 전달하고 씬 전이를 처리하는 순수 C# 중개 프레젠터 클래스입니다.
    ///         씬 하이어라키 배치가 필요 없도록 VContainer EntryPoint(IStartable) 모델로 설계되었습니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-08
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 불필요한 단순 확인용 디버그 로그(Debug.Log) 제거/주석화 및 마감 처리
    /// </summary>
    public class GradeRunnerResultPresenter : IStartable, IDisposable
    {
        #region 내부 의존 필드 (Private Fields)

        private readonly CommonResultPopupView m_commonResultPopup;
        private readonly TransitionSettings m_transitionSettings;
        private readonly PlayerSO m_playerSO;
        private readonly GradeRunnerViewModel m_viewModel;
        private readonly GradeRunnerConfigSO m_config;
        private readonly float m_transitionDelay = 0.1f;

        #endregion

        #region 초기화 및 생성자 (Constructor & Initialization)

        /// <summary>
        /// [기능]: 생성자를 통해 VContainer 컨테이너로부터 의존 객체들을 자동으로 주입받습니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public GradeRunnerResultPresenter(
            CommonResultPopupView commonResultPopup,
            PlayerSO playerSO,
            GradeRunnerViewModel viewModel,
            GradeRunnerConfigSO config,
            TransitionSettings transitionSettings = null)
        {
            m_commonResultPopup = commonResultPopup;
            m_transitionSettings = transitionSettings;
            m_playerSO = playerSO;
            m_viewModel = viewModel;
            m_config = config;
        }

        /// <summary>
        /// [기능]: VContainer EntryPoint 진입 시점에 결과 팝업을 비활성화하고 뷰모델의 결과 이벤트를 구독 바인딩합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Start()
        {
            if (m_commonResultPopup != null)
            {
                m_commonResultPopup.gameObject.SetActive(false);
            }

            if (m_viewModel != null)
            {
                m_viewModel.OnGameResult += HandleGameResult;
                // Debug.Log("[GradeRunnerResultPresenter] 게임 결과 이벤트 구독 처리를 완료했습니다 (POCO 기동).");
            }
        }

        /// <summary>
        /// [기능]: 객체 소멸 시 이벤트를 안전하게 해제합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Dispose()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnGameResult -= HandleGameResult;
                // Debug.Log("[GradeRunnerResultPresenter] 게임 결과 이벤트 구독 해제를 완료했습니다.");
            }
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        /// <summary>
        /// [기능]: 뷰모델에서 게임 결과를 전달받았을 때 DTO를 구성하여 공통 결과 팝업에 주입 및 활성화시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleGameResult(GradeRunnerResultDTO result)
        {
            if (result == null || m_commonResultPopup == null)
            {
                return;
            }

            string titleText = "게임 결과";
            
            CommonPopupDataDTO popupData = new CommonPopupDataDTO(
                titleText,
                $"현재 스코어: {result.FinalGradePoint:F1}",
                "게임엔진S/W",
                result.MinigameGrade,
                "로비로 이동",
                func_OnExitConfirm
            );

            m_commonResultPopup.Setup(popupData);
        }

        #endregion

        #region 내부 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 공통 팝업 확인 버튼 클릭 시 실행될 콜백으로, 로비 씬 복원 처리를 수행하고 화면을 전환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void func_OnExitConfirm()
        {
            // Debug.Log("[GradeRunnerResultPresenter] 플레이어가 로비 복귀 나가기 버튼을 선택했습니다.");

            // 로비 씬 복원 활성화 플래그 주입
            if (m_playerSO != null)
            {
                m_playerSO.HasSavedPosition = true;
            }

            // 이지 트랜지션 연출 적용
            if (m_transitionSettings != null)
            {
                TransitionManager manager = UnityEngine.Object.FindFirstObjectByType<TransitionManager>();
                if (manager != null)
                {
                    TransitionManager.Instance().Transition("Lobby", m_transitionSettings, m_transitionDelay);
                    return;
                }
            }

            // 트랜지션 유실 시 일반 씬 매니저 다이렉트 전이 폴백
            SceneManager.LoadScene("Lobby");
        }

        #endregion
    }
}
