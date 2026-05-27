using UnityEngine;

namespace GameArifiction.Interaction
{
    /// <summary>
    /// [기능]: 플레이어와 상호작용할 수 있는 모든 맵 객체(포탈, 표지판 등)의 공통 규격을 정의하는 인터페이스
    /// [작성자]: 윤승종
    /// </summary>
    public interface IInteractable
    {
        #region 프로퍼티 (Properties)
        /// <summary>
        /// 상호작용 버튼 UI에 표시될 간략한 안내 텍스트입니다. (예: "포탈 진입", "표지판 읽기")
        /// </summary>
        string InteractionPrompt { get; }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// 상호작용 버튼을 눌렀을 때 실행될 비즈니스 로직입니다.
        /// </summary>
        /// <param name="user">상호작용을 시작한 플레이어 게임 오브젝트</param>
        void Interact(GameObject user);
        #endregion
    }
}
