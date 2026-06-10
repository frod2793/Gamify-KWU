using UnityEngine;
using UnityEngine.UI;
using VContainer;
using System.Collections.Generic;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace GameArifiction.Core.Audio
{
    /// <summary>
    /// [기능]: 씬 전환에도 파괴되지 않으며, SoundService의 이벤트를 구독하여 실제 소리를 출력하는 뷰 클래스
    /// [작성자]: 윤승종
    /// </summary>
    public class SoundPlayerView : MonoBehaviour
    {
        private ISoundService m_soundService;
        private AudioSource m_bgmSource;
        private List<AudioSource> m_sfxSources = new List<AudioSource>();
        private readonly HashSet<Button> m_boundButtons = new HashSet<Button>();

        [Inject]
        public void Construct(ISoundService soundService)
        {
            if (m_soundService != null)
            {
                // 이전 씬의 서비스 구독 해제
                m_soundService.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
                m_soundService.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
                m_soundService.OnPlayBGMRequested -= HandlePlayBGM;
                m_soundService.OnPlaySFXRequested -= HandlePlaySFX;
                m_soundService.OnStopBGMRequested -= HandleStopBGM;
                m_soundService.OnPauseBGMRequested -= HandlePauseBGM;
                m_soundService.OnResumeBGMRequested -= HandleResumeBGM;
                m_soundService.OnPlayBGMWithFadeRequested -= HandlePlayBGMWithFade;
                m_soundService.OnStopBGMWithFadeRequested -= HandleStopBGMWithFade;
            }

            m_soundService = soundService;

            // 새로운 서비스 구독
            m_soundService.OnBgmVolumeChanged += HandleBgmVolumeChanged;
            m_soundService.OnSfxVolumeChanged += HandleSfxVolumeChanged;
            m_soundService.OnPlayBGMRequested += HandlePlayBGM;
            m_soundService.OnPlaySFXRequested += HandlePlaySFX;
            m_soundService.OnStopBGMRequested += HandleStopBGM;
            m_soundService.OnPauseBGMRequested += HandlePauseBGM;
            m_soundService.OnResumeBGMRequested += HandleResumeBGM;
            m_soundService.OnPlayBGMWithFadeRequested += HandlePlayBGMWithFade;
            m_soundService.OnStopBGMWithFadeRequested += HandleStopBGMWithFade;

            // 초기 볼륨 세팅
            HandleBgmVolumeChanged(m_soundService.Settings.IsBgmMuted ? 0f : m_soundService.Settings.BgmVolume);
            Debug.Log($"[SoundPlayerView] Construct 완료. ISoundService 주입됨. 초기 볼륨: {m_soundService.Settings.BgmVolume}, 음소거: {m_soundService.Settings.IsBgmMuted}");
        }

        private void Awake()
        {
            // 중복 방지 자가 파괴 로직 (DontDestroyOnLoad 씬에 존재하는 기존 인스턴스 보존)
            SoundPlayerView[] instances = FindObjectsByType<SoundPlayerView>(FindObjectsSortMode.None);
            if (instances.Length > 1)
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    if (instances[i] != this && instances[i].gameObject.scene.name == "DontDestroyOnLoad")
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            DontDestroyOnLoad(gameObject);
            
            // BGM 소스 초기화
            m_bgmSource = gameObject.AddComponent<AudioSource>();
            m_bgmSource.loop = true;
            m_bgmSource.playOnAwake = false;
            m_bgmSource.spatialBlend = 0f; // BGM은 항상 2D로 재생 강제 지정
        }

        private void Start()
        {
            BindButtonSounds();
            StartAutoBindingLoop().Forget();
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            BindButtonSounds();
        }

        private void OnDestroy()
        {
            if (m_soundService != null)
            {
                m_soundService.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
                m_soundService.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
                m_soundService.OnPlayBGMRequested -= HandlePlayBGM;
                m_soundService.OnPlaySFXRequested -= HandlePlaySFX;
                m_soundService.OnStopBGMRequested -= HandleStopBGM;
                m_soundService.OnPauseBGMRequested -= HandlePauseBGM;
                m_soundService.OnResumeBGMRequested -= HandleResumeBGM;
                m_soundService.OnPlayBGMWithFadeRequested -= HandlePlayBGMWithFade;
                m_soundService.OnStopBGMWithFadeRequested -= HandleStopBGMWithFade;
            }
        }

        private void HandleBgmVolumeChanged(float volume)
        {
            if (m_bgmSource != null)
            {
                m_bgmSource.volume = volume;
            }
        }

        private void HandleSfxVolumeChanged(float volume)
        {
            for (int i = 0; i < m_sfxSources.Count; i++)
            {
                if (m_sfxSources[i] != null)
                {
                    m_sfxSources[i].volume = volume;
                }
            }
        }

        private void HandlePlayBGM(AudioClip clip)
        {
            if (m_bgmSource != null)
            {
                if (m_bgmSource.clip == clip && m_bgmSource.isPlaying)
                {
                    return;
                }

                m_bgmSource.DOKill();
                m_bgmSource.volume = m_soundService.Settings.IsBgmMuted ? 0f : m_soundService.Settings.BgmVolume;
                m_bgmSource.clip = clip;
                m_bgmSource.Play();
            }
        }

        private void HandleStopBGM()
        {
            if (m_bgmSource != null)
            {
                m_bgmSource.DOKill();
                m_bgmSource.Stop();
            }
        }

        /// <summary>
        /// [기능]: 지정된 BGM 오디오 클립을 페이드 효과와 함께 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 신규 구현
        /// </summary>
        private void HandlePlayBGMWithFade(AudioClip clip, float duration)
        {
            if (m_bgmSource == null)
            {
                Debug.LogWarning("[SoundPlayerView] m_bgmSource가 null이므로 BGM을 재생할 수 없습니다.");
                return;
            }

            float targetVolume = m_soundService.Settings.IsBgmMuted ? 0f : m_soundService.Settings.BgmVolume;
            Debug.Log($"[SoundPlayerView] HandlePlayBGMWithFade 호출됨. 클립: {clip.name}, 지속시간: {duration}s, 타겟볼륨: {targetVolume}");

            m_bgmSource.DOKill();

            if (m_bgmSource.clip == clip && m_bgmSource.isPlaying)
            {
                Debug.Log("[SoundPlayerView] 동일한 클립이 이미 재생 중입니다. 볼륨 페이드만 수행합니다.");
                m_bgmSource.DOFade(targetVolume, duration).SetUpdate(true);
                return;
            }

            if (m_bgmSource.isPlaying)
            {
                Debug.Log($"[SoundPlayerView] 다른 BGM이 재생 중입니다 ({m_bgmSource.clip.name}). 순차 페이드아웃 후 페이드인을 시작합니다.");
                float fadeOutTime = duration * 0.5f;
                float fadeInTime = duration * 0.5f;

                m_bgmSource.DOFade(0f, fadeOutTime).SetUpdate(true).OnComplete(() =>
                {
                    if (m_bgmSource != null)
                    {
                        m_bgmSource.clip = clip;
                        m_bgmSource.Play();
                        m_bgmSource.DOFade(targetVolume, fadeInTime).SetUpdate(true);
                        Debug.Log($"[SoundPlayerView] 이전 BGM 페이드아웃 완료. 새 BGM 재생 및 페이드인 시작: {clip.name}");
                    }
                });
            }
            else
            {
                Debug.Log($"[SoundPlayerView] 재생 중인 BGM이 없습니다. 바로 페이드인 시작: {clip.name}");
                m_bgmSource.volume = 0f;
                m_bgmSource.clip = clip;
                m_bgmSource.Play();
                m_bgmSource.DOFade(targetVolume, duration).SetUpdate(true);
            }
        }

        /// <summary>
        /// [기능]: 재생 중인 BGM을 페이드아웃하며 정지합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 신규 구현
        /// </summary>
        private void HandleStopBGMWithFade(float duration)
        {
            if (m_bgmSource == null)
            {
                return;
            }

            m_bgmSource.DOKill();

            if (m_bgmSource.isPlaying)
            {
                m_bgmSource.DOFade(0f, duration).SetUpdate(true).OnComplete(() =>
                {
                    if (m_bgmSource != null)
                    {
                        m_bgmSource.Stop();
                    }
                });
            }
        }

        private void HandlePauseBGM()
        {
            if (m_bgmSource != null)
            {
                m_bgmSource.Pause();
            }
        }

        private void HandleResumeBGM()
        {
            if (m_bgmSource != null)
            {
                m_bgmSource.UnPause();
            }
        }

        private void HandlePlaySFX(AudioClip clip)
        {
            AudioSource source = GetAvailableSfxSource();
            source.clip = clip;
            source.volume = m_soundService.Settings.IsSfxMuted ? 0f : m_soundService.Settings.SfxVolume;
            source.Play();
        }

        private AudioSource GetAvailableSfxSource()
        {
            for (int i = 0; i < m_sfxSources.Count; i++)
            {
                AudioSource sfx = m_sfxSources[i];
                if (sfx != null && !sfx.isPlaying)
                {
                    return sfx;
                }
            }

            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.spatialBlend = 0f; // SFX 소스도 2D로 기본 설정
            m_sfxSources.Add(newSource);
            return newSource;
        }

        #region 버튼 자동 바인딩 로직

        /// <summary>
        /// [기능]: 씬의 모든 UI 버튼을 탐색하여 중복이 없고 예외 컴포넌트가 없는 대상에 클릭 사운드를 연동합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void BindButtonSounds()
        {
            m_boundButtons.RemoveWhere(b => b == null);

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                if (btn == null)
                {
                    continue;
                }

                if (m_boundButtons.Contains(btn))
                {
                    continue;
                }

                // 제외 컴포넌트 검사
                if (btn.GetComponent<IgnoreAutoButtonSound>() != null)
                {
                    continue;
                }

                btn.onClick.AddListener(() => PlayButtonClickSound());
                m_boundButtons.Add(btn);
            }
        }

        private async UniTaskVoid StartAutoBindingLoop()
        {
            while (true)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f), cancellationToken: this.GetCancellationTokenOnDestroy());
                BindButtonSounds();
            }
        }

        private void PlayButtonClickSound()
        {
            if (m_soundService != null)
            {
                m_soundService.PlaySFX(SoundDefine.Dummy_click);
            }
        }

        #endregion
    }
}
