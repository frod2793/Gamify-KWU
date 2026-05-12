using UnityEngine;

namespace GameArifiction.Camera
{
    /// <summary>
    /// [기능]: 메인 카메라가 지정된 대상을 부드럽게 추적하는 클래스
    /// [작성자]: [성함/팀명]
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("Settings")]
        [SerializeField] private Transform m_target;
        [SerializeField] private float m_smoothSpeed = 5f;
        [SerializeField] private Vector3 m_offset = new Vector3(0, 0, -10f);

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        /// <summary>
        /// [기능]: 다른 모든 오브젝트의 이동 처리가 끝난 후 호출되어 지연 현상 방지
        /// [작성자]: [성함/팀명]
        /// [수정 날짜]: 2026-05-11
        /// </summary>
        private void LateUpdate()
        {
            if (m_target != null)
            {
                // 부드러운 카메라 추적 처리
                Vector3 desiredPosition = m_target.position + m_offset;
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, m_smoothSpeed * Time.deltaTime);
                transform.position = smoothedPosition;
            }
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: 런타임에 추적 대상을 설정하는 메서드
        /// [작성자]: [성함/팀명]
        /// </summary>
        public void SetTarget(Transform target)
        {
            m_target = target;
        }

        #endregion
    }
}
