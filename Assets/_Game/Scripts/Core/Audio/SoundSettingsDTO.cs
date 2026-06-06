namespace GameArifiction.Core.Audio
{
    /// <summary>
    /// [기능]: 사운드 설정 정보(BGM, SFX 볼륨 및 음소거 여부)를 담는 순수 데이터 객체(DTO)
    /// [작성자]: 윤승종
    /// </summary>
    public class SoundSettingsDTO
    {
        public float BgmVolume { get; set; } = 1f;
        public float SfxVolume { get; set; } = 1f;
        public bool IsMuted { get; set; } = false;
    }
}
