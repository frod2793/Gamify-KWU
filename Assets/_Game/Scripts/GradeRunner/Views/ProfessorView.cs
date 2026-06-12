using DG.Tweening;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using GameArifiction.Core.Audio;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 플레이어를 향해 코드를 떨어뜨리는 교수 캐릭터의 뷰(View) 컴포넌트.
///         페이즈(Phase 1/2)에 따른 시각적 형상 교체 및 장애물 투사 위치 추적 이동 연출을 처리합니다.
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class ProfessorView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("페이즈별 교수님 비주얼")]
        [SerializeField]
        [Tooltip("1페이즈(기본/온화) 상태의 교수님 비주얼 게임오브젝트입니다.")]
        private GameObject m_phase1Visual;

        [SerializeField]
        [Tooltip("2페이즈 시작(분노 변신 연출) 상태의 교수님 비주얼 게임오브젝트입니다.")]
        private GameObject m_phase2StartVisual;

        [SerializeField]
        [Tooltip("2페이즈 실제 진행(열정/공격) 상태의 교수님 비주얼 게임오브젝트입니다.")]
        private GameObject m_phase2Visual;

        [Header("대사 UI 참조")]
        [SerializeField]
        [Tooltip("교수 대사를 출력할 말풍선 UI CanvasGroup 컴포넌트입니다.")]
        private CanvasGroup m_dialogueBubble;

        [SerializeField]
        [Tooltip("말풍선 내 텍스트를 출력할 TextMeshProUGUI 컴포넌트입니다.")]
        private TMPro.TextMeshProUGUI m_dialogueText;

        [Header("대사 연출 속도 설정")]
        [SerializeField]
        [Range(0.01f, 0.2f)]
        [Tooltip("타이핑 효과 시 글자 하나가 출력되는 시간 간격(초)입니다. 기본값 0.04초.")]
        private float m_typingSpeed = 0.04f;

        [SerializeField]
        [Range(0.5f, 5.0f)]
        [Tooltip("타이핑 완료 후 말풍선이 화면에 완전히 머무르는 시간(초)입니다. 기본값 1.5초.")]
        private float m_dialogueHoldDuration = 1.5f;

        [Header("대사 사운드 설정")]
        [SerializeField]
        [Tooltip("교수님 타이핑 대사 시 출력될 효과음 클립입니다.")]
        private AudioClip m_typewriterSound;

        [SerializeField]
        [Range(0.8f, 1.2f)]
        [Tooltip("타이핑 소리 재생 시의 최소 피치 범위입니다.")]
        private float m_minPitch = 0.95f;

        [SerializeField]
        [Range(0.8f, 1.2f)]
        [Tooltip("타이핑 소리 재생 시의 최대 피치 범위입니다.")]
        private float m_maxPitch = 1.05f;

        [Header("이동 연출 설정")]
        [SerializeField]
        [Tooltip("공격 지점으로 이동할 때의 도트윈 이징 방식입니다.")]
        private Ease m_moveEase = Ease.OutQuad;

        [SerializeField]
        [Tooltip("교수님이 좌우로 이동할 수 있는 X좌표 최소/최대 범위 제한 설정입니다.")]
        private Vector2 m_movementRangeX = new Vector2(-7.5f, 7.5f);

        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;
        private ISoundService m_soundService;
        private float m_startPositionX; // 게임 시작 시점의 최초 X좌표
        private float m_startPositionY; // 게임 시작 시점의 최초 Y좌표
        private AudioSource m_audioSource;

        private AudioClip m_shortImpactSound;
        private AudioClip m_longImpactSound;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Start()
        {
            m_startPositionX = transform.position.x; // 최초 스폰 당시 시작 X좌표 기록
            m_startPositionY = transform.position.y; // 최초 스폰 당시 시작 Y좌표 기록

            // 초기 비주얼 세팅: 말풍선 숨김 및 빈 화면으로 대기
            if (m_dialogueBubble != null)
            {
                m_dialogueBubble.gameObject.SetActive(false);
            }

            // 오디오 소스 컴포넌트 자동 바인딩 및 기본 옵션 설정
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
            m_audioSource.playOnAwake = false;
            m_audioSource.loop = false;
            m_audioSource.spatialBlend = 0f; // 2D 오디오 재생

            // 초기 볼륨 동기화
            UpdateAudioSourceVolume();

            // 사운드 리소스 로드 (사용자 지정 경로)
            m_shortImpactSound = Resources.Load<AudioClip>("코드 피하기 게임 브금, 효과음/쿠궁 짧음");
            m_longImpactSound = Resources.Load<AudioClip>("코드 피하기 게임 브금, 효과음/쿠궁 긺");

            // 시작 직후에는 빈 화면 대기 (IntroCutscene 진입 시 페이드인 등 연출 예정)
            SetVisualActiveOnly(null);
            Debug.Log("[ProfessorView] 교수님 공격 캐릭터 뷰 초기화 성공.");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            transform.DOKill();
            if (UnityEngine.Camera.main != null)
            {
                UnityEngine.Camera.main.DOKill();
            }
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: VContainer를 통해 뷰모델 및 사운드 서비스 의존성을 주입합니다.
        /// [작성자]: 윤승종
        /// </summary>
        [Inject]
        public void Construct(GradeRunnerViewModel viewModel, ISoundService soundService)
        {
            m_viewModel = viewModel;
            m_soundService = soundService;

            if (m_viewModel != null)
            {
                m_viewModel.OnPhaseChanged += HandlePhaseChanged;
                m_viewModel.OnIntroCutsceneStarted += HandleIntroCutscene;
                m_viewModel.OnPhase2CutsceneStarted += HandlePhase2Cutscene;
                m_viewModel.OnGameEndCutsceneStarted += HandleGameEndCutscene;
            }

            if (m_soundService != null)
            {
                m_soundService.OnSfxVolumeChanged += HandleSfxVolumeChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnPhaseChanged -= HandlePhaseChanged;
                m_viewModel.OnIntroCutsceneStarted -= HandleIntroCutscene;
                m_viewModel.OnPhase2CutsceneStarted -= HandlePhase2Cutscene;
                m_viewModel.OnGameEndCutsceneStarted -= HandleGameEndCutscene;
            }

            if (m_soundService != null)
            {
                m_soundService.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
            }
        }

        #endregion

        #region 공개 연출 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 스포너가 특정 X좌표에서 코드 장애물을 스폰하려 할 때 호출되며, 2페이즈에서만 작동하여 1페이즈의 무한대 무빙을 방해하지 않습니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_MoveTo(float targetX, float duration = 0.2f)
        {
            // 1페이즈 중에는 무한대 무빙을 방해하지 않기 위해 무시
            if (m_viewModel != null && m_viewModel.CurrentPhase == GradeRunnerPhase.Phase1)
            {
                return;
            }

            // 인스펙터의 좌우 X이동 한계 범위를 명확히 적용
            float clampedX = Mathf.Clamp(targetX, m_movementRangeX.x, m_movementRangeX.y);

            transform.DOKill();
            transform.DOMoveX(clampedX, duration).SetEase(m_moveEase);
        }

        #endregion

        #region 이벤트 핸들러 및 내부 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 도입부 컷씬 트리거 시 호출되며, 화면 흔들림과 시간 지연 후 교수가 위에서 등장(Fade-In)하며 대사를 출력합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleIntroCutscene()
        {
            HandleIntroCutsceneAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid HandleIntroCutsceneAsync(CancellationToken token)
        {
            // 1. 빈 화면 대기 보장
            SetVisualActiveOnly(null);

            // 2. 화면 흔들림(1초) + 짧은 쿠궁음
            if (m_shortImpactSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_shortImpactSound, m_soundService?.Settings?.SfxVolume ?? 1f);
            }
            if (UnityEngine.Camera.main != null)
            {
                UnityEngine.Camera.main.transform.DOShakePosition(1.0f, 0.5f, 10, 90f, false, true).ToUniTask().Forget();
            }

            // 3. 1초 흔들림 진행 + 1.5초 흔들림 멈춤 대기 = 총 2.5초 대기
            await UniTask.Delay(System.TimeSpan.FromSeconds(2.5f), cancellationToken: token);

            // 4. 교수 페이드 인 + 상단에서 하강 연출
            SetVisualActiveOnly(m_phase1Visual);
            
            // Y축 상단(+5)에서 기존 Y로 떨어짐
            transform.position = new Vector3(m_startPositionX, m_startPositionY + 5f, transform.position.z);
            var moveTween = transform.DOMoveY(m_startPositionY, 0.5f).SetEase(Ease.OutQuad);

            if (m_phase1Visual != null)
            {
                SpriteRenderer[] sprites = m_phase1Visual.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null)
                    {
                        Color c = sprites[i].color;
                        c.a = 0f;
                        sprites[i].color = c;
                        sprites[i].DOFade(1f, 0.5f);
                    }
                }
            }

            // 하강 완료 대기
            await moveTween.ToUniTask(cancellationToken: token);

            // 5. 착지 직후 흔들림(2초) + 긴 쿠궁음
            if (m_longImpactSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_longImpactSound, m_soundService?.Settings?.SfxVolume ?? 1f);
            }
            if (UnityEngine.Camera.main != null)
            {
                UnityEngine.Camera.main.transform.DOShakePosition(2.0f, 0.5f, 10, 90f, false, true).ToUniTask().Forget();
            }

            await UniTask.Delay(System.TimeSpan.FromSeconds(2.0f), cancellationToken: token);

            // 6. 대사 출력
            if (m_viewModel != null)
            {
                TypeDialogue(m_viewModel.IntroDialogue, () =>
                {
                    if (m_viewModel != null)
                    {
                        m_viewModel.CompleteIntroCutscene();
                    }
                });
            }
        }

        /// <summary>
        /// [기능]: 2페이즈 전환 컷씬 트리거 시 호출되며, 교수님 형상을 연출용(Phase 2 Start)으로 교체하고 분노 진동 후 대사를 치고 본 2페이즈 비주얼로 바꿉니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandlePhase2Cutscene()
        {
            // 모든 연출 스케일/이동 중지 후 즉시 최초 시작 X위치로 신속 이동
            transform.DOKill();
            transform.DOMoveX(m_startPositionX, 0.4f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                // 원위치 도착 완료 시점부터 본격 2페이즈 변신 연출 시작
                // 2페이즈 시작용 임시 연출 비주얼 활성화
                SetVisualActiveOnly(m_phase2StartVisual);

                // 위압감 넘치는 분노 진동 연출
                transform.DOShakePosition(0.6f, 0.4f, 15);
                transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.5f, 8, 1.0f);

                DOVirtual.DelayedCall(0.7f, () =>
                {
                    if (m_viewModel != null)
                    {
                        TypeDialogue(m_viewModel.Phase2Dialogue, () =>
                        {
                            // 대사가 끝나면 최종 2페이즈 공격형태 비주얼로 교체하고 게임 재개
                            SetVisualActiveOnly(m_phase2Visual);
                            if (m_viewModel != null)
                            {
                                m_viewModel.CompletePhase2Cutscene();
                            }
                        });
                    }
                });
            });
        }

        /// <summary>
        /// [기능]: 일반 페이즈 전환 이벤트가 호출될 때 시각 보완을 진행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandlePhaseChanged(GradeRunnerPhase phase)
        {
            if (phase == GradeRunnerPhase.Phase2)
            {
                // 변신 전환 효과 증대 Punch 연출
                transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.5f, 6, 1f);
            }
        }

        /// <summary>
        /// [기능]: 0초 도달 시 게임 종료 컷씬을 진행합니다. (분노 진동 -> 대사 출력 -> 위로 사라짐)
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleGameEndCutscene()
        {
            transform.DOKill();
            
            // 마지막 대사 때 연출용 변신 비주얼(Phase 2 Start)로 스위칭
            SetVisualActiveOnly(m_phase2StartVisual);

            // 위압감 넘치는 분노 진동 연출 (2페이즈 시작과 동일)
            transform.DOShakePosition(0.6f, 0.4f, 15);
            transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.5f, 8, 1.0f);

            DOVirtual.DelayedCall(0.7f, () =>
            {
                if (m_viewModel != null)
                {
                    TypeDialogue(m_viewModel.GameEndDialogue, () =>
                    {
                        // 대사 종료 후 위로 사라질 때 카메라 쉐이크 추가
                        if (UnityEngine.Camera.main != null)
                        {
                            UnityEngine.Camera.main.transform.DOShakePosition(1.0f, 0.3f, 10, 90f, false, true).ToUniTask().Forget();
                        }

                        transform.DOMoveY(m_startPositionY + 15f, 1.0f).SetEase(Ease.InQuad).OnComplete(() =>
                        {
                            if (m_viewModel != null)
                            {
                                m_viewModel.CompleteGameEndCutscene();
                            }
                        });
                    });
                }
            });
        }

        /// <summary>
        /// [기능]: 3가지 상태(1페이즈, 2페이즈 시작 연출, 2페이즈 실제 진행) 중 지정한 하나만 켜고 나머지는 끄는 안전한 전환 메서드입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void SetVisualActiveOnly(GameObject activeVisual)
        {
            if (m_phase1Visual != null)
            {
                m_phase1Visual.SetActive(m_phase1Visual == activeVisual);
            }
            if (m_phase2StartVisual != null)
            {
                m_phase2StartVisual.SetActive(m_phase2StartVisual == activeVisual);
            }
            if (m_phase2Visual != null)
            {
                m_phase2Visual.SetActive(m_phase2Visual == activeVisual);
            }
        }

        /// <summary>
        /// [기능]: 말풍선을 활성화하고 텍스트를 타자기 방식으로 쳐준 뒤 부드럽게 퇴출 후 콜백을 실행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void TypeDialogue(string text, System.Action onComplete)
        {
            TypeDialogueAsync(text, onComplete, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid TypeDialogueAsync(string text, System.Action onComplete, CancellationToken cancellationToken)
        {
            if (m_dialogueBubble != null)
            {
                m_dialogueBubble.gameObject.SetActive(true);
                m_dialogueBubble.alpha = 0f;
                m_dialogueBubble.DOFade(1f, 0.25f).ToUniTask().Forget();
            }

            if (m_dialogueText != null)
            {
                m_dialogueText.text = "";

                // 타자기 효과: 글자 단위로 차례차례 출력
                for (int i = 0; i <= text.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    m_dialogueText.text = text.Substring(0, i);

                    // 타이핑 소리 재생: 공백이 아닐 때만 유효 글자로 판단하여 박자 맞춰 1:1로 재생
                    if (i > 0 && i <= text.Length)
                    {
                        char currentChar = text[i - 1];
                        if (!char.IsWhiteSpace(currentChar))
                        {
                            PlayTypewriterSound();
                        }
                    }

                    await UniTask.Delay(System.TimeSpan.FromSeconds(m_typingSpeed), cancellationToken: cancellationToken).SuppressCancellationThrow();
                }

                // 다 출력된 후 설정된 대기 시간 동안 머무름
                await UniTask.Delay(System.TimeSpan.FromSeconds(m_dialogueHoldDuration), cancellationToken: cancellationToken).SuppressCancellationThrow();

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (m_dialogueBubble != null)
                {
                    m_dialogueBubble.DOFade(0f, 0.25f).OnComplete(() =>
                    {
                        m_dialogueBubble.gameObject.SetActive(false);
                        if (onComplete != null)
                        {
                            onComplete.Invoke();
                        }
                    }).ToUniTask().Forget();
                }
                else
                {
                    if (onComplete != null)
                    {
                        onComplete.Invoke();
                    }
                }
            }
            else
            {
                if (onComplete != null)
                {
                    onComplete.Invoke();
                }
            }
        }

        /// <summary>
        /// [기능]: 타이핑 효과음 클립을 미세한 피치 변조와 함께 즉시 재생합니다.
        ///         (PlayOneShot 대신 Play를 사용하여 이전 재생음을 끊고 1:1 박자를 맞춥니다)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이핑 연출 비트음 1:1 박자 동기화 재생 적용
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

        private void Update()
        {
            // 1페이즈 중 상단 무한대(∞) 궤도 무빙 처리
            if (m_viewModel != null && m_viewModel.CurrentState == GradeRunnerState.Playing && m_viewModel.CurrentPhase == GradeRunnerPhase.Phase1)
            {
                // 2초당 1회전 (t = Time.time * PI)
                float t = Time.time * Mathf.PI;
                
                // 가로 진폭 (좌우 이동 범위의 절반가량)
                float amplitudeX = (m_movementRangeX.y - m_movementRangeX.x) * 0.45f;
                // 세로 진폭
                float amplitudeY = 0.5f;

                float x = m_startPositionX + Mathf.Sin(t) * amplitudeX;
                float y = m_startPositionY + Mathf.Sin(t * 2f) * amplitudeY;

                transform.position = new Vector3(x, y, transform.position.z);
            }
        }

        #endregion
    }
}
