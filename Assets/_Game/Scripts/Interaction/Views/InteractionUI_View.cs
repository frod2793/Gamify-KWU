using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GameArifiction.Player;

namespace GameArifiction.Interaction
{
    /// <summary>
    /// [기능]: 상호작용 UI 버튼 노출 및 안내 메시지 데이터 바인딩만을 전담하는 조작 뷰 클래스
    /// [작성자]: 윤승종
    /// </summary>
    public class InteractionUI_View : MonoBehaviour
    {
        #region UI 참조
        [Header("UI 설정")]
        [SerializeField]
        [Tooltip("상호작용 버튼 컴포넌트입니다.")]
        private Button m_interactionButton;

        [SerializeField]
        [Tooltip("상호작용 안내 문구를 출력할 TMPro 텍스트 컴포넌트입니다.")]
        private TMP_Text m_promptText;
        #endregion

        #region 내부 필드
        private InteractionUI_ViewModel m_viewModel;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            InitializeMVVM();
        }

        private void Start()
        {
            // [Zero Singleton]: 플레이어와의 결합을 낮추기 위해 시작 시 동적 바인딩을 주입합니다.
            PlayerView player = FindFirstObjectByType<PlayerView>();
            if (player != null)
            {
                player.OnInteractableTargetDetected += HandleTargetDetected;
                player.OnInteractableTargetLost += HandleTargetLost;

                m_viewModel.OnInteractionExecuted += player.RequestInteraction;
            }

            // [자동 이벤트 등록]: 상호작용 버튼 클릭
            if (m_interactionButton != null)
            {
                m_interactionButton.onClick.AddListener(func_OnInteractButtonClicked);
            }
            
            // 최초 실행 시 비활성화 상태 보증
            UpdateUI(false, string.Empty);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region 초기화
        private void InitializeMVVM()
        {
            var model = new InteractionUI_Model();
            m_viewModel = new InteractionUI_ViewModel(model);

            m_viewModel.OnStateChanged += UpdateUI;
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged -= UpdateUI;
            }

            PlayerView player = FindFirstObjectByType<PlayerView>();
            if (player != null)
            {
                player.OnInteractableTargetDetected -= HandleTargetDetected;
                player.OnInteractableTargetLost -= HandleTargetLost;

                if (m_viewModel != null)
                {
                    m_viewModel.OnInteractionExecuted -= player.RequestInteraction;
                }
            }

            if (m_interactionButton != null)
            {
                m_interactionButton.onClick.RemoveListener(func_OnInteractButtonClicked);
            }
        }
        #endregion

        #region 이벤트 핸들러
        /// <summary>
        /// [기능]: UI 버튼이 클릭되었을 때 호출될 핸들러 (func_ 규칙 엄수)
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnInteractButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.ExecuteInteraction();
            }
        }

        private void HandleTargetDetected(IInteractable interactable)
        {
            if (m_viewModel != null && interactable != null)
            {
                m_viewModel.SetInteractionState(true, interactable.InteractionPrompt);
            }
        }

        private void HandleTargetLost()
        {
            if (m_viewModel != null)
            {
                m_viewModel.SetInteractionState(false, string.Empty);
            }

            // 상호작용 대상이 이탈하면 상위 UIManager를 통해 표지판 창도 자동으로 닫히도록 흐름 제어
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.HideSignboard();
            }
        }
        #endregion

        #region 내부 메서드
        private void UpdateUI(bool isInteractable, string promptText)
        {
            if (m_interactionButton != null)
            {
                m_interactionButton.gameObject.SetActive(isInteractable);
            }

            if (m_promptText != null)
            {
                m_promptText.text = promptText;
            }
        }
        #endregion
    }
}
