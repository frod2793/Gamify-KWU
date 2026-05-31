using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GamifyKWU.UI.Dashboard.DTO;
using GamifyKWU.UI.Dashboard.ViewModels;
using GamifyKWU.UI.Dashboard.Models;

namespace GamifyKWU.UI.Dashboard.Views
{
    /// <summary>
    /// [기능]: 중국어 지식 그래프 노드 설명 및 대시보드 UI를 시각화하는 뷰 컴포넌트
    /// [작성자]: 윤승종
    /// </summary>
    public class DashboardView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [Header("Dashboard UI Labels")]
        [SerializeField] private TMP_Text m_textTitle;
        [SerializeField] private TMP_Text m_textStudentIdLabel;
        [SerializeField] private TMP_Text m_textGpaLabel;
        [SerializeField] private TMP_Text m_textGameStatsLabel;
        [SerializeField] private TMP_Text m_textPlayTimeLabel;
        [SerializeField] private TMP_Text m_textRankingLabel;
        [SerializeField] private TMP_Text m_textBtnRefresh;
        [SerializeField] private TMP_Text m_textBtnClose;

        [Header("Knowledge Graph Nodes UI")]
        [SerializeField] private RectTransform m_nodesContainer;
        [SerializeField] private GameObject m_nodePrefab;
        #endregion

        #region 내부 필드 (Private Fields)
        private DashboardViewModel m_viewModel;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        /// <summary>
        /// [기능]: 에셋 리소스 로드 및 의존성 주입 초기화 수행
        /// [작성자]: 윤승종
        /// </summary>
        private void Awake()
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("Localization/zh_CN");
            if (jsonAsset != null)
            {
                DashboardModel model = new DashboardModel(jsonAsset.text);
                m_viewModel = new DashboardViewModel(model);
            }
            else
            {
                Debug.LogError("[DashboardView] zh_CN 번역 에셋 로드 실패!");
            }
        }

        /// <summary>
        /// [기능]: 뷰가 활성화될 때 뷰모델 이벤트 구독
        /// [작성자]: 윤승종
        /// </summary>
        private void OnEnable()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnLocalizationLoaded += UpdateUI;
                m_viewModel.OnDataRefreshed += HandleDataRefreshed;
            }
        }

        /// <summary>
        /// [기능]: 뷰가 비활성화될 때 뷰모델 이벤트 구독 해제 (메모리 누수 방지)
        /// [작성자]: 윤승종
        /// </summary>
        private void OnDisable()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnLocalizationLoaded -= UpdateUI;
                m_viewModel.OnDataRefreshed -= HandleDataRefreshed;
            }
        }

        /// <summary>
        /// [기능]: 첫 프레임 시작 시 번역 로드 트리거
        /// [작성자]: 윤승종
        /// </summary>
        private void Start()
        {
            if (m_viewModel != null)
            {
                m_viewModel.LoadLocalization();
            }
        }
        #endregion

        #region UI 이벤트 콜백 (Public Methods)
        /// <summary>
        /// [기능]: 새로고침 버튼 클릭 시 호출되는 핸들러 (Inspector 연결용)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        public void func_OnRefreshButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.RefreshData();
            }
        }

        /// <summary>
        /// [기능]: 닫기 버튼 클릭 시 호출되는 핸들러 (Inspector 연결용)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        public void func_OnCloseButtonClicked()
        {
            Debug.Log("[DashboardView] 대시보드 화면을 닫습니다.");
            gameObject.SetActive(false);
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 모델에서 로드된 번역 데이터를 기반으로 UI 요소를 갱신합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        private void UpdateUI(LocalizationDTO data)
        {
            if (data == null)
            {
                return;
            }

            // UI 텍스트 갱신 (Fake Null 방지 위해 if 명시적 널체크 사용)
            if (m_textTitle != null)
            {
                m_textTitle.text = data.DashboardUI.title;
            }
            if (m_textStudentIdLabel != null)
            {
                m_textStudentIdLabel.text = data.DashboardUI.labelStudentId;
            }
            if (m_textGpaLabel != null)
            {
                m_textGpaLabel.text = data.DashboardUI.labelGpa;
            }
            if (m_textGameStatsLabel != null)
            {
                m_textGameStatsLabel.text = data.DashboardUI.labelGameStats;
            }
            if (m_textPlayTimeLabel != null)
            {
                m_textPlayTimeLabel.text = data.DashboardUI.labelPlayTime;
            }
            if (m_textRankingLabel != null)
            {
                m_textRankingLabel.text = data.DashboardUI.labelRanking;
            }
            if (m_textBtnRefresh != null)
            {
                m_textBtnRefresh.text = data.DashboardUI.btnRefresh;
            }
            if (m_textBtnClose != null)
            {
                m_textBtnClose.text = data.DashboardUI.btnClose;
            }

            // 지식 그래프 노드 렌더링
            if (m_nodesContainer != null)
            {
                if (m_nodePrefab != null)
                {
                    // 기존 자식 노드 제거 (루프 최적화를 위해 for문 사용)
                    int childCount = m_nodesContainer.childCount;
                    for (int i = childCount - 1; i >= 0; i--)
                    {
                        Transform child = m_nodesContainer.GetChild(i);
                        if (child != null)
                        {
                            Destroy(child.gameObject);
                        }
                    }

                    // 중국어 노드 데이터 생성
                    List<KnowledgeNodeDTO> nodes = data.KnowledgeGraph.nodes;
                    if (nodes != null)
                    {
                        int nodeCount = nodes.Count;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            GameObject nodeObj = Instantiate(m_nodePrefab, m_nodesContainer);
                            if (nodeObj != null)
                            {
                                // 노드 뷰 세팅
                                TMP_Text textComponent = nodeObj.GetComponentInChildren<TMP_Text>();
                                if (textComponent != null)
                                {
                                    textComponent.text = $"<b>{nodes[i].name}</b>\n{nodes[i].description}";
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// [기능]: 새로고침 완료 이벤트를 처리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-31
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 초기 구현
        /// </summary>
        private void HandleDataRefreshed()
        {
            Debug.Log("[DashboardView] UI 상에서 대시보드 데이터 새로고침 연출을 반영했습니다.");
        }
        #endregion
    }
}
