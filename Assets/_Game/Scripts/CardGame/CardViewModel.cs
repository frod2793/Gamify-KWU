using System;
using CardGame.Model;

namespace CardGame.ViewModel
{
    public class CardViewModel
    {
        private readonly CardModel m_model;
        
        public int Id => m_model.Id;
        public int ShapeId => m_model.ShapeId;
        public bool IsFlipped => m_model.IsFlipped;
        public bool IsMatched => m_model.IsMatched;

        public event Action<bool> OnFlipStateChanged;
        public event Action OnMatched;

        public CardViewModel(CardModel model)
        {
            m_model = model;
        }

        public void Flip(bool state)
        {
            if (m_model.IsMatched) return;
            if (m_model.IsFlipped == state) return;

            m_model.IsFlipped = state;
            OnFlipStateChanged?.Invoke(state);
        }

        public void SetMatched()
        {
            m_model.IsMatched = true;
            OnMatched?.Invoke();
        }
    }
}
