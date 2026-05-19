using System.Threading.Tasks;
using CardGame.ViewModel;

namespace CardGame.FSM
{
    public interface IGameState
    {
        void Enter();
        void Exit();
        void OnCardSelected(CardViewModel card);
    }

    public abstract class BaseGameState : IGameState
    {
        protected readonly CardGameViewModel m_context;

        protected BaseGameState(CardGameViewModel context)
        {
            m_context = context;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public abstract void OnCardSelected(CardViewModel card);
    }
}
