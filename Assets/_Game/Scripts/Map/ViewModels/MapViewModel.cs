using System;

namespace GameArifiction.Map
{
    /// <summary>
    /// [기능]: 맵 모델과 뷰 사이의 통신 및 로직을 담당하는 뷰모델 클래스 (POCO)
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-27
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 문서 표준 준수 및 헤더 기입 완료
    /// </summary>
    public class MapViewModel
    {
        #region 내부 필드 (Private Fields)
        private readonly MapModel m_mapModel;
        #endregion

        #region 이벤트 (Events)
        /// <summary>
        /// 맵이 변경되었을 때 발생하는 이벤트입니다. (새로운 맵 인덱스 전달)
        /// </summary>
        public event Action<int> OnMapChanged;
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// 생성자를 통해 맵 모델을 주입받습니다.
        /// </summary>
        /// <param name="mapModel">주입할 맵 모델</param>
        public MapViewModel(MapModel mapModel)
        {
            m_mapModel = mapModel;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// 맵을 변경하고 관련 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="newIndex">새로운 맵 인덱스</param>
        public void ChangeMap(int newIndex)
        {
            if (m_mapModel.CurrentMapIndex == newIndex)
            {
                return;
            }

            m_mapModel.CurrentMapIndex = newIndex;
            OnMapChanged?.Invoke(newIndex);
        }
        #endregion
    }
}
