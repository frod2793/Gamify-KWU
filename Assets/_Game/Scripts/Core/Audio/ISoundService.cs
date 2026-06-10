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
        event Action OnPauseBGMRequested;
        event Action OnResumeBGMRequested;

        /// <summary>
        /// [기능]: 페이드 효과와 함께 BGM 재생이 요청되었을 때 발생합니다.
        /// [작성자]: 윤승종
        /// </summary>
        event Action<AudioClip, float> OnPlayBGMWithFadeRequested;

        /// <summary>
        /// [기능]: 페이드 효과와 함께 BGM 정지가 요청되었을 때 발생합니다.
        /// [작성자]: 윤승종
        /// </summary>
        event Action<float> OnStopBGMWithFadeRequested;

        void SetBgmVolume(float volume);
        void SetSfxVolume(float volume);
        void SetBgmMute(bool isMute);
        void SetSfxMute(bool isMute);

        UniTaskVoid PlayBGM(string clipPath);
        UniTaskVoid PlaySFX(string clipPath);
        void StopBGM();
        void PauseBGM();
        void ResumeBGM();

        /// <summary>
        /// [기능]: 특정 오디오 클립을 지정된 시간 동안 페이드인하며 BGM으로 재생합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 신규 생성
        /// </summary>
        UniTaskVoid PlayBGMWithFade(string clipPath, float duration = 1f);

        /// <summary>
        /// [기능]: 재생 중인 BGM을 지정된 시간 동안 페이드아웃하며 정지합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-10
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 신규 생성
        /// </summary>
        void StopBGMWithFade(float duration = 1f);
    }
}
