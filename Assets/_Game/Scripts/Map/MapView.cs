using System.Collections.Generic;
using UnityEngine;

namespace GameArifiction.Map
{
    /// <summary>
    /// 전체 맵의 관리와 시각적 전환을 담당하는 메인 뷰 컴포넌트입니다.
    /// 작성자: [Gemini CLI / Lead Client Developer]
    /// </summary>
    public class MapView : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] m_maps;

        [SerializeField]
        private Transform m_playerTransform;

        private MapViewModel m_viewModel;

        /// <summary>
        /// 뷰모델을 초기화합니다.
        /// </summary>
        private void Awake()
        {
            MapModel model = new MapModel();
            m_viewModel = new MapViewModel(model);
        }

        /// <summary>
        /// 씬 내의 모든 포탈을 탐색하여 이벤트를 구독하고 초기 맵을 활성화합니다.
        /// </summary>
        private void Start()
        {
            m_viewModel.OnMapChanged += HandleMapChanged;

            PortalView[] portals = Object.FindObjectsByType<PortalView>(FindObjectsSortMode.None);
            
            if (portals != null)
            {
                foreach (PortalView portal in portals)
                {
                    if (portal != null)
                    {
                        portal.OnPortalEntered += HandlePortalEntered;
                    }
                }
            }

            // 초기 맵 활성화
            HandleMapChanged(0);
        }

        /// <summary>
        /// 포탈 진입 시 호출되어 맵 변경을 요청하고 플레이어 좌표를 이동시킵니다.
        /// </summary>
        /// <param name="newIndex">이동할 맵 인덱스</param>
        /// <param name="spawnPosition">플레이어가 생성될 좌표</param>
        private void HandlePortalEntered(int newIndex, Vector2 spawnPosition)
        {
            m_viewModel.ChangeMap(newIndex);

            if (m_playerTransform != null)
            {
                m_playerTransform.position = spawnPosition;
            }
        }

        /// <summary>
        /// 맵 인덱스 변경 시 호출되어 실제 게임 오브젝트의 활성화 상태를 업데이트합니다.
        /// </summary>
        /// <param name="newIndex">활성화할 맵 인덱스</param>
        private void HandleMapChanged(int newIndex)
        {
            if (m_maps == null)
            {
                return;
            }

            for (int i = 0; i < m_maps.Length; i++)
            {
                if (m_maps[i] != null)
                {
                    m_maps[i].SetActive(i == newIndex);
                }
            }
        }

        /// <summary>
        /// 객체 파괴 시 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnMapChanged -= HandleMapChanged;
            }

            PortalView[] portals = Object.FindObjectsByType<PortalView>(FindObjectsSortMode.None);
            
            if (portals != null)
            {
                foreach (PortalView portal in portals)
                {
                    if (portal != null)
                    {
                        portal.OnPortalEntered -= HandlePortalEntered;
                    }
                }
            }
        }
    }
}
