using UnityEngine;

namespace GamifyKWU.UI.Title
{
    /// <summary>
    /// [기능]: 에디터 편집 모드에서도 RenderTexture용 카메라를 강제로 렌더링하여 UI 상에 실시간으로 캐릭터가 표시되도록 지원하는 유틸리티 스크립트입니다.
    /// [작성자]: 윤승종
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class TitleRenderTextureUpdater : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)

        private Camera m_camera;
        private float m_lastRenderTime;
        private const float RENDER_INTERVAL = 0.05f; // 약 20 FPS 제한 (성능 최적화)

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Start()
        {
            m_camera = GetComponent<Camera>();
        }

        private void Update()
        {
            // 에디터 편집 모드(플레이 상태가 아님)일 때만 수동 렌더링 강제 실행
            if (!Application.isPlaying)
            {
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - m_lastRenderTime >= RENDER_INTERVAL)
                {
                    m_lastRenderTime = currentTime;
                    if (m_camera != null && m_camera.targetTexture != null)
                    {
                        m_camera.Render();
                    }
                }
            }
        }

        #endregion
    }
}
