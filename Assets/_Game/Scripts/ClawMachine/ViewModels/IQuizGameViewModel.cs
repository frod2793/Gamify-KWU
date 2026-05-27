using System;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 뷰모델과 클래식 퀴즈 뷰모델의 채점/결과 이벤트를 공통 추상화하는 상위 인터페이스 (DIP)
    /// [작성자]: 윤승종
    /// </summary>
    public interface IQuizGameViewModel
    {
        #region 이벤트 (Events)

        event Action OnQuizSuccess;
        event Action OnQuizFailed;
        event Action OnReTakeRequested;

        #endregion

        #region 속성 (Properties)

        int ReTakeCount { get; }
        float TimeLeft { get; }

        #endregion

        #region 공개 메서드 (Public Methods)

        void AcceptReTake();
        void RejectReTake();
        
        /// <summary>
        /// [기능]: 오답 입력 후 게임 오버 처리 없이 그 자리에서 이어서 진행할 수 있도록 처리합니다.
        /// [작성자]: 윤승종
        /// </summary>
        void ContinueAfterWrongAnswer();

        /// <summary>
        /// [기능]: 정답 입력 후 다음 퀴즈/단계로 이어서 진행할 수 있도록 처리합니다.
        /// [작성자]: 윤승종
        /// </summary>
        void ContinueAfterCorrectAnswer();

        #endregion
    }
}
