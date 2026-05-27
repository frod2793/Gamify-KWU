/// <summary>
/// [기능]: 타이틀 패널의 순수 비즈니스 데이터 및 상태 정보를 가지는 모델 클래스입니다.
/// [작성자]: 윤승종
/// </summary>
namespace GamifyKWU.UI.Title
{
    public class TitleModel
    {
        #region 내부 필드 (Private Fields)

        private string m_targetSceneName;
        private bool m_isTransitioning;

        #endregion

        #region 공개 프로퍼티

        public string TargetSceneName
        {
            get 
            { 
                return m_targetSceneName; 
            }
        }

        public bool IsTransitioning
        {
            get 
            { 
                return m_isTransitioning; 
            }
            set 
            { 
                m_isTransitioning = value; 
            }
        }

        #endregion

        #region 초기화

        public TitleModel(string targetSceneName)
        {
            m_targetSceneName = targetSceneName;
            m_isTransitioning = false;
        }

        #endregion
    }
}
