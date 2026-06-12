using System;
using GameArifiction.Player;

namespace GameArifiction.Map
{
    /// <summary>
    /// [기능]: 맵 모델과 뷰 사이의 통신 및 로직을 담당하는 뷰모델 클래스 (POCO)
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-13
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 포탈 뷰에서 미니게임 성적을 조회할 수 있도록 PlayerSO 연동 및 캡슐화
    /// </summary>
    public class MapViewModel
    {
        #region 내부 필드 (Private Fields)
        private readonly MapModel m_mapModel;
        private readonly PlayerSO m_playerSO;
        #endregion

        #region 이벤트 (Events)
        /// <summary>
        /// 맵이 변경되었을 때 발생하는 이벤트입니다. (새로운 맵 인덱스 전달)
        /// </summary>
        public event Action<int> OnMapChanged;
        #endregion

        #region 초기화 (Initialization)
        /// <summary>
        /// 생성자를 통해 맵 모델과 플레이어 데이터 모델을 주입받습니다.
        /// </summary>
        /// <param name="mapModel">주입할 맵 모델</param>
        /// <param name="playerSO">주입할 플레이어 데이터 에셋</param>
        public MapViewModel(MapModel mapModel, PlayerSO playerSO)
        {
            m_mapModel = mapModel;
            m_playerSO = playerSO;
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

        /// <summary>
        /// [기능]: 미니게임 ID를 기반으로 플레이어의 최고 성적을 조회하여 반환합니다. (ID 예외 보정 포함)
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-13
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        /// <param name="minigameId">조회할 미니게임 ID</param>
        /// <returns>최고 성적 등급</returns>
        public MinigameGrade GetMinigameGrade(string minigameId)
        {
            if (m_playerSO == null)
            {
                return MinigameGrade.None;
            }

            string targetId = minigameId;
            if (string.Equals(targetId, "ClawMachine", StringComparison.OrdinalIgnoreCase))
            {
                targetId = "CraneGame";
            }

            return m_playerSO.GetMinigameGrade(targetId);
        }
        #endregion
    }
}
