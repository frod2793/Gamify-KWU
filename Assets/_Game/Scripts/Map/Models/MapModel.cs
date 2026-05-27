using System;

namespace GameArifiction.Map
{
    /// <summary>
    /// [기능]: 맵 데이터를 관리하는 순수 POCO 데이터 모델 클래스
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-27
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 문서 표준 준수 및 헤더 기입 완료
    /// </summary>
    public class MapModel
    {
        #region 공개 프로퍼티 (Public Properties)
        /// <summary>
        /// 현재 활성화된 맵의 인덱스입니다.
        /// </summary>
        public int CurrentMapIndex { get; set; }
        #endregion
    }
}
