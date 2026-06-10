using UnityEngine;
using UnityEngine.UI;
using EasyTransition;
using VContainer;
using DG.Tweening;

/// <summary>
/// [기능]: 타이틀 화면 UI의 시각적 요소와 플레이어 입력을 담당하며, Transition 패키지를 제어하는 뷰 클래스입니다.
/// [작성자]: 윤승종
/// </summary>
namespace GamifyKWU.UI.Title
{
    /// <summary>
    /// [기능]: 타이틀 로고 이미지의 인트로 연출 애니메이션 타입 정의입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public enum TitleLogoAnimType
    {
        None,
        RotateAndFlyIn, // 회전하며 날아오기
        ScalePopIn      // 축소 상태에서 확대되며 나타나기 (Scale 0 -> 1)
    }

    public class TitleView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("트랜지션 설정")]
        [SerializeField] private TransitionSettings m_transitionSettings;
        [SerializeField] private float m_startDelay = 0f;

        [Header("타이틀 로고 애니메이션 설정")]
        [SerializeField]
        [Tooltip("애니메이션을 적용할 타이틀 로고의 RectTransform입니다.")]
        private RectTransform m_titleLogoRect;

        [SerializeField]
        [Tooltip("타이틀 시작 시 적용할 애니메이션 종류입니다.")]
        private TitleLogoAnimType m_logoAnimType = TitleLogoAnimType.ScalePopIn;

        [SerializeField]
        [Tooltip("애니메이션 진행 시간(초)입니다.")]
        private float m_logoAnimDuration = 0.8f;

        [SerializeField]
        [Tooltip("회전하며 날아올 때 시작할 오프셋 위치입니다.")]
        private Vector2 m_flyInStartOffset = new Vector2(0f, 600f);

        [SerializeField]
        [Tooltip("회전하며 날아올 때의 시작 회전 각도(Z축)입니다.")]
        private float m_flyInStartRotation = -180f;

        [SerializeField]
        [Tooltip("애니메이션의 Ease 종류입니다.")]
        private Ease m_logoAnimEase = Ease.OutBack;

        #endregion

        #region 내부 필드 (Private Fields)

        private TitleViewModel m_viewModel;

        [Inject]
        private IntroCutsceneController m_introController;

        #endregion

        #region 의존성 주입 (Dependency Injection)

        [Inject]
        public void Construct(TitleViewModel viewModel)
        {
            m_viewModel = viewModel;
        }

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Start()
        {
            InitializeMVVM();
            PlayTitleLogoAnimation();
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnPlayCommandTriggered -= HandlePlayCommandTriggered;
            }
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: MVVM 구조에 맞추어 모델 및 뷰모델을 생성하고 이벤트를 구독 바인딩합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void InitializeMVVM()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnPlayCommandTriggered += HandlePlayCommandTriggered;
            }
            else
            {
                Debug.LogError("[TitleView] 뷰모델이 주입되지 않았습니다!");
            }
        }

        /// <summary>
        /// [기능]: 설정된 애니메이션 타입에 맞춰 타이틀 로고의 인트로 연출을 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 신규 생성 및 DOTween 애니메이션 구현
        /// </summary>
        private void PlayTitleLogoAnimation()
        {
            if (m_titleLogoRect == null)
            {
                Debug.LogWarning("[TitleView] m_titleLogoRect가 할당되지 않아 애니메이션을 재생할 수 없습니다.");
                return;
            }

            // DOTween 안전 예외 처리 (이전 실행 중인 트윈이 있다면 정지)
            m_titleLogoRect.DOKill();

            switch (m_logoAnimType)
            {
                case TitleLogoAnimType.RotateAndFlyIn:
                    Vector2 originalPos = m_titleLogoRect.anchoredPosition;
                    m_titleLogoRect.anchoredPosition = originalPos + m_flyInStartOffset;
                    m_titleLogoRect.localRotation = Quaternion.Euler(0f, 0f, m_flyInStartRotation);

                    m_titleLogoRect.DOAnchorPos(originalPos, m_logoAnimDuration).SetEase(m_logoAnimEase);
                    m_titleLogoRect.DOLocalRotate(Vector3.zero, m_logoAnimDuration, RotateMode.FastBeyond360).SetEase(m_logoAnimEase);
                    break;

                case TitleLogoAnimType.ScalePopIn:
                    m_titleLogoRect.localScale = Vector3.zero;
                    m_titleLogoRect.DOScale(Vector3.one, m_logoAnimDuration).SetEase(m_logoAnimEase);
                    break;

                case TitleLogoAnimType.None:
                default:
                    break;
            }
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        /// <summary>
        /// [기능]: UI 플레이 버튼 클릭 시 인스펙터 UnityEvent 등을 통해 직접 실행되도록 열려 있는 public 이벤트 핸들러입니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnPlayButtonClicked()
        {
            Debug.Log("[TitleView] 플레이 버튼 클릭됨. 인게임 진입 프로세스를 시작합니다.");
            
            if (m_viewModel != null)
            {
                m_viewModel.ExecutePlayCommand();
            }
        }

        /// <summary>
        /// [기능]: 뷰모델에서 플레이 명령이 최종 트리거되었을 때 트랜지션을 실행합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-28
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이틀 패널이 로비 씬 내부에 배치되어 있으므로 씬 전환 로드가 아니라 이지 트랜지션 완료 시점(CutPoint)에 타이틀 패널을 비활성화하도록 수정
        /// </summary>
        /// <param name="dto">전환 시 사용될 전송 데이터 DTO</param>
        private void HandlePlayCommandTriggered(TitleToInGameDTO dto)
        {
            Debug.Log("[TitleView] 동일 씬(Lobby) 내에서 타이틀 패널을 비활성화하는 이지 트랜지션 연출을 재생합니다.");

            TransitionManager transitionManager = TransitionManager.Instance();
            if (transitionManager != null)
            {
                if (m_transitionSettings != null)
                {
                    // 1. 트랜지션의 컷포인트(화면이 완전히 가려진 중심점) 도달 이벤트에 패널 비활성화 메서드 임시 구독
                    transitionManager.onTransitionCutPointReached += HandleTransitionCutPointReached;

                    // 2. 씬 전환 없이 트랜지션 효과만 재생하는 API 호출
                    transitionManager.Transition(m_transitionSettings, m_startDelay);
                }
                else
                {
                    Debug.LogWarning("[TitleView] TransitionSettings가 할당되지 않았습니다. 패널을 즉시 비활성화하고 인트로를 트리거합니다.");
                    TriggerIntroCutsceneDirectly();
                }
            }
            else
            {
                Debug.LogError("[TitleView] 씬에 TransitionManager가 존재하지 않습니다. 패널을 즉시 비활성화하고 인트로를 트리거합니다.");
                TriggerIntroCutsceneDirectly();
            }
        }

        /// <summary>
        /// [기능]: 트랜지션 연출이 화면을 완전히 덮는 컷포인트 도달 시점에 호출되어 타이틀 UI 패널을 비활성화하고 인트로 연출을 트리거합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleTransitionCutPointReached()
        {
            TransitionManager transitionManager = TransitionManager.Instance();
            if (transitionManager != null)
            {
                // 이벤트 중복 호출 방지를 위한 수동 해제
                transitionManager.onTransitionCutPointReached -= HandleTransitionCutPointReached;
            }

            Debug.Log("[TitleView] 트랜지션 컷포인트에 도달하여 타이틀 패널을 비활성화 처리하고 인트로 연출을 트리거합니다.");
            gameObject.SetActive(false);

            // 씬 내에 주입받은 IntroCutsceneController의 컷씬 시작을 명령합니다.
            if (m_introController != null)
            {
                m_introController.StartIntroCutscene();
            }
            else
            {
                Debug.LogWarning("[TitleView] m_introController가 주입되지 않았습니다.");
            }
        }

        /// <summary>
        /// [기능]: 트랜지션 에셋이 유실되었을 때 다이렉트로 인트로 연출을 시작하도록 처리하는 헬퍼 메서드입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void TriggerIntroCutsceneDirectly()
        {
            gameObject.SetActive(false);

            if (m_introController != null)
            {
                m_introController.StartIntroCutscene();
            }
            else
            {
                Debug.LogWarning("[TitleView] m_introController가 주입되지 않았습니다.");
            }
        }

        #endregion
    }
}
