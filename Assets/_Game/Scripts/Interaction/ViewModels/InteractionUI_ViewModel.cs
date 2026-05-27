using System;

namespace GameArifiction.Interaction
{
    /// <summary>
    /// [기능]: 상호작용 UI의 상태 통신 및 데이터 바인딩 이벤트를 관리하는 뷰모델 클래스 (POCO)
    /// [작성자]: 윤승종
    /// </summary>
    public class InteractionUI_ViewModel
    {
        #region 내부 필드 (Private Fields)
        private readonly InteractionUI_Model m_model;
        #endregion

        #region 이벤트 (Events)
        /// <summary>
        /// 상호작용 상태가 변화했을 때 뷰가 구독할 이벤트입니다. (상호작용 가능 여부, UI 텍스트)
        /// </summary>
        public event Action<bool, string> OnStateChanged;

        /// <summary>
        /// 실제 상호작용이 트리거되어 상호작용을 실행할 때 발생하는 이벤트입니다.
        /// </summary>
        public event Action OnInteractionExecuted;
        #endregion

        #region 공개 프로퍼티 (Public Properties)
        public bool IsInteractable => m_model.IsInteractable;
        public string PromptText => m_model.PromptText;
        #endregion

        #region 초기화 (Initialization)
        public InteractionUI_ViewModel(InteractionUI_Model model)
        {
            m_model = model;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 상호작용 대상을 인지하거나 해제할 때 UI 상태 데이터를 갱신하고 이벤트를 방출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        /// <param name="isInteractable">상호작용 가능 상태 여부</param>
        /// <param name="promptText">상호작용 프롬프트 텍스트</param>
        public void SetInteractionState(bool isInteractable, string promptText)
        {
            m_model.IsInteractable = isInteractable;
            m_model.PromptText = isInteractable ? promptText : string.Empty;

            OnStateChanged?.Invoke(m_model.IsInteractable, m_model.PromptText);
        }

        /// <summary>
        /// [기능]: 뷰에서 상호작용 버튼 클릭 시 호출되어 등록된 로직을 위임 실행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void ExecuteInteraction()
        {
            if (m_model.IsInteractable)
            {
                OnInteractionExecuted?.Invoke();
            }
        }
        #endregion
    }
}
