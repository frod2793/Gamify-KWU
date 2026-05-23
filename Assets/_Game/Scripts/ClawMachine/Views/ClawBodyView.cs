using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 절차적 애니메이션 수식에 의해 위치와 회전이 제어되는 집게 헤드 객체
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-24
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 물리 엔진(Rigidbody2D) 의존성을 완전히 제거하고 절차적 제어 방식으로 전환
    /// </summary>
    public class ClawBodyView : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("왼쪽 집게발의 회전 및 애니메이션을 담당할 RectTransform 객체입니다.")]
        private RectTransform m_leftClaw;

        [SerializeField]
        [Tooltip("오른쪽 집게발의 회전 및 애니메이션을 담당할 RectTransform 객체입니다.")]
        private RectTransform m_rightClaw;

        [SerializeField]
        [Tooltip("인형 획득(충돌) 판정의 중심이 되는 위치 포인트입니다.")]
        private Transform m_grabPoint;

        [SerializeField]
        [Tooltip("인형 획득(충돌) 판정의 반지름 크기입니다.")]
        private float m_grabRadius = 50f;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private ClawMachineDollView m_grabbedDoll;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            // [전면 재설계]: UI 캔버스와의 충돌을 방지하기 위해 물리 엔진 사용을 중단합니다.
            // 모든 이동과 회전은 ClawView에서 수식으로 계산되어 직접 주입됩니다.
        }

        private void OnDestroy()
        {
            if (m_leftClaw != null) DOTween.Kill(m_leftClaw);
            if (m_rightClaw != null) DOTween.Kill(m_rightClaw);
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 하강/상승 및 이동 상태에 따른 물리 모드 전환 (더 이상 실제 Rigidbody를 조작하지 않음)
        /// [작성자]: 윤승종
        /// </summary>
        public void SetPhysicsMode(bool isHighResistance)
        {
            // [참고]: 이제 실제 물리 엔진이 아닌 ClawView의 수식 파라미터가 이 역할을 대신합니다.
        }

        /// <summary>
        /// [기능]: 집게 닫기 연출 및 인형 획득 시도
        /// [작성자]: 윤승종
        /// </summary>
        public async UniTask PlayGrabSequenceAsync(System.Threading.CancellationToken token)
        {
            // 집게 닫기 애니메이션
            await CloseClawsAsync(token);
            
            // 실제 획득 판정 (OverlapBox 등)
            TryGrabDoll();

            if (m_viewModel != null)
            {
                m_viewModel.NotifyGrabCompleted(m_grabbedDoll != null);
            }
        }

        /// <summary>
        /// [기능]: 게임 시작 시 트윈 연출 없이 집게를 즉시 오픈 상태 각도로 설정
        /// [작성자]: 윤승종
        /// </summary>
        public void SetClawsOpenImmediately()
        {
            if (m_leftClaw != null)
            {
                m_leftClaw.localRotation = Quaternion.Euler(0, 0, -30f);
            }
            if (m_rightClaw != null)
            {
                m_rightClaw.localRotation = Quaternion.Euler(0, 0, 30f);
            }
        }

        /// <summary>
        /// [기능]: 집게 서서히 열기 애니메이션
        /// [작성자]: 윤승종
        /// </summary>
        public void OpenClaws()
        {
            if (m_leftClaw != null) m_leftClaw.DOLocalRotate(new Vector3(0, 0, -30f), 0.3f);
            if (m_rightClaw != null) m_rightClaw.DOLocalRotate(new Vector3(0, 0, 30f), 0.3f);
        }

        /// <summary>
        /// [기능]: 수동 릴리즈 및 복귀 연동에 따른 집게 해제
        /// [작성자]: 윤승종
        /// </summary>
        public void ReleaseDoll()
        {
            OpenClaws();

            if (m_grabbedDoll != null)
            {
                // [수정]: Rigidbody2D가 제거되었으므로 Transform 계층 구조 기반의 릴리즈 수행
                m_grabbedDoll.transform.SetParent(null);
                m_grabbedDoll.SetGrabbed(false);
                m_grabbedDoll = null;
                Debug.Log("[ClawBodyView] 인형 릴리즈 완료.");
            }
        }
        #endregion

        #region 내부 획득 로직 (Private Methods)
        private async UniTask CloseClawsAsync(System.Threading.CancellationToken token)
        {
            UniTask leftTask = m_leftClaw != null ? m_leftClaw.DOLocalRotate(Vector3.zero, 0.5f).ToUniTask(cancellationToken: token) : UniTask.CompletedTask;
            UniTask rightTask = m_rightClaw != null ? m_rightClaw.DOLocalRotate(Vector3.zero, 0.5f).ToUniTask(cancellationToken: token) : UniTask.CompletedTask;

            await UniTask.WhenAll(leftTask, rightTask);
        }

        private void TryGrabDoll()
        {
            if (m_grabPoint == null)
            {
                return;
            }

            // 2D Physics 오버랩 체크 (레이어 기반 필터링 권장)
            Collider2D[] colliders = Physics2D.OverlapCircleAll(m_grabPoint.position, m_grabRadius);
            int count = colliders.Length;
            
            for (int i = 0; i < count; i++)
            {
                Collider2D col = colliders[i];
                if (col == null) continue;

                ClawMachineDollView doll = col.GetComponent<ClawMachineDollView>();
                if (doll != null)
                {
                    m_grabbedDoll = doll;

                    // [재설계]: 조인트 대신 트랜스폼 부모-자식 관계로 획득 유지
                    // 인형의 Rigidbody는 획득 시 Kinematic으로 변환하여 물리 연산 배제 (DollView 내부에서 처리하거나 직접 수행)
                    Rigidbody2D dollRb = doll.GetComponent<Rigidbody2D>();
                    if (dollRb != null)
                    {
                        dollRb.bodyType = RigidbodyType2D.Kinematic;
                        dollRb.linearVelocity = Vector2.zero;
                        dollRb.angularVelocity = 0f;
                    }

                    m_grabbedDoll.SetGrabbed(true, m_grabPoint);
                    m_grabbedDoll.transform.SetParent(m_grabPoint);
                    m_grabbedDoll.transform.localPosition = Vector3.zero;

                    Debug.Log($"[ClawBodyView] 인형 획득 성공 (계층 결합): {m_grabbedDoll.DollId}");
                    break; 
                }
            }
        }
        #endregion
    }
}
