using System;
using UnityEngine;

namespace GameArifiction.Map
{
    /// <summary>
    /// 포탈의 충돌을 감지하고 이동할 맵 정보를 전달하는 뷰 컴포넌트입니다.
    /// 작성자: [Gemini CLI / Lead Client Developer]
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PortalView : MonoBehaviour
    {
        [SerializeField]
        private int m_targetMapIndex;

        [SerializeField]
        private Transform m_spawnPoint;

        /// <summary>
        /// 포탈에 진입했을 때 발생하는 이벤트입니다. (대상 맵 인덱스, 스폰 위치)
        /// </summary>
        public event Action<int, Vector2> OnPortalEntered;

        /// <summary>
        /// 트리거 충돌 시 플레이어 태그를 확인하여 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="other">충돌한 콜라이더</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                if (m_spawnPoint != null)
                {
                    OnPortalEntered?.Invoke(m_targetMapIndex, m_spawnPoint.position);
                }
                else
                {
                    OnPortalEntered?.Invoke(m_targetMapIndex, Vector2.zero);
                }
            }
        }
    }
}
