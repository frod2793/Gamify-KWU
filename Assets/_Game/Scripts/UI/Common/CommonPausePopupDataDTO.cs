using System;

namespace GameArifiction.UI.Common
{
    /// <summary>
    /// [기능]: 공통 일시정지 팝업에 전달될 데이터 및 콜백을 캡슐화하는 DTO
    /// [작성자]: 윤승종
    /// </summary>
    public class CommonPausePopupDataDTO
    {
        #region 공개 프로퍼티

        /// <summary>
        /// [기능]: 튜토리얼 다시보기 버튼 클릭 시 호출할 콜백
        /// </summary>
        public Action OnReplayTutorial { get; set; }

        /// <summary>
        /// [기능]: 퀴즈 다시보기 버튼 클릭 시 호출할 콜백 (null일 경우 퀴즈 버튼 숨김)
        /// </summary>
        public Action OnReplayQuiz { get; set; }

        /// <summary>
        /// [기능]: 게임 계속하기 버튼 클릭 시 호출할 콜백
        /// </summary>
        public Action OnResume { get; set; }

        #endregion
    }
}
