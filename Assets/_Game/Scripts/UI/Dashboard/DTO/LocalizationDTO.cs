using System;
using System.Collections.Generic;

namespace GamifyKWU.UI.Dashboard.DTO
{
    /// <summary>
    /// [기능]: 지식 그래프 노드의 중국어 데이터를 보관하는 DTO 클래스
    /// [작성자]: 윤승종
    /// </summary>
    [Serializable]
    public class KnowledgeNodeDTO
    {
        public string id;
        public string name;
        public string description;
    }

    /// <summary>
    /// [기능]: 지식 그래프 노드 리스트를 래핑하는 DTO 클래스
    /// [작성자]: 윤승종
    /// </summary>
    [Serializable]
    public class KnowledgeGraphDTO
    {
        public List<KnowledgeNodeDTO> nodes;
    }

    /// <summary>
    /// [기능]: 대시보드 UI 상의 텍스트 라벨 정보를 보관하는 DTO 클래스
    /// [작성자]: 윤승종
    /// </summary>
    [Serializable]
    public class DashboardUIDTO
    {
        public string title;
        public string labelStudentId;
        public string labelGpa;
        public string labelGameStats;
        public string labelPlayTime;
        public string labelRanking;
        public string btnRefresh;
        public string btnClose;
    }

    /// <summary>
    /// [기능]: 중국어 현지화 번역 문서 전체 데이터를 래핑하는 DTO 클래스
    /// [작성자]: 윤승종
    /// </summary>
    [Serializable]
    public class LocalizationDTO
    {
        public KnowledgeGraphDTO KnowledgeGraph;
        public DashboardUIDTO DashboardUI;
    }
}
