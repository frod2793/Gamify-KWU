using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 집게가 떨어뜨린 캡슐이 퇴출구(Exit Zone) 물리 영역에 도달했을 때 감지하여 ViewModel에 정답을 제출합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ClawMachineExitView : MonoBehaviour
    {
        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            // 퇴출구 콜라이더 트리거 설정 강제
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;
        }
        #endregion

        #region 유니티 물리 트리거 감지 (Unity Physics Lifecycle)
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_viewModel == null || collision == null)
            {
                return;
            }

            // 충돌 대상이 인형 캡슐 뷰 컴포넌트를 들고 있는지 확인
            ClawMachineDollView dollView = collision.GetComponentInParent<ClawMachineDollView>();
            if (dollView != null)
            {
                // 집게가 현재 인형을 릴리즈(놓기)하여 완벽히 낙하에 성공했는지 검증
                if (dollView.IsGrabbed == false)
                {
                    Debug.Log($"[ClawMachineExitView] 퇴출구 캡슐 골인 감지 완료. DollId: {dollView.DollId}");

                    // 뷰모델로 정답 체킹 이벤트 전송
                    m_viewModel.func_SubmitAnswer(dollView.DollId);

                    // 연출을 위해 골인된 캡슐은 0.3초 후 안전하게 소멸시킵니다.
                    Destroy(dollView.gameObject, 0.3f);
                }
            }
        }
        #endregion
    }
}
