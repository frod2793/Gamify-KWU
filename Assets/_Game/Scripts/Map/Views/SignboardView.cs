using UnityEngine;
using GameArifiction.Interaction;

namespace GameArifiction.Map
{
    /// <summary>
    /// [기능]: 안내판/표지판 오브젝트에 장착되어 플레이어 진입 시 안내 문구를 상호작용 형태로 보여주는 뷰 클래스
    /// [작성자]: 윤승종
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SignboardView : MonoBehaviour, IInteractable
    {
        #region UI 참조
        [Header("표지판 설정")]
        [SerializeField]
        [TextArea(3, 10)]
        [Tooltip("상호작용 시 화면 로그 또는 대화창에 출력할 안내판 내용입니다.")]
        private string m_signboardContent = "여기는 한국외대 월드입니다.";

        [SerializeField]
        [Tooltip("상호작용 버튼 UI에 표시될 텍스트입니다.")]
        private string m_interactionPrompt = "안내판 읽기";
        #endregion

        #region 프로퍼티
        /// <summary>
        /// 상호작용 버튼 UI에 표시될 안내 텍스트입니다.
        /// </summary>
        public string InteractionPrompt => m_interactionPrompt;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            Collider2D signboardCollider = GetComponent<Collider2D>();
            if (signboardCollider != null)
            {
                signboardCollider.isTrigger = true;
                Debug.Log("[SignboardView] 안내판의 Collider2D를 트리거(isTrigger = true)로 자동으로 설정했습니다.");
            }
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [기능]: 플레이어가 안내판 상호작용 버튼을 클릭했을 때 호출되며 UI 안내 창을 엽니다.
        /// [작성자]: 윤승종
        /// </summary>
        /// <param name="user">상호작용을 실행한 플레이어 오브젝트</param>
        public void Interact(GameObject user)
        {
            Debug.Log($"[SignboardView] 안내판 읽기 상호작용 결과: {m_signboardContent}");

            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowSignboard(m_signboardContent);
            }
        }
        #endregion
    }
}
