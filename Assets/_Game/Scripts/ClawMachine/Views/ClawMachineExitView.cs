using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 집게가 떨어뜨린 캡슐이 퇴출구(Exit Zone) 물리 영역에 도달했을 때 감지하여 ViewModel에 정답을 제출합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ClawMachineExitView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [Header("캡슐 골인 이펙트 프리팹")]
        [SerializeField]
        [Tooltip("정답 캡슐 골인 시 월드 공간에 생성할 노란색 반짝이 이펙트 프리팹입니다.")]
        private GameObject m_correctEffectPrefab;

        [SerializeField]
        [Tooltip("오답 캡슐 골인 시 월드 공간에 생성할 회색 연기 구름 이펙트 프리팹입니다.")]
        private GameObject m_wrongEffectPrefab;

        [Header("결과 팝업 지연 설정")]
        [SerializeField]
        [Tooltip("골인 이펙트 연출을 완전히 감상하기 위해 결과 팝업 노출을 지연시킬 시간(초)입니다.")]
        private float m_resultDelaySeconds = 1.2f;
        #endregion

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
        /// <summary>
        /// [기능]: 캡슐이 골인 영역에 닿으면 정답/오답 여부를 식별하여 해당 이펙트를 재생한 후 제출 처리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 이펙트 효과를 보기 위한 정오답 발동 팝업 지연 시스템(UniTask.Delay) 및 중복 방어 코드 적용
        /// </summary>
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

                    // 중복 감지 방지를 위해 캡슐의 콜라이더를 즉시 비활성화
                    Collider2D dollCollider = dollView.GetComponent<Collider2D>();
                    if (dollCollider != null)
                    {
                        dollCollider.enabled = false;
                    }

                    // 비동기로 이펙트 재생 및 지연 결과 제출 수행
                    SubmitAnswerWithDelayAsync(dollView, this.GetCancellationTokenOnDestroy()).Forget();
                }
            }
        }
        #endregion

        #region 비동기 처리 및 연출 제어 (Private Methods)
        /// <summary>
        /// [기능]: 골인된 캡슐에 대해 이펙트를 재생하고, 지정된 지연 시간 대기 후 결과 판정 팝업을 표시하도록 뷰모델에 제출합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-12
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        private async UniTaskVoid SubmitAnswerWithDelayAsync(ClawMachineDollView dollView, System.Threading.CancellationToken token)
        {
            if (dollView == null)
            {
                return;
            }

            // 1. 골인 캡슐의 물리 운동을 즉시 정하여 제자리에 고정시킴
            Rigidbody2D rb = dollView.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }

            Vector3 spawnPosition = dollView.transform.position;
            bool isCorrect = dollView.IsCorrect;
            string dollId = dollView.DollId;

            // 정오답 여부에 따른 반짝이/연기 이펙트 스폰
            if (isCorrect)
            {
                if (m_correctEffectPrefab != null)
                {
                    GameObject correctEffectInstance = Instantiate(m_correctEffectPrefab, spawnPosition, Quaternion.identity);
                    Destroy(correctEffectInstance, 1.5f);
                }
                else
                {
                    Debug.LogWarning("[ClawMachineExitView] m_correctEffectPrefab이 인스펙터에 할당되어 있지 않습니다.");
                }
            }
            else
            {
                if (m_wrongEffectPrefab != null)
                {
                    GameObject wrongEffectInstance = Instantiate(m_wrongEffectPrefab, spawnPosition, Quaternion.identity);
                    Destroy(wrongEffectInstance, 1.5f);
                }
                else
                {
                    Debug.LogWarning("[ClawMachineExitView] m_wrongEffectPrefab이 인스펙터에 할당되어 있지 않습니다.");
                }
            }

            // 캡슐 오브젝트는 이펙트 연출을 방해하지 않게 0.3초 후 안전하게 소멸시킵니다.
            Destroy(dollView.gameObject, 0.3f);

            // 지정된 팝업 노출 지연 시간 대기
            await UniTask.Delay(System.TimeSpan.FromSeconds(m_resultDelaySeconds), cancellationToken: token);

            // 2. 뷰모델로 정답 체킹 이벤트 최종 제출 (이로 인해 결과 팝업이 노출됨)
            if (m_viewModel != null)
            {
                m_viewModel.func_SubmitAnswer(dollId);
            }
        }
        #endregion
    }
}
