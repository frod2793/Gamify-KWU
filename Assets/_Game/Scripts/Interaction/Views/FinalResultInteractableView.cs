using UnityEngine;
using VContainer;
using GameArifiction.Interaction;
using GameArifiction.Player;

namespace GameArifiction.UI.FinalResult
{
    /// <summary>
    /// [기능]: 맵 상의 객체(키오스크 등)와 상호작용하여 최종 결과 팝업을 띄우는 View 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class FinalResultInteractableView : MonoBehaviour, IInteractable
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("활성화가 되었을 때만 뜨는 말풍선 형태의 하이라이트 오브젝트입니다.")]
        private GameObject m_activeHighlightObject;
        #endregion

        #region 내부 필드 (Private Fields)
        private FinalResultPopupView m_popupView;
        private PlayerSO m_playerSO;
        private bool m_isActive = false;
        #endregion

        #region 프로퍼티 (Properties)
        /// <summary>
        /// [기능]: 활성화 상태일 때만 상호작용 안내 텍스트를 제공합니다.
        /// </summary>
        public string InteractionPrompt => m_isActive ? "결과 확인" : string.Empty;
        #endregion

        #region VContainer 주입 (Injection)
        /// <summary>
        /// [기능]: VContainer로부터 팝업 뷰와 플레이어 데이터를 주입받습니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 플레이어 데이터 확인을 위해 PlayerSO 주입 매개변수 추가
        /// </summary>
        [Inject]
        public void Construct(FinalResultPopupView popupView, PlayerSO playerSO)
        {
            m_popupView = popupView;
            m_playerSO = playerSO;
        }
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        /// <summary>
        /// [기능]: 모든 미니게임의 클리어 여부를 검사하여 하이라이트를 활성화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 문서화 주석 이력 추가
        /// </summary>
        private void Start()
        {
            RefreshKioskState();
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 외부(디버그 트리거 등)에서 성적이 강제 갱신되었을 때, 키오스크 활성화 여부 및 말풍선 하이라이트 상태를 실시간 동기화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 키오스크 상태 새로고침 기능 퍼블릭 분리 구현
        /// </summary>
        public void RefreshKioskState()
        {
            if (m_playerSO != null)
            {
                m_isActive = m_playerSO.IsAllMinigamesCleared;
            }
            else
            {
                m_isActive = false;
                Debug.LogWarning("[FinalResultInteractableView] PlayerSO 데이터가 주입되지 않았습니다.");
            }

            if (m_activeHighlightObject != null)
            {
                m_activeHighlightObject.SetActive(m_isActive);
            }
            Debug.Log($"[FinalResultInteractableView] 키오스크 활성화 상태 새로고침 완료: {m_isActive}");
        }

        /// <summary>
        /// [기능]: 플레이어가 상호작용을 시도했을 때 호출되며, 활성화 상태일 경우 결과 팝업을 표시합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 상호작용 시점에 실시간으로 최종 클리어 여부를 재검증하여 동기화 오류 방지
        /// </summary>
        /// <param name="user">상호작용을 발생시킨 플레이어 오브젝트</param>
        public void Interact(GameObject user)
        {
            // 상호작용 시점에도 최신 성적 기반으로 활성화 상태를 한 번 더 재검증하여 동기화 오류를 방지합니다.
            if (m_playerSO != null)
            {
                m_isActive = m_playerSO.IsAllMinigamesCleared;
            }

            if (m_isActive == false)
            {
                Debug.Log("[FinalResultInteractableView] 아직 모든 미니게임을 완료하지 않아 결과 확인이 불가능합니다.");
                return;
            }

            if (m_popupView != null)
            {
                Debug.Log($"[FinalResultInteractableView] {user.name}이(가) 키오스크와 상호작용하여 결과 팝업을 엽니다.");
                m_popupView.ShowPopup();
            }
            else
            {
                Debug.LogError("[FinalResultInteractableView] 의존성 주입이 정상적으로 이루어지지 않아 팝업을 열 수 없습니다.");
            }
        }
        #endregion
    }
}
