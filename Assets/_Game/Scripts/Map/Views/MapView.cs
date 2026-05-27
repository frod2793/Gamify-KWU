using System.Collections.Generic;
using UnityEngine;

namespace GameArifiction.Map
{
    /// <summary>
    /// [기능]: 전체 맵의 관리와 시각적 포탈 기반 맵 전환을 담당하는 메인 뷰 컴포넌트
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-27
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: foreach 이터레이터를 for 루프로 변환하여 GC 최적화 및 Allman 코드 표준 완비
    /// </summary>
    public class MapView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        private GameObject[] m_maps;

        [SerializeField]
        private Transform m_playerTransform;
        #endregion

        #region 내부 필드 (Private Fields)
        private MapViewModel m_viewModel;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
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
            if (m_viewModel != null)
            {
                m_viewModel.OnMapChanged += HandleMapChanged;
            }

            PortalView[] portals = Object.FindObjectsByType<PortalView>(FindObjectsSortMode.None);
            
            if (portals != null)
            {
                for (int i = 0; i < portals.Length; i++)
                {
                    if (portals[i] != null)
                    {
                        portals[i].OnPortalEntered += HandlePortalEntered;
                    }
                }
            }

            // 초기 맵 활성화
            HandleMapChanged(0);
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
                for (int i = 0; i < portals.Length; i++)
                {
                    if (portals[i] != null)
                    {
                        portals[i].OnPortalEntered -= HandlePortalEntered;
                    }
                }
            }
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// 포탈 진입 시 호출되어 맵 변경을 요청하고 플레이어 좌표를 이동시킵니다.
        /// </summary>
        private void HandlePortalEntered(int newIndex, Vector2 spawnPosition)
        {
            if (m_viewModel != null)
            {
                m_viewModel.ChangeMap(newIndex);
            }

            if (m_playerTransform != null)
            {
                m_playerTransform.position = spawnPosition;
            }
        }

        /// <summary>
        /// 맵 인덱스 변경 시 호출되어 실제 게임 오브젝트의 활성화 상태를 업데이트합니다.
        /// </summary>
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
        #endregion
    }
}
