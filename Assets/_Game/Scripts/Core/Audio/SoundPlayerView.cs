using UnityEngine;
using VContainer;
using System.Collections.Generic;

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
            }

            m_soundService = soundService;

            // 새로운 서비스 구독
            m_soundService.OnBgmVolumeChanged += HandleBgmVolumeChanged;
            m_soundService.OnSfxVolumeChanged += HandleSfxVolumeChanged;
            m_soundService.OnPlayBGMRequested += HandlePlayBGM;
            m_soundService.OnPlaySFXRequested += HandlePlaySFX;
            m_soundService.OnStopBGMRequested += HandleStopBGM;

            // 초기 볼륨 세팅
            HandleBgmVolumeChanged(m_soundService.Settings.IsMuted ? 0f : m_soundService.Settings.BgmVolume);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            
            // BGM 소스 초기화
            m_bgmSource = gameObject.AddComponent<AudioSource>();
            m_bgmSource.loop = true;
            m_bgmSource.playOnAwake = false;
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
            foreach (var sfx in m_sfxSources)
            {
                sfx.volume = volume;
            }
        }

        private void HandlePlayBGM(AudioClip clip)
        {
            if (m_bgmSource.clip == clip && m_bgmSource.isPlaying) return;
            m_bgmSource.clip = clip;
            m_bgmSource.Play();
        }

        private void HandleStopBGM()
        {
            m_bgmSource.Stop();
        }

        private void HandlePlaySFX(AudioClip clip)
        {
            AudioSource source = GetAvailableSfxSource();
            source.clip = clip;
            source.volume = m_soundService.Settings.IsMuted ? 0f : m_soundService.Settings.SfxVolume;
            source.Play();
        }

        private AudioSource GetAvailableSfxSource()
        {
            foreach (var sfx in m_sfxSources)
            {
                if (!sfx.isPlaying) return sfx;
            }

            var newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            m_sfxSources.Add(newSource);
            return newSource;
        }
    }
}
