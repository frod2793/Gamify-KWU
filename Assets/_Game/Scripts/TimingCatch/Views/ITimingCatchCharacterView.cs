namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 캐치 캐릭터의 성공·실패·대기 반응 명령을 정의합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public interface ITimingCatchCharacterView
    {
        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 성공 판정 캐릭터 반응을 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 성공 반응 명령 계약 추가.
        /// </summary>
        void PlaySuccessReaction();

        /// <summary>
        /// [기능]: 실패 판정 캐릭터 반응을 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 실패 반응 명령 계약 추가.
        /// </summary>
        void PlayFailureReaction();

        /// <summary>
        /// [기능]: 진행 중인 반응을 종료하고 대기 상태로 복귀합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: IDLE 복귀 명령 계약 추가.
        /// </summary>
        void ResetToIdle();
        #endregion
    }
}
