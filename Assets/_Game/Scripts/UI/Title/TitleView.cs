using UnityEngine;
using UnityEngine.UI;
using EasyTransition;
using VContainer;
using DG.Tweening;
using TMPro;

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

        [Header("빌드 버전 표시 설정")]
        [SerializeField]
        [Tooltip("빌드 버전을 표시할 TextMeshProUGUI 컴포넌트입니다. 미지정 시 런타임에 동적으로 좌하단에 생성합니다.")]
        private TextMeshProUGUI m_versionText;

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

        [Header("타이틀 로고 플로팅 설정")]
        [SerializeField]
        [Tooltip("타이틀 로고에 둥둥 뜨는 플로팅 이펙트를 적용할지 여부입니다.")]
        private bool m_useFloatingEffect = true;

        [SerializeField]
        [Tooltip("플로팅 이펙트의 위아래 흔들림 진폭(px)입니다.")]
        private float m_floatingAmplitude = 15f;

        [SerializeField]
        [Tooltip("플로팅 이펙트의 한 주기 시간(초)입니다.")]
        private float m_floatingDuration = 1.5f;

        [Header("배경 구름 스크롤 설정 (통합 연출)")]
        [SerializeField]
        [Tooltip("무한 스크롤을 적용할 구름 이미지들의 RectTransform 목록입니다.")]
        private RectTransform[] m_cloudRects;

        [SerializeField]
        [Tooltip("초당 스크롤 속도(px)입니다.")]
        private float m_cloudScrollSpeed = 30f;

        #endregion

        #region 내부 필드 (Private Fields)

        private TitleViewModel m_viewModel;
        private RectTransform m_canvasRect;

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
            SetupVersionText();

            // WebGL 최적화: 해상도 변경 대응을 위한 부모 Canvas 캐싱
            if (m_cloudRects != null && m_cloudRects.Length > 0)
            {
                if (m_cloudRects[0] != null)
                {
                    m_canvasRect = m_cloudRects[0].parent as RectTransform;
                }
            }
        }

        private void Update()
        {
            ScrollClouds();
        }

        private void OnDestroy()
        {
            if (m_titleLogoRect != null)
            {
                m_titleLogoRect.DOKill();
            }

            if (m_viewModel != null)
            {
                m_viewModel.OnPlayCommandTriggered -= HandlePlayCommandTriggered;
            }
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: 타이틀 화면 구석에 빌드 버전을 표시하기 위한 UI 텍스트메시프를 설정합니다. 미지정 시 런타임에 자동 생성합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-26
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 텍스트메시프로 버전 텍스트 셋업 및 Fallback 생성 연출 구현
        /// </summary>
        private void SetupVersionText()
        {
            if (m_versionText != null)
            {
                m_versionText.text = Application.version;
                return;
            }

            // UI 참조가 비어 있을 시 런타임에 코드로 동적 생성 및 스타일링
            GameObject versionObj = new GameObject("VersionText", typeof(RectTransform), typeof(TextMeshProUGUI));
            versionObj.transform.SetParent(this.transform, false);

            RectTransform rectTransform = versionObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(20f, 20f);

            m_versionText = versionObj.GetComponent<TextMeshProUGUI>();
            m_versionText.text = Application.version;
            m_versionText.fontSize = 18f;
            m_versionText.color = new Color(1f, 1f, 1f, 0.7f);
            
            Debug.Log($"[TitleView] 버전 텍스트가 인스펙터에 미할당되어 런타임에 동적으로 설치했습니다: {Application.version}");
        }

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

            Vector2 originalPos = m_titleLogoRect.anchoredPosition;

            switch (m_logoAnimType)
            {
                case TitleLogoAnimType.RotateAndFlyIn:
                    m_titleLogoRect.anchoredPosition = originalPos + m_flyInStartOffset;
                    m_titleLogoRect.localRotation = Quaternion.Euler(0f, 0f, m_flyInStartRotation);

                    m_titleLogoRect.DOAnchorPos(originalPos, m_logoAnimDuration).SetEase(m_logoAnimEase);
                    m_titleLogoRect.DOLocalRotate(Vector3.zero, m_logoAnimDuration, RotateMode.FastBeyond360)
                        .SetEase(m_logoAnimEase)
                        .OnComplete(() => StartFloatingAnimation(originalPos));
                    break;

                case TitleLogoAnimType.ScalePopIn:
                    m_titleLogoRect.localScale = Vector3.zero;
                    m_titleLogoRect.DOScale(Vector3.one, m_logoAnimDuration)
                        .SetEase(m_logoAnimEase)
                        .OnComplete(() => StartFloatingAnimation(originalPos));
                    break;

                case TitleLogoAnimType.None:
                default:
                    StartFloatingAnimation(originalPos);
                    break;
            }
        }

        /// <summary>
        /// [기능]: 등장 애니메이션이 완전히 끝난 뒤, 로고 이미지에 부드럽게 위아래로 흔들리는 플로팅 연출을 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 등장 애니메이션 종료 콜백과 연계하여 둥둥 뜨는 요요 애니메이션 구현
        /// </summary>
        private void StartFloatingAnimation(Vector2 anchorPos)
        {
            if (!m_useFloatingEffect || m_titleLogoRect == null)
            {
                return;
            }

            // 이전 트윈 중복 제거 및 초기 위치 재싱크
            m_titleLogoRect.DOKill();
            m_titleLogoRect.anchoredPosition = anchorPos;

            m_titleLogoRect.DOAnchorPosY(anchorPos.y + m_floatingAmplitude, m_floatingDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// [기능]: 타이틀 배경 구름들을 무한 스크롤하고 WebGL 해상도 경계 이탈 시 꼬리 물기 정밀 시프팅 처리합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void ScrollClouds()
        {
            if (m_cloudRects == null || m_canvasRect == null)
            {
                return;
            }

            float halfParentWidth = m_canvasRect.rect.width * 0.5f;
            int cloudCount = m_cloudRects.Length;

            for (int i = 0; i < cloudCount; i++)
            {
                RectTransform cloud = m_cloudRects[i];
                if (cloud == null)
                {
                    continue;
                }

                // 1. 왼쪽으로 스크롤 이동
                float movement = m_cloudScrollSpeed * Time.deltaTime;
                cloud.anchoredPosition -= new Vector2(movement, 0f);

                // 2. 동적 해상도 바운더리 체크 (완전히 화면 밖으로 이탈 시 반대편 배치)
                float halfSelfWidth = cloud.rect.width * 0.5f;
                float wrapLimit = halfParentWidth + halfSelfWidth;

                if (cloud.anchoredPosition.x <= -wrapLimit)
                {
                    // 프레임 오버플로우 오차까지 완벽 보정하기 위해 구름 총 너비 스팬만큼 정밀 시프트
                    float totalWidthSpan = cloud.rect.width * cloudCount;
                    cloud.anchoredPosition += new Vector2(totalWidthSpan, 0f);
                }
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
