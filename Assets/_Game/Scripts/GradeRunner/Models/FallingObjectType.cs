/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 하늘에서 떨어지는 오브젝트의 종류 및 속성을 정의하는 파일
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    /// <summary>
    /// 낙하 오브젝트의 분류 (장애물 코드 또는 점수 획득 족보)
    /// </summary>
    public enum FallingObjectType
    {
        Code,       // 코드 (학점 감점 장애물)
        CheatSheet  // 족보 (학점 가점 아이템)
    }

    /// <summary>
    /// 코드 오브젝트의 색상 카테고리
    /// </summary>
    public enum CodeColorType
    {
        Red,        // 빨간색 (8%)
        Purple,     // 보라색 (23%)
        Yellow,     // 노란색 (23%)
        SkyBlue,    // 하늘색 (23%)
        Green       // 녹색 (23%)
    }
}
