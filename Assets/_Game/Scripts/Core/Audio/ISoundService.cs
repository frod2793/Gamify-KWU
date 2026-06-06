using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace GameArifiction.Core.Audio
{
    /// <summary>
    /// [기능]: 전역 사운드 제어 인터페이스
    /// [작성자]: 윤승종
    /// </summary>
    public interface ISoundService
    {
        SoundSettingsDTO Settings { get; }
        
        event Action<float> OnBgmVolumeChanged;
        event Action<float> OnSfxVolumeChanged;
        
        event Action<AudioClip> OnPlayBGMRequested;
        event Action<AudioClip> OnPlaySFXRequested;
        event Action OnStopBGMRequested;

        void SetBgmVolume(float volume);
        void SetSfxVolume(float volume);
        void SetMute(bool isMute);

        UniTaskVoid PlayBGM(string clipPath);
        UniTaskVoid PlaySFX(string clipPath);
        void StopBGM();
    }
}
