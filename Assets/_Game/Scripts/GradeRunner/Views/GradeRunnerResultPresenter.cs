using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using GameArifiction.Player;
using EasyTransition;
using GameArifiction.UI.Common;

namespace GameArifiction.GradeRunner
{
    /// <summary>
    /// [기능]: GradeRunner 게임 결과를 수신하여 공통 결과 팝업(CommonResultPopupView)에 전달하고 씬 전이를 처리하는 중개 컴포넌트입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class GradeRunnerResultPresenter : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("공통 결과 팝업")]
        [SerializeField]
        [Tooltip("결과를 노출할 공통 결과 팝업 뷰입니다.")]
        private CommonResultPopupView m_commonResultPopup;

        [Header("이지 트랜지션 설정")]
        [SerializeField]
        [Tooltip("로비 복귀 시 씬 화면 전환을 수려하게 연출해 줄 이지 트랜지션 설정 자산입니다.")]
        private TransitionSettings m_transitionSettings;

        [SerializeField]
        [Tooltip("트랜지션 전환 효과가 시작되기까지의 대기 지연시간(초)입니다.")]
        private float m_transitionDelay = 0.1f;

        [Header("세션 데이터")]
        [SerializeField]
        [Tooltip("플레이어의 위치 상태를 로비에서 복원하기 위한 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;

        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;
        private GradeRunnerConfigSO m_config;

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: VContainer를 통해 의존성을 주입합니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public void Construct(GradeRunnerViewModel viewModel, GradeRunnerConfigSO config)
        {
            m_viewModel = viewModel;
            m_config = config;

            if (m_viewModel != null)
            {
                m_viewModel.OnGameResult += HandleGameResult;
            }
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnGameResult -= HandleGameResult;
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

            string titleText = "★ 미니게임 결과 ★";
            string descriptionText = $"최종 학점: {result.FinalGradePoint:F1} / 5.0\n소요 시간: {result.ElapsedTime:F1}초";
            
            CommonPopupDataDTO popupData = new CommonPopupDataDTO(
                titleText,
                descriptionText,
                result.MinigameGrade,
                "메인 화면으로",
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
            Debug.Log("[GradeRunnerResultPresenter] 플레이어가 로비 복귀 나가기 버튼을 선택했습니다.");

            // 로비 씬 복원 활성화 플래그 주입
            if (m_playerSO != null)
            {
                m_playerSO.HasSavedPosition = true;
            }

            // 이지 트랜지션 연출 적용
            if (m_transitionSettings != null)
            {
                TransitionManager manager = FindFirstObjectByType<TransitionManager>();
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
