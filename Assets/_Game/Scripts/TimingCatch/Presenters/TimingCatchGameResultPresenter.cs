using System;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using EasyTransition;
using GameArifiction.Player;
using GameArifiction.UI.Common;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 게임의 최종 점수 결과를 공통 팝업으로 전달하고 로비 전환을 처리합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchGameResultPresenter : IStartable, IDisposable
    {
        #region 내부 필드 (Private Fields)
        private readonly TimingCatchGameViewModel m_viewModel;
        private readonly CommonResultPopupView m_commonResultPopup;
        private readonly PlayerSO m_playerSO;
        private readonly TransitionSettings m_transitionSettings;
        #endregion

        #region 생성자 (Constructor)
        [Inject]
        public TimingCatchGameResultPresenter(
            TimingCatchGameViewModel viewModel,
            CommonResultPopupView commonResultPopup,
            PlayerSO playerSO,
            TransitionSettings transitionSettings = null)
        {
            m_viewModel = viewModel;
            m_commonResultPopup = commonResultPopup;
            m_playerSO = playerSO;
            m_transitionSettings = transitionSettings;
        }
        #endregion

        #region IStartable
        /// <summary>
        /// [기능]: 결과 이벤트 구독 등록과 팝업 초기 상태 셋업을 수행합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
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
            }
        }
        #endregion

        #region IDisposable
        /// <summary>
        /// [기능]: 이벤트 구독을 안전하게 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        public void Dispose()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnGameResult -= HandleGameResult;
            }
        }
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        private void HandleGameResult(TimingCatchGameResultDTO result)
        {
            if (m_commonResultPopup == null || result == null)
            {
                return;
            }

            CommonPopupDataDTO popupData = new CommonPopupDataDTO(
                "게임 결과",
                $"총점: {result.TotalScore} / {result.MaxPossibleScore}\n" +
                $"Great: {result.GreatCount}, Miss: {result.MissCount}\n" +
                $"등급: {result.MinigameGrade}",
                "발표 과목",
                result.MinigameGrade,
                "로비로 이동",
                func_OnExitLobby,
                result.MinigameId
            );

            m_commonResultPopup.Setup(popupData);
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        private void func_OnExitLobby()
        {
            if (m_playerSO != null)
            {
                m_playerSO.HasSavedPosition = true;
                m_playerSO.IsReturnedFromMinigame = true;
            }

            if (m_transitionSettings != null)
            {
                TransitionManager manager = UnityEngine.Object.FindFirstObjectByType<TransitionManager>();
                if (manager != null)
                {
                    TransitionManager.Instance().Transition("Lobby", m_transitionSettings, 0.1f);
                    return;
                }
            }

            SceneManager.LoadScene("Lobby");
        }
        #endregion
    }
}

