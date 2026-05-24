using UnityEngine;
using UnityEngine.UI;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 제한 시간 초과 시 화면에 나타나 재수강 여부를 결정하는 팝업 View
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawReTakePopupView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("재수강 설명 및 패널티 정보를 보여줄 Text 컴포넌트입니다.")]
        private Text m_descriptionText;

        [SerializeField]
        [Tooltip("재수강 수락(동의) 버튼입니다.")]
        private Button m_acceptButton;

        [SerializeField]
        [Tooltip("재수강 거절(비동의) 버튼입니다.")]
        private Button m_rejectButton;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;
            
            if (m_acceptButton != null)
            {
                m_acceptButton.onClick.AddListener(func_OnAcceptButtonClick);
            }
            if (m_rejectButton != null)
            {
                m_rejectButton.onClick.AddListener(func_OnRejectButtonClick);
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (m_acceptButton != null)
            {
                m_acceptButton.onClick.RemoveListener(func_OnAcceptButtonClick);
            }
            if (m_rejectButton != null)
            {
                m_rejectButton.onClick.RemoveListener(func_OnRejectButtonClick);
            }
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        public void func_ShowPopup()
        {
            gameObject.SetActive(true);
            UpdateDescription();
        }

        public void func_HidePopup()
        {
            gameObject.SetActive(false);
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        private void UpdateDescription()
        {
            if (m_descriptionText != null && m_viewModel != null)
            {
                int currentPenaltySeconds = (m_viewModel.ReTakeCount + 1) * 20;
                int nextTimeLimit = 120 - currentPenaltySeconds;
                if (nextTimeLimit < 20)
                {
                    nextTimeLimit = 20;
                }

                m_descriptionText.text = "시간이 초과되었습니다!\n재수강을 신청하시겠습니까?\n\n" +
                                         $"[혜택] 방해 캡슐 '동의 안 함' 1개 제거\n" +
                                         $"[패널티] 제한 시간 {currentPenaltySeconds}초 차감 (다음 판: {nextTimeLimit}초)";
            }
        }

        private void func_OnAcceptButtonClick()
        {
            if (m_viewModel != null)
            {
                Debug.Log("[ClawReTakePopupView] 플레이어가 재수강 동의 버튼을 클릭했습니다.");
                m_viewModel.AcceptReTake();
            }
            func_HidePopup();
        }

        private void func_OnRejectButtonClick()
        {
            if (m_viewModel != null)
            {
                Debug.Log("[ClawReTakePopupView] 플레이어가 재수강 거절 버튼을 클릭했습니다.");
                m_viewModel.RejectReTake();
            }
            func_HidePopup();
        }
        #endregion
    }
}
