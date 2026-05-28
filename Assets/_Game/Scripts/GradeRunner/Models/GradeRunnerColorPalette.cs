using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 사용되는 등급별 및 오브젝트별 색상 상수 정의
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public static class GradeRunnerColorPalette
    {
        #region 성적 등급 색상

        public static readonly Color GradeA = new Color(1.0f, 0.76f, 0.03f, 1f); // Golden Amber
        public static readonly Color GradeB = new Color(0.18f, 0.8f, 0.44f, 1f); // Soft Emerald Green
        public static readonly Color GradeC = new Color(0.16f, 0.5f, 0.73f, 1f); // Deep SkyBlue
        public static readonly Color GradeD = new Color(0.9f, 0.5f, 0.13f, 1f);  // Warm Orange
        public static readonly Color GradeF = new Color(0.9f, 0.28f, 0.24f, 1f); // Coral Crimson Red

        #endregion

        #region 코드(장애물) 텍스트 색상

        public static readonly Color CodeRed = new Color(0.95f, 0.26f, 0.21f, 1f);
        public static readonly Color CodePurple = new Color(0.61f, 0.15f, 0.69f, 1f);
        public static readonly Color CodeYellow = new Color(1.0f, 0.76f, 0.03f, 1f);
        public static readonly Color CodeSkyBlue = new Color(0.03f, 0.73f, 0.95f, 1f);
        public static readonly Color CodeGreen = new Color(0.3f, 0.69f, 0.31f, 1f);

        #endregion

        #region 족보(아이템) 색상

        public static readonly Color CheatSheetGold = new Color(1.0f, 0.84f, 0.0f, 1f);

        #endregion
    }
}
