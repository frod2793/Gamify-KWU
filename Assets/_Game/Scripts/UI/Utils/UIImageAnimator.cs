using UnityEngine;
using UnityEngine.UI;

namespace GamifyKWU.UI.Utils
{
    /// <summary>
    /// [기능]: UI Image 컴포넌트의 Sprite를 배열 순서대로 교체하여 스프라이트 시트 애니메이션 효과를 구현하는 유틸리티 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIImageAnimator : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)
        [Header("애니메이션 설정")]
        [SerializeField]
        [Tooltip("애니메이션이 적용될 대상 UI Image 컴포넌트입니다. 미지정 시 스스로를 탐색합니다.")]
        private Image m_targetImage;

        [SerializeField]
        [Tooltip("순차적으로 재생할 스프라이트 프레임 배열입니다.")]
        private Sprite[] m_sprites;

        [SerializeField]
        [Tooltip("프레임이 전환되는 간격 시간(초 단위)입니다.")]
        private float m_frameDuration = 0.1f;

        [SerializeField]
        [Tooltip("체크 시 애니메이션을 무한히 반복하여 재생합니다.")]
        private bool m_loop = true;

        [SerializeField]
        [Tooltip("체크 시 활성화되는 즉시 애니메이션 재생을 시작합니다.")]
        private bool m_playOnAwake = true;

        private int m_currentFrameIndex = 0;
        private float m_timer = 0f;
        private bool m_isPlaying = false;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            if (m_targetImage == null)
            {
                m_targetImage = GetComponent<Image>();
            }
        }

        private void Start()
        {
            if (m_playOnAwake)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!m_isPlaying)
            {
                return;
            }

            if (m_sprites == null || m_sprites.Length == 0)
            {
                return;
            }

            m_timer += Time.deltaTime;
            if (m_timer >= m_frameDuration)
            {
                m_timer -= m_frameDuration;
                AdvanceFrame();
            }
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 애니메이션 재생을 시작합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-23
        /// </summary>
        public void Play()
        {
            m_isPlaying = true;
            m_timer = 0f;
            m_currentFrameIndex = 0;
            UpdateSprite();
        }

        /// <summary>
        /// [기능]: 애니메이션 재생을 일시 정지하거나 중단합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-23
        /// </summary>
        public void Stop()
        {
            m_isPlaying = false;
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        private void AdvanceFrame()
        {
            m_currentFrameIndex++;
            if (m_currentFrameIndex >= m_sprites.Length)
            {
                if (m_loop)
                {
                    m_currentFrameIndex = 0;
                }
                else
                {
                    m_currentFrameIndex = m_sprites.Length - 1;
                    m_isPlaying = false;
                }
            }

            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (m_targetImage != null && m_sprites != null && m_currentFrameIndex >= 0 && m_currentFrameIndex < m_sprites.Length)
            {
                m_targetImage.sprite = m_sprites[m_currentFrameIndex];
                m_targetImage.SetNativeSize();
            }
        }
        #endregion
    }
}
