using UnityEngine;
using DG.Tweening;

namespace GameArifiction.Camera
{
    /// <summary>
    /// [기능]: 메인 카메라가 지정된 대상을 부드럽게 추적하고, Z축 깊이 제어를 통해 줌인/줌아웃 연출을 제어하는 클래스
    /// [작성자]: 윤승종
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("기본 팔로우 설정")]
        [SerializeField] private Transform m_target;
        [SerializeField] private float m_smoothSpeed = 5f;
        [SerializeField] private Vector3 m_offset = new Vector3(0, 0, -10f);

        [Header("대사 카메라 줌 연출 설정")]
        [SerializeField]
        [Tooltip("대사 진행 시 카메라가 플레이어에게 다가갈 목표 Z 오프셋 거리입니다.")]
        private float m_dialogueZoomZ = -5.0f;

        [SerializeField]
        [Tooltip("카메라 줌인 및 줌아웃에 소요되는 시간(초)입니다.")]
        private float m_zoomDuration = 1.0f;

        [Header("카메라 경계 제한")]
        [SerializeField]
        [Tooltip("체크 시 카메라가 지정된 스프라이트 영역 바깥을 비추지 않도록 제한합니다.")]
        private bool m_useBoundaryLimits = false;

        [SerializeField]
        [Tooltip("카메라가 넘어가지 못하게 영역을 잡을 맵의 배경 SpriteRenderer입니다.")]
        private SpriteRenderer m_boundarySprite;

        #endregion

        #region 내부 필드 (Private Fields)

        private UnityEngine.Camera m_camera;
        private float m_originalZoomZ = -10f;
        private Tween m_zoomTween;

        #endregion

        #region 공개 프로퍼티 (Public Properties)

        /// <summary>
        /// [기능]: 메인 카메라 컴포넌트 인스턴스 반환 프로퍼티
        /// </summary>
        public UnityEngine.Camera MainCamera
        {
            get
            {
                return m_camera;
            }
        }

        /// <summary>
        /// [기능]: 카메라 줌 연출에 소요되는 지연 시간 값 반환 프로퍼티
        /// </summary>
        public float ZoomDuration
        {
            get
            {
                return m_zoomDuration;
            }
        }

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            m_camera = GetComponent<UnityEngine.Camera>();
            m_originalZoomZ = m_offset.z;
        }

        /// <summary>
        /// [기능]: 다른 모든 오브젝트의 이동 처리가 끝난 후 호출되어 지연 현상 방지 및 카메라 맵 이탈 방지 처리
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-30
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 카메라 종종 맵 밖 이탈 방지 경계 클램핑 로직 구현
        /// </summary>
        private void LateUpdate()
        {
            if (m_target != null)
            {
                // 부드러운 카메라 추적 처리
                Vector3 desiredPosition = m_target.position + m_offset;
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, m_smoothSpeed * Time.deltaTime);

                // 2D 카메라 해상도 비율 기반 경계 클램핑 연산
                if (m_useBoundaryLimits && m_boundarySprite != null && m_camera != null)
                {
                    Bounds bounds = m_boundarySprite.bounds;

                    // 카메라의 수직 절반 크기
                    float camHeight = m_camera.orthographicSize;
                    // 해상도 비율(Aspect)을 반영한 수평 절반 크기
                    float camWidth = camHeight * m_camera.aspect;

                    // 카메라가 비추어질 수 있는 최대/최소 안전 범위 클램프 좌표 산출
                    float minX = bounds.min.x + camWidth;
                    float maxX = bounds.max.x - camWidth;
                    float minY = bounds.min.y + camHeight;
                    float maxY = bounds.max.y - camHeight;

                    // 맵 폭이 카메라 너비보다 강제로 좁다면 중심 고정
                    if (minX > maxX)
                    {
                        smoothedPosition.x = bounds.center.x;
                    }
                    else
                    {
                        smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
                    }

                    // 맵 높이가 카메라 높이보다 강제로 좁다면 중심 고정
                    if (minY > maxY)
                    {
                        smoothedPosition.y = bounds.center.y;
                    }
                    else
                    {
                        smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
                    }
                }

                transform.position = smoothedPosition;
            }
        }

        private void OnDestroy()
        {
            if (m_zoomTween != null)
            {
                m_zoomTween.Kill();
            }
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 런타임에 추적 대상을 설정하는 메서드
        /// [작성자]: 윤승종
        /// </summary>
        public void SetTarget(Transform target)
        {
            m_target = target;
        }

        /// <summary>
        /// [기능]: 기본 설정값을 활용하여 카메라의 Z 깊이를 줌인하는 메서드
        /// [작성자]: 윤승종
        /// </summary>
        public void ZoomIn()
        {
            Zoom(m_dialogueZoomZ, m_zoomDuration);
        }

        /// <summary>
        /// [기능]: 기본 설정값을 활용하여 카메라의 Z 깊이를 원래 크기로 되돌리는 메서드
        /// [작성자]: 윤승종
        /// </summary>
        public void ZoomOut()
        {
            ResetZoom(m_zoomDuration);
        }

        /// <summary>
        /// [기능]: 지정된 Z 깊이와 시간으로 카메라의 오프셋 거리를 부드럽게 전환하는 메서드
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-28
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Z축 깊이 트위닝 시 구조체 직접 적극 할당으로 변경
        /// </summary>
        public void Zoom(float targetZ, float duration)
        {
            if (m_zoomTween != null)
            {
                m_zoomTween.Kill();
            }
            m_zoomTween = DOTween.To(() => m_offset.z, z => 
            {
                m_offset = new Vector3(m_offset.x, m_offset.y, z);
            }, targetZ, duration).SetEase(Ease.OutCubic);
        }

        /// <summary>
        /// [기능]: 카메라의 오프셋 Z 거리를 원본 초기 깊이로 되돌리는 메서드
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-28
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Z축 원래 깊이 복귀 시 구조체 직접 적극 할당으로 변경
        /// </summary>
        public void ResetZoom(float duration)
        {
            if (m_zoomTween != null)
            {
                m_zoomTween.Kill();
            }
            m_zoomTween = DOTween.To(() => m_offset.z, z => 
            {
                m_offset = new Vector3(m_offset.x, m_offset.y, z);
            }, m_originalZoomZ, duration).SetEase(Ease.OutCubic);
        }

        #endregion
    }
}
