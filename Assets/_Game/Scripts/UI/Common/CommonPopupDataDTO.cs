using System;
using UnityEngine;
using GameArifiction.Player;

namespace GameArifiction.UI.Common
{
    /// <summary>
    /// [기능]: 공통 결과 팝업(CommonResultPopupView)에 데이터를 동적으로 전달하기 위한 DTO 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class CommonPopupDataDTO
    {
        #region 공개 프로퍼티 (Public Properties)

        /// <summary>
        /// 팝업 상단에 표시될 제목 텍스트입니다.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 결과 설명 및 상세 정보 본문 텍스트입니다.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 결과 학점 등급 정보입니다. (없을 경우 null)
        /// </summary>
        public MinigameGrade? Grade { get; set; }

        /// <summary>
        /// 확인 버튼에 노출될 텍스트입니다.
        /// </summary>
        public string ConfirmButtonText { get; set; }

        /// <summary>
        /// 확인 버튼을 눌렀을 때 호출될 콜백 이벤트입니다.
        /// </summary>
        public Action OnConfirm { get; set; }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: CommonPopupDataDTO 생성자
        /// [작성자]: 윤승종
        /// </summary>
        public CommonPopupDataDTO(string title, string description, MinigameGrade? grade, string confirmButtonText, Action onConfirm)
        {
            Title = title;
            Description = description;
            Grade = grade;
            ConfirmButtonText = confirmButtonText;
            OnConfirm = onConfirm;
        }

        #endregion
    }
}
