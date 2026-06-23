using UnityEngine;
using UnityEngine.UI;

namespace GamifyKWU.UI.Title
{
    /// <summary>
    /// [기능]: uGUI Image 컴포넌트에 스프라이트 시퀀스를 지정하여 가볍게 루프 애니메이션을 돌리는 유틸리티 클래스입니다. (에디터 모드 작동 지원)
    /// [작성자]: 윤승종
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class UISpriteAnimator : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [SerializeField]
        [Tooltip("애니메이션을 재생할 스프라이트 시퀀스 목록입니다.")]
        private Sprite[] m_sprites;

        [SerializeField]
        [Tooltip("초당 프레임 수(FPS)입니다.")]
        private float m_fps = 10f;

        #endregion

        #region 내부 필드 (Private Fields)

        private Image m_image;
        private int m_currentIndex;
        private float m_timer;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Start()
        {
            m_image = GetComponent<Image>();
        }

        private void Update()
        {
            if (m_sprites == null || m_sprites.Length == 0 || m_image == null)
            {
                return;
            }

            m_timer += Time.deltaTime;
            float interval = 1f / m_fps;

            if (m_timer >= interval)
            {
                m_timer -= interval;
                m_currentIndex = (m_currentIndex + 1) % m_sprites.Length;
                
                if (m_sprites[m_currentIndex] != null)
                {
                    m_image.sprite = m_sprites[m_currentIndex];
                }
            }
        }

        #endregion
    }
}
