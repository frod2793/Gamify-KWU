using System.Collections.Generic;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 게임의 전체 상태(카드 목록, 뒤집기 횟수, 맞춘 짝 수 등)를 보유하는 순수 C# 모델 클래스입니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardMatchModel
    {
        #region Private Fields
        private List<CardData> m_cards;
        private int m_flipCount;
        private int m_matchedPairs;
        private int m_totalPairs;
        private int? m_firstSelectedIndex;
        #endregion

        #region Properties
        /// <summary> 게임 보드에 배치된 카드 목록 (24장) </summary>
        public List<CardData> Cards => m_cards;

        /// <summary> 현재까지의 총 뒤집기 횟수 </summary>
        public int FlipCount
        {
            get => m_flipCount;
            set => m_flipCount = value;
        }

        /// <summary> 현재까지 맞춘 짝의 수 </summary>
        public int MatchedPairs
        {
            get => m_matchedPairs;
            set => m_matchedPairs = value;
        }

        /// <summary> 총 쌍의 수 (12) </summary>
        public int TotalPairs => m_totalPairs;

        /// <summary> 첫 번째로 선택한 카드의 인덱스 (null이면 아직 선택 안 함) </summary>
        public int? FirstSelectedIndex
        {
            get => m_firstSelectedIndex;
            set => m_firstSelectedIndex = value;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// [기능]: 카드 맞추기 게임 모델을 초기화합니다.
        /// [작성자]: 김지연
        /// </summary>
        /// <param name="cards">셔플된 카드 목록</param>
        /// <param name="totalPairs">총 쌍 수</param>
        public CardMatchModel(List<CardData> cards, int totalPairs)
        {
            m_cards = cards;
            m_totalPairs = totalPairs;
            m_flipCount = 0;
            m_matchedPairs = 0;
            m_firstSelectedIndex = null;
        }
        #endregion
    }
}
