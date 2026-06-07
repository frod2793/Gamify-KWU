using System;
using GameArifiction.Core.Audio;
using UnityEngine;

namespace GameArifiction.UI.Common
{
    /// <summary>
    /// [기능]: 공통 설정 팝업(BGM, SFX 볼륨 및 음소거 조절)의 상태와 명령을 관리하는 ViewModel
    /// [작성자]: 윤승종
    /// </summary>
    public class CommonSettingsViewModel
    {
        private readonly ISoundService m_soundService;

        // 상태
        public int BgmVolumeLevel { get; private set; }
        public int SfxVolumeLevel { get; private set; }
        public bool IsBgmMuted { get; private set; }
        public bool IsSfxMuted { get; private set; }

        // 이벤트
        public event Action<int> OnBgmLevelChanged;
        public event Action<int> OnSfxLevelChanged;
        public event Action<bool> OnBgmMuteChanged;
        public event Action<bool> OnSfxMuteChanged;

        public CommonSettingsViewModel(ISoundService soundService)
        {
            m_soundService = soundService;
            Initialize();
        }

        private void Initialize()
        {
            var settings = m_soundService.Settings;
            BgmVolumeLevel = VolumeToLevel(settings.BgmVolume);
            SfxVolumeLevel = VolumeToLevel(settings.SfxVolume);
            IsBgmMuted = settings.IsBgmMuted;
            IsSfxMuted = settings.IsSfxMuted;
        }

        // BGM 볼륨 변경 커맨드
        public void SetBgmVolumeLevel(int level)
        {
            BgmVolumeLevel = Mathf.Clamp(level, 0, 4);
            float volume = LevelToVolume(BgmVolumeLevel);
            m_soundService.SetBgmVolume(volume);
            
            OnBgmLevelChanged?.Invoke(BgmVolumeLevel);
        }

        // SFX 볼륨 변경 커맨드
        public void SetSfxVolumeLevel(int level)
        {
            SfxVolumeLevel = Mathf.Clamp(level, 0, 4);
            float volume = LevelToVolume(SfxVolumeLevel);
            m_soundService.SetSfxVolume(volume);
            
            OnSfxLevelChanged?.Invoke(SfxVolumeLevel);
        }

        // BGM 음소거 토글 커맨드
        public void ToggleBgmMute()
        {
            IsBgmMuted = !IsBgmMuted;
            m_soundService.SetBgmMute(IsBgmMuted);
            OnBgmMuteChanged?.Invoke(IsBgmMuted);
        }

        // SFX 음소거 토글 커맨드
        public void ToggleSfxMute()
        {
            IsSfxMuted = !IsSfxMuted;
            m_soundService.SetSfxMute(IsSfxMuted);
            OnSfxMuteChanged?.Invoke(IsSfxMuted);
        }

        // 유틸리티 메서드
        private int VolumeToLevel(float volume)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(volume) * 4f);
        }

        private float LevelToVolume(int level)
        {
            return Mathf.Clamp(level, 0, 4) / 4f;
        }
    }
}
