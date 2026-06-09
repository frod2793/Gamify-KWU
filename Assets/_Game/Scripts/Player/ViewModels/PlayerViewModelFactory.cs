using UnityEngine;

namespace GameArifiction.Player
{
    /// <summary>
    /// [기능]: 런타임 값에 기반하여 PlayerModel과 PlayerViewModel을 생성하고 반환하는 팩토리 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class PlayerViewModelFactory
    {
        public PlayerViewModel Create(float moveSpeed, Vector2 startPosition)
        {
            PlayerModel model = new PlayerModel(moveSpeed);
            return new PlayerViewModel(model, startPosition);
        }
    }
}
