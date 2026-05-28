using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 하늘에서 떨어지는 피해야 할 코드(장애물) 텍스트 목록을 보관 및 관리하는 ScriptableObject
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    [CreateAssetMenu(fileName = "GradeRunnerCodeListSO", menuName = "Gamify-KWU/GradeRunnerCodeListSO")]
    public class GradeRunnerCodeListSO : ScriptableObject
    {
        #region 내부 필드 (Private Fields)

        [Header("피해야 할 코드 목록")]
        [SerializeField]
        [Tooltip("하늘에서 떨어지는 피해야 할 C# 프로그래밍 관련 단어 및 경고 텍스트 배열입니다.")]
        private string[] m_codeWords = new string[]
        {
            "int", "float", "double", "string", "bool", "char", "var", "object", "void",
            "if", "else", "switch", "case", "break", "for", "foreach", "while", "do",
            "in", "return", "public", "private", "protected", "internal", "static", "const", "readonly"
        };

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public string[] CodeWords => m_codeWords;

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 피해야 할 단어 목록 중에서 무작위로 하나의 단어를 산출하여 반환합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public string GetRandomCodeWord()
        {
            if (m_codeWords == null || m_codeWords.Length == 0)
            {
                return "bug";
            }
            int randIdx = Random.Range(0, m_codeWords.Length);
            return m_codeWords[randIdx];
        }

        #endregion
    }
}
