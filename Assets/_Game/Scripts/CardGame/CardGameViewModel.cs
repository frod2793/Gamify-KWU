using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.FSM;
using CardGame.Model;

namespace CardGame.ViewModel
{
    public class CardGameViewModel
    {
        private IGameState m_currentState;
        private readonly List<CardViewModel> m_cards = new List<CardViewModel>();
        
        public IReadOnlyList<CardViewModel> Cards => m_cards;
        public int Score { get; private set; }
        
        public event Action<int> OnScoreChanged;
        public event Action OnGameOver;

        public void Initialize(IEnumerable<int> shapeIds)
        {
            m_cards.Clear();
            int idCounter = 0;
            foreach (var shapeId in shapeIds)
            {
                var model = new CardModel(idCounter++, shapeId);
                m_cards.Add(new CardViewModel(model));
            }

            ChangeState(new IdleState(this));
        }

        public void ChangeState(IGameState newState)
        {
            m_currentState?.Exit();
            m_currentState = newState;
            m_currentState?.Enter();
        }

        public void SelectCard(CardViewModel card)
        {
            m_currentState?.OnCardSelected(card);
        }

        public void AddScore(int amount)
        {
            Score += amount;
            OnScoreChanged?.Invoke(Score);
            
            if (m_cards.All(c => c.IsMatched))
            {
                OnGameOver?.Invoke();
            }
        }
    }
}
