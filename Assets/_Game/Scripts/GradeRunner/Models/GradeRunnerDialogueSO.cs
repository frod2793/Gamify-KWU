using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 등장하는 교수님의 등장 및 페이즈 전환 시의 말풍선 대사 목록을 보관하고 관리하는 ScriptableObject
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    [CreateAssetMenu(fileName = "GradeRunnerDialogueSO", menuName = "Gamify-KWU/GradeRunnerDialogueSO")]
    public class GradeRunnerDialogueSO : ScriptableObject
    {
        #region 내부 필드 (Private Fields)

        [Header("교수 등장 대사 설정")]
        [SerializeField]
        [TextArea(2, 5)]
        [Tooltip("도입부 교수 등장 시 출력될 타이핑 대사입니다.")]
        private string m_introDialogue = "자, 지금부터 코딩 테스트를 시작하겠다!";

        [Header("교수 2페이즈 전환 대사 설정")]
        [SerializeField]
        [TextArea(2, 5)]
        [Tooltip("2페이즈 전환 시 출력될 타이핑 대사입니다.")]
        private string m_phase2Dialogue = "아직 끝나지 않았다! 진정한 매운맛을 보여주지!";

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        public string IntroDialogue => m_introDialogue;
        public string Phase2Dialogue => m_phase2Dialogue;

        #endregion
    }
}
