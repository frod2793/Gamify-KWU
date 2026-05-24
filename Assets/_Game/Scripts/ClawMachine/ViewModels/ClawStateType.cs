namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 기계의 현재 동작 상태를 나타내는 열거형
    /// [작성자]: 윤승종
    /// </summary>
    public enum ClawStateType
    {
        Idle,           // 대기 상태 (이동 가능)
        MovingLeft,     // 좌측 이동 중
        MovingRight,    // 우측 이동 중
        Descending,     // 하강 중
        Grabbing,       // 집기(벌리고 닫기)
        Ascending,      // 상승 중
        Returning,      // 원래 위치로 복귀 중
        Result,         // 결과 확인
        ReTakeRequest   // [신규]: 시간초과 시 재수강 요청 및 팝업 대기 상태
    }
}
