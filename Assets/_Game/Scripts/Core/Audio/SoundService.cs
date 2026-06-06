using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameArifiction.Core.Audio
{
    /// <summary>
    /// [기능]: 사운드 리소스 로드, 캐싱 및 재생 요청을 관리하는 순수 C# 비즈니스 로직 클래스
    /// [작성자]: 윤승종
    /// </summary>
    public class SoundService : ISoundService
    {
        public SoundSettingsDTO Settings { get; private set; }

        public event Action<float> OnBgmVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;
        
        public event Action<AudioClip> OnPlayBGMRequested;
        public event Action<AudioClip> OnPlaySFXRequested;
        public event Action OnStopBGMRequested;

        private Dictionary<string, AudioClip> m_clipCache = new Dictionary<string, AudioClip>();

        public SoundService()
        {
            Settings = new SoundSettingsDTO();
            LoadSettings();
        }

        private void LoadSettings()
        {
            Settings.BgmVolume = PlayerPrefs.GetFloat("BgmVolume", 1f);
            Settings.SfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1f);
            Settings.IsMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("BgmVolume", Settings.BgmVolume);
            PlayerPrefs.SetFloat("SfxVolume", Settings.SfxVolume);
            PlayerPrefs.SetInt("IsMuted", Settings.IsMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetBgmVolume(float volume)
        {
            Settings.BgmVolume = Mathf.Clamp01(volume);
            SaveSettings();
            OnBgmVolumeChanged?.Invoke(Settings.IsMuted ? 0f : Settings.BgmVolume);
        }

        public void SetSfxVolume(float volume)
        {
            Settings.SfxVolume = Mathf.Clamp01(volume);
            SaveSettings();
            OnSfxVolumeChanged?.Invoke(Settings.IsMuted ? 0f : Settings.SfxVolume);
        }

        public void SetMute(bool isMute)
        {
            Settings.IsMuted = isMute;
            SaveSettings();
            OnBgmVolumeChanged?.Invoke(isMute ? 0f : Settings.BgmVolume);
            OnSfxVolumeChanged?.Invoke(isMute ? 0f : Settings.SfxVolume);
        }

        public async UniTaskVoid PlayBGM(string clipPath)
        {
            var clip = await LoadClipAsync(clipPath);
            if (clip != null)
            {
                OnPlayBGMRequested?.Invoke(clip);
            }
        }

        public async UniTaskVoid PlaySFX(string clipPath)
        {
            var clip = await LoadClipAsync(clipPath);
            if (clip != null)
            {
                OnPlaySFXRequested?.Invoke(clip);
            }
        }

        public void StopBGM()
        {
            OnStopBGMRequested?.Invoke();
        }

        private async UniTask<AudioClip> LoadClipAsync(string path)
        {
            if (m_clipCache.TryGetValue(path, out var cachedClip))
            {
                return cachedClip;
            }

            var request = Resources.LoadAsync<AudioClip>(path);
            await request.ToUniTask();

            var clip = request.asset as AudioClip;
            if (clip != null)
            {
                m_clipCache[path] = clip;
                return clip;
            }
            else
            {
                Debug.LogWarning($"[SoundService] 오디오 리소스를 찾을 수 없습니다: {path}");
                return null;
            }
        }
    }
}
