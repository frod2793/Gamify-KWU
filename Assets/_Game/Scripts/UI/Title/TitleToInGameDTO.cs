/// <summary>
/// [기능]: 타이틀에서 인게임(로비)으로 전환 시 전달할 데이터를 캡슐화하는 DTO 클래스입니다.
/// [작성자]: 윤승종
/// </summary>
namespace GamifyKWU.UI.Title
{
    public class TitleToInGameDTO
    {
        #region 공개 프로퍼티
        
        public string TargetSceneName { get; }
        public System.DateTime PlayStartTime { get; }

        #endregion

        #region 초기화

        public TitleToInGameDTO(string targetSceneName)
        {
            TargetSceneName = targetSceneName;
            PlayStartTime = System.DateTime.Now;
        }

        #endregion
    }
}
