using System;
using GamifyKWU.UI.Dashboard.DTO;
using GamifyKWU.UI.Dashboard.Models;
using UnityEngine;

namespace GamifyKWU.UI.Dashboard.ViewModels
{
    /// <summary>
    /// [기능]: Dashboard UI의 상태 관리 및 View의 바인딩을 위한 뷰모델
    /// [작성자]: 윤승종
    /// </summary>
    public class DashboardViewModel
    {
        #region 내부 필드 (Private Fields)
        private readonly DashboardModel m_model;
        #endregion

        #region 이벤트 (Events)
        public event Action<LocalizationDTO> OnLocalizationLoaded;
        public event Action OnDataRefreshed;
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: 뷰모델 생성자 및 모델 주입
        /// [작성자]: 윤승종
        /// </summary>
        public DashboardViewModel(DashboardModel model)
        {
            m_model = model;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 번역 데이터를 로드하고 뷰에 전파합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        public void LoadLocalization()
        {
            if (m_model != null)
            {
                if (m_model.LocalizationData != null)
                {
                    OnLocalizationLoaded?.Invoke(m_model.LocalizationData);
                }
            }
        }

        /// <summary>
        /// [기능]: 대시보드 데이터를 새로고침하는 명령을 처리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        public void RefreshData()
        {
            Debug.Log("[DashboardViewModel] 대시보드 데이터를 성공적으로 새로고침했습니다.");
            OnDataRefreshed?.Invoke();
        }
        #endregion
    }
}
