namespace GameArifiction.Interaction
{
    /// <summary>
    /// [기능]: 상호작용 UI의 순수 상태 데이터를 표현하는 모델 클래스 (POCO)
    /// [작성자]: 윤승종
    /// </summary>
    public class InteractionUI_Model
    {
        #region 내부 필드 (Private Fields)
        private bool m_isInteractable;
        private string m_promptText;
        #endregion

        #region 공개 프로퍼티 (Public Properties)
        public bool IsInteractable
        {
            get => m_isInteractable;
            set => m_isInteractable = value;
        }

        public string PromptText
        {
            get => m_promptText;
            set => m_promptText = value;
        }
        #endregion

        #region 초기화 (Initialization)
        public InteractionUI_Model()
        {
            m_isInteractable = false;
            m_promptText = string.Empty;
        }
        #endregion
    }
}
