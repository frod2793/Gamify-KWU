using UnityEngine;
using System.Threading;
using System.Text;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using GameArifiction.Player;
using GameArifiction.Interaction;
using TMPro;
using VContainer;
using GameArifiction.Core.Audio;

/// <summary>
/// [기능]: 최초 플레이 시 플레이어가 입구에서 시작 지점까지 걷고 말풍선 튜토리얼을 띄우는 인트로 연출 제어기입니다.
/// [작성자]: 윤승종
/// [수정 날짜]: 2026-06-12
/// [마지막 수정 작성자]: 윤승종
/// [수정 내용]: 빌드 컴파일 단계에서 ResetData 디버그 로직이 제외되도록 UNITY_EDITOR 전처리기 지시문 적용
/// </summary>
namespace GamifyKWU.UI.Title
{
    public class IntroCutsceneController : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("연출 대상 참조")]
        [SerializeField]
        [Inject]
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

        [SerializeField]
        [Tooltip("말풍선 UI RectTransform입니다. 앵커 및 좌표 추적을 위해 필수 지정합니다.")]
        private RectTransform m_speechBubbleRect;

        [SerializeField]
        [Tooltip("말풍선의 꼬리표를 기준으로 확장되고 위치가 동기화되도록 하는 피벗(Pivot) 값입니다. (예: 하단 중앙 = 0.5, 0)")]
        private Vector2 m_bubblePivot = new Vector2(0.5f, 0f);

        [SerializeField]
        [Tooltip("플레이어 캐릭터 머리 위에 띄우기 위한 월드 Y축 보정(Offset) 값입니다.")]
        private float m_worldOffsetY = 2.3f;

        [Header("타자 연출 설정")]
        [SerializeField]
        [Tooltip("텍스트 한 글자당 찍히는 타자 지연 속도(초)입니다.")]
        private float m_typingSpeed = 0.05f;

        [Header("대사 사운드 설정")]
        [SerializeField]
        [Tooltip("튜토리얼 타이핑 대사 시 출력될 효과음 클립입니다.")]
        private AudioClip m_typewriterSound;

        [SerializeField]
        [Range(0.8f, 1.2f)]
        [Tooltip("타이핑 소리 재생 시의 최소 피치 범위입니다.")]
        private float m_minPitch = 0.95f;

        [SerializeField]
        [Range(0.8f, 1.2f)]
        [Tooltip("타이핑 소리 재생 시의 최대 피치 범위입니다.")]
        private float m_maxPitch = 1.05f;

        [Header("디버그 옵션")]
        [SerializeField]
        [Tooltip("활성화 시, 시청 완료 기록 및 세션 복원 여부를 강제 우회하여 항상 인트로를 구동합니다.")]
        private bool m_forcePlayIntro = false;

        #endregion

        #region 내부 필드 (Private Fields)

        private string[] m_tutorialTexts = new string[]
        {
            "아휴... 광운대학교 마스코트로서 어느덧 [XX]년...\n그냥 얼굴만 비추면 다 되는 줄 알았는데,\n총장님께서 마스코트의 가치를 증명하라며\n성적표를 받아오라고 하시네?",
            "그래서 여러 학과 중에 제일 재밌어 보이는\n['게임콘텐츠학과']로 냉큼 달려왔지!\n와~ 보니까 신기하고 재밌어 보이는\n미니게임들이 엄청 많은걸?",
            "조아써! 이 게임들을 전부 플레이하고,\n아주 우수한 성적을 받아서\n총장님께 당당하게 보여드리는 거야!\n다들 우니를 도와줄 거지?"
        };
        private int m_currentTextIndex = 0;
        private bool m_isIntroRunning = false;
        private CancellationTokenSource m_cts;
        private CancellationTokenSource m_typingCts;
        private bool m_isTypingActive = false;
        private string m_fullTextOfCurrentPage = string.Empty;
        private GameArifiction.Camera.CameraFollow m_cameraFollow;
        private RectTransform m_canvasRect;
        private AudioSource m_audioSource;
        private ISoundService m_soundService;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        /// <summary>
        /// [기능]: 라이프사이클 초기화 및 디버그 상태 설정에 따른 데이터 초기화 처리
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: m_forcePlayIntro 옵션 활성화 상태일 시 플레이어 성적 및 세션 데이터(PlayerSO)도 강제 초기화 연동
        /// </summary>
        private void Start()
        {
            if (m_speechBubblePanel != null)
            {
                m_speechBubblePanel.SetActive(false);
            }

            if (m_cameraFollow == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    m_cameraFollow = mainCam.GetComponent<GameArifiction.Camera.CameraFollow>();
                }
            }

            // 오디오 소스 컴포넌트 자동 캐싱 및 기본 2D 오디오 재생 옵션 설정
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
            m_audioSource.playOnAwake = false;
            m_audioSource.loop = false;
            m_audioSource.spatialBlend = 0f;

            // 초기 볼륨 동기화
            UpdateAudioSourceVolume();

#if UNITY_EDITOR
            if (m_playerSO != null && m_forcePlayIntro)
            {
                m_playerSO.ResetData();
                Debug.Log("[IntroCutsceneController] 디버그 옵션 m_forcePlayIntro가 활성화되어 있어 플레이 기록(PlayerSO)을 초기화합니다.");
            }
#endif

            if (m_playerSO != null && !m_forcePlayIntro)
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
            if (m_soundService != null)
            {
                m_soundService.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
            }
        }

        #endregion

        #region 초기화 및 실행 (Initialization & Execution)

        /// <summary>
        /// [기능]: VContainer를 통해 전역 사운드 제어 서비스를 주입합니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public void Construct(ISoundService soundService)
        {
            m_soundService = soundService;
            if (m_soundService != null)
            {
                m_soundService.OnSfxVolumeChanged += HandleSfxVolumeChanged;
            }
        }

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

            if (m_playerSO != null && !m_forcePlayIntro)
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
                Debug.LogError("[IntroCutsceneController] 씬에 주입되거나 할당된 PlayerView가 없어 인트로를 재생할 수 없습니다.");
                return;
            }

            // PlayerView의 Start() 초기화(InitializeMVVM)가 실행될 시간을 보장하기 위해 1프레임 대기합니다.
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            // 플레이어 조작 ViewModel을 리플렉션 없이 안전하게 획득
            PlayerViewModel playerVM = m_playerView.GetViewModel();

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

            // 3.5. 대사 시작 전 카메라 줌인 연출 선행 개시 및 대기
            if (m_cameraFollow != null)
            {
                m_cameraFollow.ZoomIn();
                float delaySeconds = m_cameraFollow.ZoomDuration;
                if (delaySeconds > 0f)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);
                }
            }

            // 4. 말풍선 튜토리얼 텍스트 팝업 개시
            m_currentTextIndex = 0;
            ShowSpeechBubble();
        }

        #endregion

        #region 말풍선 및 타자 연출 제어 (Speech Bubble & Typing Effect Control)

        private void UpdateBubblePosition()
        {
            if (m_playerView == null || m_speechBubbleRect == null || m_cameraFollow == null)
            {
                return;
            }

            // [수정] 인스펙터에서의 피벗 값 수정을 실시간으로 반영하기 위해 매 프레임 피벗을 갱신합니다.
            m_speechBubbleRect.pivot = m_bubblePivot;

            var activeCamera = m_cameraFollow.MainCamera;
            if (activeCamera == null)
            {
                return;
            }

            Vector3 worldPos = m_playerView.transform.position;
            worldPos.y += m_worldOffsetY;

            Vector2 screenPoint = activeCamera.WorldToScreenPoint(worldPos);

            // [최적화] parent 캐스팅 오버헤드를 줄이기 위한 레이지 캐싱(Lazy Caching) 적용
            if (m_canvasRect == null)
            {
                m_canvasRect = m_speechBubbleRect.parent as RectTransform;
            }

            if (m_canvasRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvasRect, screenPoint, null, out localPoint))
                {
                    m_speechBubbleRect.anchoredPosition = localPoint;
                }
            }
        }

        private void ShowSpeechBubble()
        {
            if (m_speechBubblePanel != null && m_speechText != null)
            {
                m_speechBubblePanel.SetActive(true);
                UpdateBubblePosition();
                
                // [수정] 피벗(꼬리표)이 (0.5, 0)으로 실시간 갱신되는 RectTransform을 직접 스케일 제어합니다.
                if (m_speechBubbleRect != null)
                {
                    m_speechBubbleRect.localScale = Vector3.zero;
                    m_speechBubbleRect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
                else
                {
                    m_speechBubblePanel.transform.localScale = Vector3.zero;
                    m_speechBubblePanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }

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

                // 타이핑 소리 재생: 공백이 아닐 때만 유효 글자로 판단하여 박자 맞춰 1:1로 재생
                char currentChar = fullText[i];
                if (!char.IsWhiteSpace(currentChar))
                {
                    PlayTypewriterSound();
                }

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
                // [수정] 종료 시에도 설정된 피벗(꼬리표) 기준으로 축소하도록 RectTransform을 직접 제어합니다.
                if (m_speechBubbleRect != null)
                {
                    m_speechBubbleRect.DOScale(Vector3.zero, 0.2f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => m_speechBubblePanel.SetActive(false));
                }
                else
                {
                    m_speechBubblePanel.transform.DOScale(Vector3.zero, 0.2f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => m_speechBubblePanel.SetActive(false));
                }
            }

            if (m_cameraFollow != null)
            {
                m_cameraFollow.ZoomOut();
            }

            if (m_playerView != null)
            {
                PlayerViewModel playerVM = m_playerView.GetViewModel();

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

        #region 내부 헬퍼 및 사운드 제어 (Private Helpers & Sound Control)

        /// <summary>
        /// [기능]: 타이핑 효과음 클립을 미세한 피치 변조와 함께 즉시 재생합니다.
        ///         (PlayOneShot 대신 Play를 사용하여 이전 재생음을 끊고 1:1 박자를 맞춥니다)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 인트로 튜토리얼 텍스트 타이핑 효과음 1:1 박자 동기화 재생 적용
        /// </summary>
        private void PlayTypewriterSound()
        {
            if (m_audioSource != null && m_typewriterSound != null)
            {
                m_audioSource.pitch = UnityEngine.Random.Range(m_minPitch, m_maxPitch);
                m_audioSource.clip = m_typewriterSound;
                m_audioSource.Play();
            }
        }

        /// <summary>
        /// [기능]: 전역 SFX 볼륨 변경 시 로컬 오디오 소스의 볼륨을 즉각 갱신합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 볼륨 실시간 변경 핸들링 구현
        /// </summary>
        private void HandleSfxVolumeChanged(float volume)
        {
            if (m_audioSource != null)
            {
                m_audioSource.volume = volume;
            }
        }

        /// <summary>
        /// [기능]: SoundSettingsDTO의 초기 설정을 기반으로 로컬 오디오 소스의 볼륨을 동기화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 볼륨 및 Mute 여부 값 동기화 로직 구현
        /// </summary>
        private void UpdateAudioSourceVolume()
        {
            if (m_audioSource == null)
            {
                return;
            }

            if (m_soundService != null && m_soundService.Settings != null)
            {
                m_audioSource.volume = m_soundService.Settings.IsSfxMuted ? 0f : m_soundService.Settings.SfxVolume;
            }
            else
            {
                m_audioSource.volume = 1f;
            }
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
