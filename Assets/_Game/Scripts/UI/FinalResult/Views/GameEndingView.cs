using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer;

namespace GameArifiction.UI.FinalResult
{
    /// <summary>
    /// [기능]: 엔딩 스크린의 UI 연출(페이드, 텍스트 깜빡임)을 수행하고 입력을 감지하는 View 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class GameEndingView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [Header("UI 요소")]
        [SerializeField]
        [Tooltip("화면을 덮을 검은색 배경 패널 캔버스 그룹입니다.")]
        private CanvasGroup m_backgroundCanvasGroup;

        [SerializeField]
        [Tooltip("엔딩 화면 상단 이미지 컴포넌트입니다.")]
        private Image m_topImage;

        [SerializeField]
        [Tooltip("엔딩 화면 중단 이미지 컴포넌트입니다.")]
        private Image m_middleImage;

        [SerializeField]
        [Tooltip("엔딩 화면 하단 이미지 컴포넌트입니다.")]
        private Image m_bottomImage;

        [SerializeField]
        [Tooltip("'아무 키나 누르세요' 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_pressAnyKeyText;
        #endregion

        #region 내부 필드 (Private Fields)
        private GameEndingViewModel m_viewModel;
        private bool m_isWaitingForInput = false;
        private Sequence m_endingSequence;
        #endregion

        #region VContainer 주입 (Injection)
        /// <summary>
        /// [기능]: VContainer를 통해 GameEndingViewModel을 주입받고 이벤트를 구독합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 추가
        /// </summary>
        [Inject]
        public void Construct(GameEndingViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 이벤트 구독
            m_viewModel.OnPlayEndingSequence += HandlePlayEndingSequence;
            m_viewModel.OnLoadLobbyScene += HandleLoadLobbyScene;
        }
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        /// <summary>
        /// [기능]: 시작 시 엔딩 화면 요소를 숨김 처리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 아무 키 안내 텍스트의 게임오브젝트 초기 비활성화 코드 추가
        /// </summary>
        private void Awake()
        {
            if (m_backgroundCanvasGroup != null)
            {
                m_backgroundCanvasGroup.alpha = 0f;
                m_backgroundCanvasGroup.gameObject.SetActive(false);
            }

            if (m_topImage != null)
            {
                m_topImage.color = new Color(m_topImage.color.r, m_topImage.color.g, m_topImage.color.b, 0f);
            }

            if (m_middleImage != null)
            {
                m_middleImage.color = new Color(m_middleImage.color.r, m_middleImage.color.g, m_middleImage.color.b, 0f);
            }

            if (m_bottomImage != null)
            {
                m_bottomImage.color = new Color(m_bottomImage.color.r, m_bottomImage.color.g, m_bottomImage.color.b, 0f);
            }

            if (m_pressAnyKeyText != null)
            {
                m_pressAnyKeyText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// [기능]: 입력 대기 상태일 때 키보드, 마우스, 모바일 터치 입력을 감지합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Input System 패키지 대응 입력 감지 교정 (키보드, 마우스, 터치 일괄 감지)
        /// </summary>
        private void Update()
        {
            if (m_isWaitingForInput)
            {
                bool hasInput = false;

                // 1. 키보드 임의 키 입력 감지
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                {
                    hasInput = true;
                }

                // 2. 모바일 터치 입력 감지
                if (hasInput == false)
                {
                    var touchScreen = Touchscreen.current;
                    if (touchScreen != null && touchScreen.touches.Count > 0 && touchScreen.touches[0].press.wasPressedThisFrame)
                    {
                        hasInput = true;
                    }
                }

                // 3. 마우스 클릭 감지 (폴백 및 에디터 테스트)
                if (hasInput == false)
                {
                    var mouse = Mouse.current;
                    if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    {
                        hasInput = true;
                    }
                }

                if (hasInput)
                {
                    m_isWaitingForInput = false;
                    m_viewModel.AnyKeyInputProcessed();
                }
            }
        }

        /// <summary>
        /// [기능]: 파괴 시 DOTween 킬 및 이벤트를 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 이력 추가
        /// </summary>
        private void OnDestroy()
        {
            if (m_endingSequence != null)
            {
                m_endingSequence.Kill();
            }

            if (m_topImage != null)
            {
                m_topImage.DOKill();
            }

            if (m_middleImage != null)
            {
                m_middleImage.DOKill();
            }

            if (m_bottomImage != null)
            {
                m_bottomImage.DOKill();
            }

            if (m_pressAnyKeyText != null)
            {
                m_pressAnyKeyText.DOKill();
            }

            if (m_viewModel != null)
            {
                m_viewModel.OnPlayEndingSequence -= HandlePlayEndingSequence;
                m_viewModel.OnLoadLobbyScene -= HandleLoadLobbyScene;
            }
        }
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        /// <summary>
        /// [기능]: ViewModel로부터 엔딩 연출 재생 지시를 받았을 때 실행됩니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 3분할 이미지 순차적 페이드 인 연출 순서 교정 및 안내 텍스트 동적 활성화, DOColor 연출 적용, 속도(1.2초) 조정
        /// </summary>
        private void HandlePlayEndingSequence()
        {
            if (m_backgroundCanvasGroup == null || m_topImage == null || m_middleImage == null || m_bottomImage == null || m_pressAnyKeyText == null)
            {
                Debug.LogError("[GameEndingView] UI 참조가 누락되었습니다.");
                return;
            }

            // 부모 게임오브젝트를 활성화
            gameObject.SetActive(true);
            m_backgroundCanvasGroup.gameObject.SetActive(true);

            // 각 연출 대상의 알파값을 안전하게 0으로 초기화
            m_backgroundCanvasGroup.alpha = 0f;
            m_topImage.color = new Color(m_topImage.color.r, m_topImage.color.g, m_topImage.color.b, 0f);
            m_middleImage.color = new Color(m_middleImage.color.r, m_middleImage.color.g, m_middleImage.color.b, 0f);
            m_bottomImage.color = new Color(m_bottomImage.color.r, m_bottomImage.color.g, m_bottomImage.color.b, 0f);
            m_pressAnyKeyText.color = new Color(m_pressAnyKeyText.color.r, m_pressAnyKeyText.color.g, m_pressAnyKeyText.color.b, 0f);

            m_endingSequence = DOTween.Sequence();

            // 1. 검은 배경 페이드인 (1.5초)
            m_endingSequence.Append(m_backgroundCanvasGroup.DOFade(1f, 1.5f));

            // 2. 각각 연출: 상단 이미지 페이드인 (1.2초)
            m_endingSequence.Append(m_topImage.DOFade(1f, 1.2f));

            // 3. 각각 연출: 약간 대기 (0.3초) 후 하단 이미지 페이드인 (1.2초)
            m_endingSequence.AppendInterval(0.3f);
            m_endingSequence.Append(m_bottomImage.DOFade(1f, 1.2f));

            // 4. 각각 연출: 약간 대기 (0.3초) 후 중단 이미지 페이드인 (1.5초)
            m_endingSequence.AppendInterval(0.3f);
            m_endingSequence.Append(m_middleImage.DOFade(1f, 1.5f));

            // 5. 약간 대기 (0.5초)
            m_endingSequence.AppendInterval(0.5f);

            // 6. "아무 키나 누르세요" 텍스트 페이드인 및 깜빡임 애니메이션 시작, 입력 대기 허용
            m_endingSequence.AppendCallback(() =>
            {
                m_pressAnyKeyText.gameObject.SetActive(true);
                Color targetColor = m_pressAnyKeyText.color;
                targetColor.a = 1f;
                m_pressAnyKeyText.DOColor(targetColor, 1.2f).SetLoops(-1, LoopType.Yoyo);
                m_isWaitingForInput = true;
            });
        }

        /// <summary>
        /// [기능]: 씬 전환 이벤트 수신 시 로비 씬을 로드합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 이력 추가
        /// </summary>
        private void HandleLoadLobbyScene()
        {
            Debug.Log("[GameEndingView] 엔딩이 종료되어 Lobby 씬으로 이동합니다.");
            SceneManager.LoadScene("Lobby");
        }
        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 디버그 테스트용으로 외부에서 엔딩 연출 시퀀스를 즉시 강제 트리거합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 엔딩 연출 디버그용 테스트 메서드 추가
        /// </summary>
        public void func_TestPlayEndingSequence()
        {
            HandlePlayEndingSequence();
        }

        #endregion
    }
}
