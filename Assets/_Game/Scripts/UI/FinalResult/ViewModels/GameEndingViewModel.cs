using System;

namespace GameArifiction.UI.FinalResult
{
    /// <summary>
    /// [기능]: 엔딩 스크린 연출을 제어하고 사용자 입력에 따른 씬 전환 이벤트를 발생시키는 ViewModel 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class GameEndingViewModel
    {
        #region 이벤트 핸들러 (Event Handlers)
        /// <summary>
        /// [기능]: 뷰에게 엔딩 시퀀스 애니메이션을 시작하도록 지시합니다.
        /// </summary>
        public event Action OnPlayEndingSequence;

        /// <summary>
        /// [기능]: 엔딩 시퀀스 도중 아무 키나 입력되었을 때, 로비 씬을 로드하도록 지시합니다.
        /// </summary>
        public event Action OnLoadLobbyScene;
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 엔딩 연출을 시작합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void StartEndingCommand()
        {
            OnPlayEndingSequence?.Invoke();
        }

        /// <summary>
        /// [기능]: 사용자가 키보드나 마우스를 입력했을 때 호출되는 커맨드입니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void AnyKeyInputProcessed()
        {
            OnLoadLobbyScene?.Invoke();
        }
        #endregion
    }
}
