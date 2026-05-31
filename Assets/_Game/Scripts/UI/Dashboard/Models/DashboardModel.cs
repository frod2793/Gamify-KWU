using GamifyKWU.UI.Dashboard.DTO;
using UnityEngine;

namespace GamifyKWU.UI.Dashboard.Models
{
    /// <summary>
    /// [기능]: 중국어 번역 데이터를 보유하고 가공하는 순수 비즈니스 데이터 모델
    /// [작성자]: 윤승종
    /// </summary>
    public class DashboardModel
    {
        #region 내부 필드 (Private Fields)
        private LocalizationDTO m_localizationData;
        #endregion

        #region 프로퍼티 (Properties)
        public LocalizationDTO LocalizationData
        {
            get
            {
                return m_localizationData;
            }
        }
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// [기능]: JSON 문자열을 로드하여 번역 데이터를 초기화하는 생성자
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        public DashboardModel(string jsonContent)
        {
            if (!string.IsNullOrEmpty(jsonContent))
            {
                m_localizationData = JsonUtility.FromJson<LocalizationDTO>(jsonContent);
            }
        }
        #endregion
    }
}
