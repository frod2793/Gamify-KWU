using UnityEngine;
using DG.Tweening;

namespace GameArifiction.Camera
{
    /// <summary>
    /// [기능]: DOTween의 DOShakePosition을 사용하여 카메라의 로컬 트랜스폼을 독립적으로 흔드는 컴포넌트
    /// [작성자]: 윤승종
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)

        private Transform m_cameraTransform;
        private Vector3 m_originalLocalPos;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Awake()
        {
            m_cameraTransform = transform;
            m_originalLocalPos = m_cameraTransform.localPosition;
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// [기능]: DOTween의 DOShakePosition을 사용하여 로컬 좌표를 흔듭니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Shake(float duration, float strength)
        {
            if (m_cameraTransform != null)
            {
                m_cameraTransform.DOKill(true); // 기존 트윈 강제 완료 및 복원
                m_cameraTransform.localPosition = m_originalLocalPos;

                // 2D 쉐이킹이므로 Z축 변위를 0으로 고정하여 흔듭니다.
                m_cameraTransform.DOShakePosition(duration, new Vector3(strength, strength, 0f), 10, 90f, false, true, ShakeRandomnessMode.Full)
                    .OnComplete(() =>
                    {
                        if (m_cameraTransform != null)
                        {
                            m_cameraTransform.localPosition = m_originalLocalPos;
                        }
                    });
            }
        }

        #endregion
    }
}
