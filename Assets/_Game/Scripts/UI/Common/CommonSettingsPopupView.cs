using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace GameArifiction.UI.Common
{
    /// <summary>
    /// [기능]: 공통 설정 팝업(BGM, SFX 볼륨 조절 및 음소거 기능)을 표시하고 입력을 ViewModel에 전달하는 View
    /// [작성자]: 윤승종
    /// </summary>
    public class CommonSettingsPopupView : MonoBehaviour
    {
        #region 공개 이벤트

        /// <summary>
        /// [기능]: 설정 팝업이 닫힐 때 발생하는 이벤트입니다.
        /// </summary>
        public event Action OnClosePopup;

        #endregion

        #region UI 참조 (Inspector)

        [Header("BGM 설정")]
        [SerializeField]
        [Tooltip("BGM 볼륨 슬라이더 (0~4단계)")]
        private Slider m_bgmSlider;

        [SerializeField]
        [Tooltip("BGM 음소거 토글 버튼")]
        private Button m_bgmMuteButton;

        [SerializeField]
        [Tooltip("BGM 사운드 상태를 표시할 Image 컴포넌트")]
        private Image m_bgmSoundIconImage;

        [Header("SFX 설정")]
        [SerializeField]
        [Tooltip("SFX 볼륨 슬라이더 (0~4단계)")]
        private Slider m_sfxSlider;

        [SerializeField]
        [Tooltip("SFX 음소거 토글 버튼")]
        private Button m_sfxMuteButton;

        [SerializeField]
        [Tooltip("SFX 사운드 상태를 표시할 Image 컴포넌트")]
        private Image m_sfxSoundIconImage;

        [Header("공통 사운드 스프라이트 설정")]
        [SerializeField]
        [Tooltip("사운드 켜짐 상태 스프라이트")]
        private Sprite m_soundOnSprite;

        [SerializeField]
        [Tooltip("사운드 꺼짐 상태 스프라이트")]
        private Sprite m_soundOffSprite;

        [Header("조작 버튼")]
        [SerializeField]
        [Tooltip("설정 팝업을 닫는 확인 버튼")]
        private Button m_confirmButton;

        #endregion

        #region 내부 필드

        private CommonSettingsViewModel m_viewModel;

        #endregion

        #region 유니티 생명주기 및 의존성 주입

        [Inject]
        public void Construct(CommonSettingsViewModel viewModel)
        {
            m_viewModel = viewModel;

            // ViewModel 이벤트 구독
            m_viewModel.OnBgmLevelChanged += UpdateBgmSliderUI;
            m_viewModel.OnSfxLevelChanged += UpdateSfxSliderUI;
            m_viewModel.OnBgmMuteChanged += UpdateBgmMuteUI;
            m_viewModel.OnSfxMuteChanged += UpdateSfxMuteUI;

            // UI 이벤트 바인딩
            if (m_bgmSlider != null)
            {
                // 슬라이더 값이 변경될 때 ViewModel 업데이트 (소수점 없이 int로 사용하기 위함)
                m_bgmSlider.onValueChanged.AddListener(value => m_viewModel.SetBgmVolumeLevel(Mathf.RoundToInt(value)));
            }

            if (m_sfxSlider != null)
            {
                m_sfxSlider.onValueChanged.AddListener(value => m_viewModel.SetSfxVolumeLevel(Mathf.RoundToInt(value)));
            }

            if (m_bgmMuteButton != null)
            {
                m_bgmMuteButton.onClick.AddListener(func_OnBgmMuteClick);
            }

            if (m_sfxMuteButton != null)
            {
                m_sfxMuteButton.onClick.AddListener(func_OnSfxMuteClick);
            }

            if (m_confirmButton != null)
            {
                m_confirmButton.onClick.AddListener(func_OnConfirmClick);
            }

            // 초기 UI 상태 동기화 및 팝업 비활성화 (시작 시 숨김 처리)
            UpdateBgmSliderUI(m_viewModel.BgmVolumeLevel);
            UpdateSfxSliderUI(m_viewModel.SfxVolumeLevel);
            UpdateBgmMuteUI(m_viewModel.IsBgmMuted);
            UpdateSfxMuteUI(m_viewModel.IsSfxMuted);

            gameObject.SetActive(false);

            Debug.Log("[CommonSettingsPopupView] ViewModel 주입 및 이벤트 바인딩 완료");
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnBgmLevelChanged -= UpdateBgmSliderUI;
                m_viewModel.OnSfxLevelChanged -= UpdateSfxSliderUI;
                m_viewModel.OnBgmMuteChanged -= UpdateBgmMuteUI;
                m_viewModel.OnSfxMuteChanged -= UpdateSfxMuteUI;
            }

            if (m_bgmSlider != null) m_bgmSlider.onValueChanged.RemoveAllListeners();
            if (m_sfxSlider != null) m_sfxSlider.onValueChanged.RemoveAllListeners();
            if (m_bgmMuteButton != null) m_bgmMuteButton.onClick.RemoveListener(func_OnBgmMuteClick);
            if (m_sfxMuteButton != null) m_sfxMuteButton.onClick.RemoveListener(func_OnSfxMuteClick);
            if (m_confirmButton != null) m_confirmButton.onClick.RemoveListener(func_OnConfirmClick);
        }

        #endregion

        #region UI 업데이트 메서드

        private void UpdateBgmSliderUI(int level)
        {
            if (m_bgmSlider != null && Mathf.RoundToInt(m_bgmSlider.value) != level)
            {
                m_bgmSlider.value = level;
            }
        }

        private void UpdateSfxSliderUI(int level)
        {
            if (m_sfxSlider != null && Mathf.RoundToInt(m_sfxSlider.value) != level)
            {
                m_sfxSlider.value = level;
            }
        }

        /// <summary>
        /// [기능]: BGM 음소거 상태에 따라 사운드 아이콘 스프라이트를 업데이트합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-07
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 공통 사운드 스프라이트(m_soundOnSprite, m_soundOffSprite)를 사용하도록 수정
        /// </summary>
        private void UpdateBgmMuteUI(bool isMuted)
        {
            if (m_bgmSoundIconImage != null)
            {
                m_bgmSoundIconImage.sprite = isMuted ? m_soundOffSprite : m_soundOnSprite;
            }
        }

        /// <summary>
        /// [기능]: SFX 음소거 상태에 따라 사운드 아이콘 스프라이트를 업데이트합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-07
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 공통 사운드 스프라이트(m_soundOnSprite, m_soundOffSprite)를 사용하도록 수정
        /// </summary>
        private void UpdateSfxMuteUI(bool isMuted)
        {
            if (m_sfxSoundIconImage != null)
            {
                m_sfxSoundIconImage.sprite = isMuted ? m_soundOffSprite : m_soundOnSprite;
            }
        }

        #endregion

        #region UI 이벤트 핸들러

        public void func_OnBgmMuteClick()
        {
            m_viewModel?.ToggleBgmMute();
        }

        public void func_OnSfxMuteClick()
        {
            m_viewModel?.ToggleSfxMute();
        }

        public void func_OnConfirmClick()
        {
            Debug.Log("[CommonSettingsPopupView] 확인 버튼 클릭, 팝업 닫기");
            func_HidePopup();
            OnClosePopup?.Invoke();
        }

        #endregion

        #region 공개 메서드 (팝업 제어)

        /// <summary>
        /// [기능]: 팝업을 활성화하고 DOTween 애니메이션으로 노출합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: timeScale = 0f 상태에서도 팝업이 나타나도록 SetUpdate(true) 추가
        /// </summary>
        public void ShowPopup()
        {
            gameObject.SetActive(true);
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        /// <summary>
        /// [기능]: 팝업을 비활성화하는 애니메이션을 실행 후 닫습니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: timeScale = 0f 상태에서도 팝업이 닫히도록 SetUpdate(true) 추가
        /// </summary>
        public void func_HidePopup()
        {
            transform.DOKill();
            transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }

        #endregion
    }
}
