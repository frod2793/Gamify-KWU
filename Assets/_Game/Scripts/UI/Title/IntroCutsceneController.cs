using UnityEngine;
using System.Threading;
using System.Text;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using GameArifiction.Player;
using GameArifiction.Interaction;
using TMPro;

/// <summary>
/// [기능]: 최초 플레이 시 플레이어가 입구에서 시작 지점까지 걷고 말풍선 튜토리얼을 띄우는 인트로 연출 제어기입니다.
/// [작성자]: 윤승종
/// </summary>
namespace GamifyKWU.UI.Title
{
    public class IntroCutsceneController : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("연출 대상 참조")]
        [SerializeField]
        [Tooltip("씬 상에 존재하는 실제 제어 대상인 플레이어 뷰 컴포넌트입니다.")]
        private PlayerView m_playerView;

        [SerializeField]
        [Tooltip("인트로 연출 감상 여부 세션 상태를 기록/참조할 플레이어 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;

        [Header("좌표 설정")]
        [SerializeField]
        [Tooltip("컷씬 개시 시 캐릭터가 최초 텔레포트 스폰될 입구 지점의 트랜스폼 좌표입니다.")]
        private Transform m_entrancePoint;

        [SerializeField]
        [Tooltip("캐릭터가 걸어와 최종 정지하게 될 로비 시작 구역의 트랜스폼 좌표입니다.")]
        private Transform m_startPoint;

        [SerializeField]
        [Tooltip("목표 도달을 판정하는 절대 오차 임계치 반경(m)입니다. 이 거리 이내로 좁혀지면 정지합니다.")]
        private float m_arrivalThreshold = 0.1f;

        [SerializeField]
        [Tooltip("타이틀 트랜지션이 완전히 끝난 뒤, 인트로 걷기 연출이 구동되기 전까지의 짧은 정적인 지연 대기 시간(초)입니다.")]
        private float m_postTransitionDelay = 0.5f;

        [Header("말풍선 UI 참조")]
        [SerializeField]
        [Tooltip("캐릭터 머리 위에 띄워져 활성화될 캔버스 말풍선 패널 오브젝트입니다.")]
        private GameObject m_speechBubblePanel;

        [SerializeField]
        [Tooltip("말풍선 패널 내부에서 튜토리얼 워딩을 순차 출력할 텍스트메쉬 프로 컴포넌트입니다.")]
        private TextMeshProUGUI m_speechText;

        [Header("말풍선 위치 및 월드 변환 보정 설정")]
        [SerializeField]
        [Tooltip("월드 좌표를 UI 좌표로 변환할 때 사용할 씬 메인 카메라입니다. 미지정 시 Camera.main을 사용합니다.")]
        private Camera m_uiCamera;

        [SerializeField]
        [Tooltip("말풍선 UI RectTransform입니다. 앵커 및 좌표 추적을 위해 필수 지정합니다.")]
        private RectTransform m_speechBubbleRect;

        [SerializeField]
        [Tooltip("플레이어 캐릭터 머리 위에 띄우기 위한 월드 Y축 보정(Offset) 값입니다.")]
        private float m_worldOffsetY = 2.3f;

        [SerializeField]
        [Tooltip("UI 스크린 좌표로 매핑된 뒤의 UI 기준 픽셀 x, y 보정(Offset) 오프셋입니다.")]
        private Vector2 m_uiPixelOffset = Vector2.zero;

        [Header("타자 연출 설정")]
        [SerializeField]
        [Tooltip("텍스트 한 글자당 찍히는 타자 지연 속도(초)입니다.")]
        private float m_typingSpeed = 0.05f;

        #endregion

        #region 내부 필드 (Private Fields)

        private string[] m_tutorialTexts = new string[]
        {
            "아휴... 광운대학교 마스코트로서 어느덧 [XX]년...\n그냥 얼굴만 비추면 다 되는 줄 알았는데, 총장님께서 마스코트의 가치를 증명하라며 성적표를 받아오라고 하시네?\n\n그래서 여러 학과 중에 제일 재밌어 보이는 **'게임콘텐츠학과'**로 냉큼 달려왔지!\n와~ 보니까 신기하고 재밌어 보이는 미니게임들이 엄청 많은걸?\n\n조아써! 이 게임들을 전부 플레이하고, 아주 우수한 성적을 받아서 총장님께 당당하게 보여드리는 거야! 다들 우니를 도와줄 거지?"
        };
        private int m_currentTextIndex = 0;
        private bool m_isIntroRunning = false;
        private CancellationTokenSource m_cts;
        private CancellationTokenSource m_typingCts;
        private bool m_isTypingActive = false;
        private string m_fullTextOfCurrentPage = string.Empty;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Start()
        {
            if (m_speechBubblePanel != null)
            {
                m_speechBubblePanel.SetActive(false);
            }

            if (m_uiCamera == null)
            {
                m_uiCamera = Camera.main;
            }

            if (m_playerSO != null)
            {
                if (m_playerSO.IsIntroPlayed || m_playerSO.HasSavedPosition)
                {
                    Debug.Log("[IntroCutsceneController] 이미 인트로를 시청했거나 세션이 복귀 상태이므로 대기하지 않고 리턴합니다.");
                    return;
                }
            }
        }

        private void Update()
        {
            // 인트로 가동 중이면서 대화창(말풍선)이 켜진 상황일 때만 실시간 단축 키보드 입력 및 터치를 수신합니다.
            if (m_isIntroRunning && m_speechBubblePanel != null && m_speechBubblePanel.activeInHierarchy)
            {
                HandleInputDetection();
            }
        }

        private void LateUpdate()
        {
            if (m_isIntroRunning && m_speechBubblePanel != null && m_speechBubblePanel.activeInHierarchy)
            {
                UpdateBubblePosition();
            }
        }

        private void OnDestroy()
        {
            if (m_cts != null)
            {
                m_cts.Cancel();
                m_cts.Dispose();
            }
            if (m_typingCts != null)
            {
                m_typingCts.Cancel();
                m_typingCts.Dispose();
            }
        }

        #endregion

        #region 초기화 및 실행 (Initialization & Execution)

        /// <summary>
        /// [기능]: 타이틀 뷰의 이지 트랜지션 페이드 아웃(페이드 차단 완료) 시점에 호출되어 인트로 컷씬을 비동기 구동합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void StartIntroCutscene()
        {
            if (m_isIntroRunning)
            {
                return;
            }

            if (m_playerSO != null)
            {
                if (m_playerSO.IsIntroPlayed || m_playerSO.HasSavedPosition)
                {
                    return;
                }
            }

            m_cts = new CancellationTokenSource();
            PlayIntroSequenceAsync(m_cts.Token).Forget();
        }

        /// <summary>
        /// [기능]: 프레임 가상 입력을 주입해 플레이어 캐릭터 조작 시스템 루프를 완전히 활용한 자동 걷기 컷씬을 진행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private async UniTaskVoid PlayIntroSequenceAsync(CancellationToken token)
        {
            m_isIntroRunning = true;
            Debug.Log("[IntroCutsceneController] 조작 복사형 인트로 컷씬 시퀀스를 개시합니다.");

            SetInteractionUIActiveState(false);

            if (m_playerView == null)
            {
                m_playerView = FindFirstObjectByType<PlayerView>();
            }

            if (m_playerView == null)
            {
                Debug.LogError("[IntroCutsceneController] 씬에 PlayerView가 없어 인트로를 재생할 수 없습니다.");
                return;
            }

            // PlayerView의 Start() 초기화(InitializeMVVM)가 실행될 시간을 보장하기 위해 1프레임 대기합니다.
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            // 플레이어 조작 ViewModel 획득 (리플렉션 활용)
            var viewModelField = typeof(PlayerView).GetField("m_viewModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            PlayerViewModel playerVM = viewModelField?.GetValue(m_playerView) as PlayerViewModel;

            if (playerVM == null)
            {
                Debug.LogError("[IntroCutsceneController] PlayerViewModel을 가져올 수 없습니다.");
                return;
            }

            // 1. 조작 입력 잠금 & 입구 포인트로 즉각 이동(텔레포트)
            playerVM.SetInputLocked(true);
            if (m_entrancePoint != null)
            {
                playerVM.ForceSetPosition(m_entrancePoint.position);
                m_playerView.transform.position = m_entrancePoint.position;
            }

            // 트랜지션 페이드 아웃 효과 완료 후 화면이 깨끗하게 복구될 때까지 설정한 시간(m_postTransitionDelay)만큼 정적인 대기를 수행합니다.
            if (m_postTransitionDelay > 0f)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(m_postTransitionDelay), cancellationToken: token);
            }

            // 2. 가상 입력 피딩 이동 루프 실행
            if (m_startPoint != null)
            {
                Vector2 targetPos = m_startPoint.position;

                while (Vector2.Distance(m_playerView.transform.position, targetPos) > m_arrivalThreshold)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    // 목표 지점으로 향하는 가상 방향 입력 벡터(정규화) 계산
                    Vector2 currentPos = m_playerView.transform.position;
                    Vector2 direction = (targetPos - currentPos).normalized;

                    // 실제 조작과 정확하게 일치하도록 ProcessInput 프레임 주입 실행
                    playerVM.ProcessInput(direction, Time.deltaTime);

                    // 다음 프레임까지 양보 대기
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }

            // 3. 목적지 도착 후 가상 입력 zero를 주입해 정지 애니메이션(IDLE) 자동 복원
            playerVM.ProcessInput(Vector2.zero, Time.deltaTime);

            // 4. 말풍선 튜토리얼 텍스트 팝업 개시
            m_currentTextIndex = 0;
            ShowSpeechBubble();
        }

        #endregion

        #region 말풍선 및 타자 연출 제어 (Speech Bubble & Typing Effect Control)

        private void UpdateBubblePosition()
        {
            if (m_playerView == null || m_speechBubbleRect == null)
            {
                return;
            }

            if (m_uiCamera == null)
            {
                m_uiCamera = Camera.main;
            }

            if (m_uiCamera == null)
            {
                return;
            }

            Vector3 worldPos = m_playerView.transform.position;
            worldPos.y += m_worldOffsetY;

            Vector2 screenPoint = m_uiCamera.WorldToScreenPoint(worldPos);

            RectTransform canvasRect = m_speechBubbleRect.parent as RectTransform;
            if (canvasRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint))
                {
                    m_speechBubbleRect.anchoredPosition = localPoint + m_uiPixelOffset;
                }
            }
        }

        private void ShowSpeechBubble()
        {
            if (m_speechBubblePanel != null && m_speechText != null)
            {
                m_speechBubblePanel.SetActive(true);
                UpdateBubblePosition();
                
                m_speechBubblePanel.transform.localScale = Vector3.zero;
                m_speechBubblePanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

                m_fullTextOfCurrentPage = m_tutorialTexts[m_currentTextIndex];
                StartTypingEffect(m_fullTextOfCurrentPage).Forget();
            }
        }

        private async UniTaskVoid StartTypingEffect(string fullText)
        {
            if (m_typingCts != null)
            {
                m_typingCts.Cancel();
                m_typingCts.Dispose();
            }

            m_typingCts = new CancellationTokenSource();
            CancellationToken token = m_typingCts.Token;

            m_isTypingActive = true;
            m_speechText.text = "";

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < fullText.Length; i++)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                builder.Append(fullText[i]);
                m_speechText.text = builder.ToString();

                await UniTask.Delay(System.TimeSpan.FromSeconds(m_typingSpeed), cancellationToken: token);
            }

            m_isTypingActive = false;
        }

        /// <summary>
        /// [기능]: 사용자가 말풍선 패널 또는 클릭 버튼을 누르면 다음 텍스트로 넘기거나 연출을 끝마칩니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnNextSpeechBubbleClicked()
        {
            if (!m_isIntroRunning)
            {
                return;
            }

            if (m_isTypingActive)
            {
                if (m_typingCts != null)
                {
                    m_typingCts.Cancel();
                }
                m_isTypingActive = false;
                m_speechText.text = m_fullTextOfCurrentPage;
                return;
            }

            m_currentTextIndex++;

            if (m_currentTextIndex < m_tutorialTexts.Length)
            {
                m_fullTextOfCurrentPage = m_tutorialTexts[m_currentTextIndex];
                StartTypingEffect(m_fullTextOfCurrentPage).Forget();
            }
            else
            {
                FinishIntroSequence();
            }
        }

        private void FinishIntroSequence()
        {
            m_isIntroRunning = false;

            if (m_speechBubblePanel != null)
            {
                m_speechBubblePanel.transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => m_speechBubblePanel.SetActive(false));
            }

            if (m_playerView != null)
            {
                var viewModelField = typeof(PlayerView).GetField("m_viewModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                PlayerViewModel playerVM = viewModelField?.GetValue(m_playerView) as PlayerViewModel;

                if (playerVM != null)
                {
                    playerVM.SetInputLocked(false);
                }
            }

            if (m_playerSO != null)
            {
                m_playerSO.IsIntroPlayed = true;
            }

            SetInteractionUIActiveState(true);

            Debug.Log("[IntroCutsceneController] 인트로 연출이 완전히 완료되었습니다. 플레이어 자유 조작 모드 개시.");
        }

        #endregion

        #region 디바이스 입력 및 모바일 터치 감지 (Device Input & Touch Detection)

        /// <summary>
        /// [기능]: PC WebGL 환경에서의 키보드 임의 키 누름 또는 모바일 환경에서의 화면 터치를 실시간 감지하여 대화창 클릭 명령으로 라우팅합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleInputDetection()
        {
            // 1. PC WebGL 환경용 키보드 입력 감지 (Space, Enter, F 키 또는 임의의 키 누름)
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.anyKey.wasPressedThisFrame)
                {
                    Debug.Log("[IntroCutsceneController] 키보드 임의 키 누름 감지 -> 대화 단계를 실행합니다.");
                    func_OnNextSpeechBubbleClicked();
                    return;
                }
            }

            // 2. 모바일/터치 지원 환경용 터치 입력 감지 (화면 터치 감지)
            var touchScreen = UnityEngine.InputSystem.Touchscreen.current;
            if (touchScreen != null && touchScreen.touches.Count > 0)
            {
                if (touchScreen.touches[0].press.wasPressedThisFrame)
                {
                    Debug.Log("[IntroCutsceneController] 모바일 화면 터치 입력 감지 -> 대화 단계를 실행합니다.");
                    func_OnNextSpeechBubbleClicked();
                    return;
                }
            }

            // 마우스 클릭 폴백 감지 (에디터 테스트 및 마우스 환경 호환)
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                // UI 버튼 자체 영역이 아닌 다른 화면 임의 클릭 시에도 라우팅
                Debug.Log("[IntroCutsceneController] 마우스 왼쪽 클릭 입력 감지 -> 대화 단계를 실행합니다.");
                func_OnNextSpeechBubbleClicked();
            }
        }

        #endregion

        #region 상호작용 UI 제어 헬퍼 (Interaction UI Helper)

        private void SetInteractionUIActiveState(bool isActive)
        {
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.SetInteractionUIActive(isActive);
                Debug.Log($"[IntroCutsceneController] UIManager를 통해 상호작용 UI 활성화 값을 {isActive}(으)로 싱크 적용했습니다.");
            }
        }

        #endregion
    }
}
