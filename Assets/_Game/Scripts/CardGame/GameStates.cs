using System.Threading.Tasks;
using CardGame.ViewModel;

namespace CardGame.FSM
{
    public class IdleState : BaseGameState
    {
        public IdleState(CardGameViewModel context) : base(context) { }

        public override void OnCardSelected(CardViewModel card)
        {
            if (card.IsFlipped || card.IsMatched) return;

            card.Flip(true);
            m_context.ChangeState(new FirstCardFlippedState(m_context, card));
        }
    }

    public class FirstCardFlippedState : BaseGameState
    {
        private readonly CardViewModel m_firstCard;

        public FirstCardFlippedState(CardGameViewModel context, CardViewModel firstCard) : base(context)
        {
            m_firstCard = firstCard;
        }

        public override void OnCardSelected(CardViewModel card)
        {
            if (card.IsFlipped || card.IsMatched || card == m_firstCard) return;

            card.Flip(true);
            m_context.ChangeState(new CheckingMatchState(m_context, m_firstCard, card));
        }
    }

    public class CheckingMatchState : BaseGameState
    {
        private readonly CardViewModel m_firstCard;
        private readonly CardViewModel m_secondCard;

        public CheckingMatchState(CardGameViewModel context, CardViewModel first, CardViewModel second) : base(context)
        {
            m_firstCard = first;
            m_secondCard = second;
        }

        public override async void Enter()
        {
            // Wait for 1 second to let user see the cards
            await Task.Delay(1000);

            if (m_firstCard.ShapeId == m_secondCard.ShapeId)
            {
                m_firstCard.SetMatched();
                m_secondCard.SetMatched();
                m_context.AddScore(10);
            }
            else
            {
                m_firstCard.Flip(false);
                m_secondCard.Flip(false);
            }

            m_context.ChangeState(new IdleState(m_context));
        }

        public override void OnCardSelected(CardViewModel card)
        {
            // Do nothing while checking
        }
    }
}
