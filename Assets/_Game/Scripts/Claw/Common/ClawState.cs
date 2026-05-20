namespace GameArifiction.Claw
{
    /// <summary>
    /// [기능]: 3버튼 제어 및 자동 복귀 흐름, 진입 연출을 제어하기 위한 집게 머신 상태 열거형
    /// [작성자]: [Senior Client Developer]
    /// </summary>
    public enum ClawState
    {
        IDLE,
        MOVING_LEFT,
        MOVING_RIGHT,
        DESCENDING,
        GRABBING,
        ASCENDING,
        MOVING_TO_DROP,
        RELEASING,
        RETURNING,
        ENTRANCE_SEQUENCE
    }
}
