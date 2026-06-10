using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GameArifiction.Player;
using VContainer;

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

        [SerializeField]
        [Tooltip("세팅 팝업을 열 수 있는 버튼 컴포넌트입니다.")]
        private Button m_settingsButton;

        [SerializeField]
        [Tooltip("세팅 버튼 클릭 시 활성화될 공통 설정 팝업 뷰 컴포넌트입니다.")]
        private GameArifiction.UI.Common.CommonSettingsPopupView m_settingsPopup;
        #endregion

        #region 내부 필드
        private InteractionUI_ViewModel m_viewModel;
        #endregion

        #region 의존성 주입
        [Inject]
        public void Construct(InteractionUI_ViewModel viewModel)
        {
            m_viewModel = viewModel;
        }
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            // VContainer를 통해 m_viewModel이 주입됨
        }

        private void Start()
        {
            // [Zero Singleton]: 플레이어와의 결합을 낮추기 위해 시작 시 동적 바인딩을 주입합니다.
            PlayerView player = FindFirstObjectByType<PlayerView>();
            if (player != null)
            {
                player.OnInteractableTargetDetected += HandleTargetDetected;
                player.OnInteractableTargetLost += HandleTargetLost;

                if (m_viewModel != null)
                {
                    m_viewModel.OnInteractionExecuted += player.RequestInteraction;
                }
            }

            // [자동 이벤트 등록]: 상호작용 버튼 클릭
            if (m_interactionButton != null)
            {
                m_interactionButton.onClick.AddListener(func_OnInteractButtonClicked);
            }

            // [자동 이벤트 등록]: 설정 버튼 클릭
            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.AddListener(func_OnSettingsButtonClicked);
            }
            
            // 자동 주입 후 상태 변화 이벤트 구독
            if (m_viewModel != null)
            {
                m_viewModel.OnStateChanged += UpdateUI;
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

            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.RemoveListener(func_OnSettingsButtonClicked);
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

        /// <summary>
        /// [기능]: 세팅 버튼이 클릭되었을 때 설정 팝업을 띄우는 핸들러 (func_ 규칙 엄수)
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnSettingsButtonClicked()
        {
            if (m_settingsPopup != null)
            {
                m_settingsPopup.ShowPopup();
                Debug.Log("[InteractionUI_View] 설정 버튼 클릭으로 공통 설정 팝업을 오픈했습니다.");
            }
            else
            {
                Debug.LogWarning("[InteractionUI_View] 설정 버튼이 클릭되었으나 연결된 설정 팝업(m_settingsPopup)이 존재하지 않습니다.");
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
