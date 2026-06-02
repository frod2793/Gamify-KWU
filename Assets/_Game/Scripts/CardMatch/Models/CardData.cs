namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 게임에서 카드 1장의 상태 데이터를 보유하는 순수 C# 데이터 클래스입니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardData
    {
        #region Private Fields
        private int m_cardId;
        private int m_pairId;
        private bool m_isFlipped;
        private bool m_isMatched;
        #endregion

        #region Properties
        /// <summary> 카드 고유 식별자 (0 ~ 23) </summary>
        public int CardId => m_cardId;

        /// <summary> 짝 식별자 (0 ~ 11). 같은 PairId를 가진 카드 2장이 한 쌍 </summary>
        public int PairId => m_pairId;

        /// <summary> 현재 앞면이 보이는 상태인지 여부 </summary>
        public bool IsFlipped
        {
            get => m_isFlipped;
            set => m_isFlipped = value;
        }

        /// <summary> 매칭이 완료된 카드인지 여부 </summary>
        public bool IsMatched
        {
            get => m_isMatched;
            set => m_isMatched = value;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// [기능]: 카드 데이터를 초기화합니다.
        /// [작성자]: 김지연
        /// </summary>
        /// <param name="cardId">카드 고유 ID</param>
        /// <param name="pairId">짝 ID (같은 그림끼리 동일)</param>
        public CardData(int cardId, int pairId)
        {
            m_cardId = cardId;
            m_pairId = pairId;
            m_isFlipped = false;
            m_isMatched = false;
        }
        #endregion
    }
}
