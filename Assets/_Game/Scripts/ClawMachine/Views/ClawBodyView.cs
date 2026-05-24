using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 절차적 애니메이션 수식에 의해 위치와 회전이 제어되는 집게 헤드 객체 (World Space 2D)
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-05-24
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: FixedJoint2D 폐기 → Kinematic 전환 + Transform 페어런팅 방식으로 완벽한 인형 동기화 구현
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ClawBodyView : MonoBehaviour
    {
        #region 참조 (Inspector)
        [SerializeField]
        [Tooltip("왼쪽 집게발의 회전 및 애니메이션을 담당할 Transform 객체입니다.")]
        private Transform m_leftClaw;

        [SerializeField]
        [Tooltip("오른쪽 집게발의 회전 및 애니메이션을 담당할 Transform 객체입니다.")]
        private Transform m_rightClaw;

        [SerializeField]
        [Tooltip("인형 획득(충돌) 판정의 중심이 되는 위치 포인트입니다.")]
        private Transform m_grabPoint;

        [SerializeField]
        [Tooltip("인형 획득(충돌) 판정의 반지름 크기입니다.")]
        private float m_grabRadius = 0.5f;

        [Header("집게 각도 설정")]
        [SerializeField]
        [Tooltip("에디터에 설정된 닫힘(Closed) 각도에서 추가로 벌어질 각도입니다.")]
        private float m_openAngleOffset = 30f;

        [Header("집게 고정 및 압력 설정")]
        [SerializeField]
        [Tooltip("캡슐 접촉 감지 후 집게를 추가로 더 꽉 조여 쥘 압박 각도(도)입니다.")]
        private float m_gripSensitivity = 5f;

        [SerializeField]
        [Tooltip("집게가 닫히는 데 걸리는 시간(초)입니다. 짧을수록 빠르게 닫힙니다.")]
        private float m_closeDuration = 0.5f;

        [SerializeField]
        [Tooltip("미끄러짐 방지를 위해 집게발에 적용할 고마찰 물리 재질입니다. (Friction 1.0 이상 권장)")]
        private PhysicsMaterial2D m_highFrictionMaterial;
        #endregion

        #region 내부 필드 (Private Fields)
        private ClawGameViewModel m_viewModel;
        private ClawMachineDollView m_grabbedDoll;
        private Rigidbody2D m_rigidbody;

        private Vector3 m_targetPosition;
        private Quaternion m_targetRotation;
        private bool m_hasTarget;

        private Vector3 m_leftClosedAngles;
        private Vector3 m_rightClosedAngles;

        private Collider2D[] m_leftColliders;
        private Collider2D[] m_rightColliders;
        private int m_fixedFrameCount;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            if (m_rigidbody != null)
            {
                m_rigidbody.bodyType = RigidbodyType2D.Kinematic;
                m_rigidbody.simulated = true;
                m_rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            // 혹시라도 자식 집게발에 기존 Rigidbody2D가 붙어있다면 물리 비틀림 예방을 위해 즉각 제거
            if (m_leftClaw != null)
            {
                var rb = m_leftClaw.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Destroy(rb);
                }
            }
            if (m_rightClaw != null)
            {
                var rb = m_rightClaw.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Destroy(rb);
                }
            }

            // 에디터에 설정된 기본 각도를 '닫힌 상태'로 기억 및 콜라이더 캐싱
            if (m_leftClaw != null)
            {
                m_leftClosedAngles = m_leftClaw.localEulerAngles;
                m_leftColliders = m_leftClaw.GetComponentsInChildren<Collider2D>();
            }
            if (m_rightClaw != null)
            {
                m_rightClosedAngles = m_rightClaw.localEulerAngles;
                m_rightColliders = m_rightClaw.GetComponentsInChildren<Collider2D>();
            }

            ApplyHighFriction();
        }

        private void ApplyHighFriction()
        {
            if (m_highFrictionMaterial == null)
            {
                return;
            }

            if (m_leftColliders != null)
            {
                for (int i = 0; i < m_leftColliders.Length; i++)
                {
                    if (m_leftColliders[i] != null)
                    {
                        m_leftColliders[i].sharedMaterial = m_highFrictionMaterial;
                    }
                }
            }

            if (m_rightColliders != null)
            {
                for (int i = 0; i < m_rightColliders.Length; i++)
                {
                    if (m_rightColliders[i] != null)
                    {
                        m_rightColliders[i].sharedMaterial = m_highFrictionMaterial;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (m_leftClaw != null)
            {
                DOTween.Kill(m_leftClaw);
            }
            if (m_rightClaw != null)
            {
                DOTween.Kill(m_rightClaw);
            }
        }

        private void FixedUpdate()
        {
            if (m_hasTarget == false || m_rigidbody == null)
            {
                return;
            }

            m_rigidbody.MovePosition(m_targetPosition);
            m_rigidbody.MoveRotation(m_targetRotation);

            if (m_grabbedDoll != null)
            {
                m_fixedFrameCount++;
                
                // 5 물리 프레임마다 정밀 물리 진단 로그 출력 (한글 로그 규칙 준수)
                if (m_fixedFrameCount % 5 == 1)
                {
                    float distance = Vector2.Distance(m_grabPoint.position, m_grabbedDoll.transform.position);
                    
                    Debug.Log($"[ClawBodyView] 진단 프레임: {m_fixedFrameCount} | 집게위치: {m_grabPoint.position} | 인형위치: {m_grabbedDoll.transform.position} | 실제거리: {distance:F3}");
                }

                // 인형이 Kinematic 상태가 아니게 되었다면 (외부 요인으로 상태 변경 시) 안전하게 해제
                if (m_grabbedDoll.IsGrabbed == false)
                {
                    ClawMachineDollView lostDoll = m_grabbedDoll;
                    m_grabbedDoll = null;
                    m_fixedFrameCount = 0;
                    
                    if (m_viewModel != null)
                    {
                        m_viewModel.NotifyJointBroken();
                    }
                    Debug.Log($"[ClawBodyView] 물리 낙하 감지: 인형({lostDoll.DollId})이 외부 요인으로 해제되었습니다.");
                }
            }
            else
            {
                m_fixedFrameCount = 0;
            }
        }
        #endregion

        #region 초기화 (Initialization)
        public void Initialize(ClawGameViewModel viewModel)
        {
            m_viewModel = viewModel;
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        public void UpdatePhysicsTarget(Vector3 position, Quaternion rotation)
        {
            m_targetPosition = position;
            m_targetRotation = rotation;
            m_hasTarget = true;
        }

        public async UniTask PlayGrabSequenceAsync(System.Threading.CancellationToken token)
        {
            // [물리 오므리기]: 실시간으로 충돌할 때까지 집게를 닫습니다 (압박 그립 포함).
            await CloseClawsAsync(token);
            
            TryGrabDoll();

            if (m_viewModel != null)
            {
                m_viewModel.NotifyGrabCompleted(m_grabbedDoll != null);
            }
        }

        public void SetClawsOpenImmediately()
        {
            if (m_leftClaw != null)
            {
                m_leftClaw.localRotation = Quaternion.Euler(m_leftClosedAngles + new Vector3(0, 0, -m_openAngleOffset));
            }
            if (m_rightClaw != null)
            {
                m_rightClaw.localRotation = Quaternion.Euler(m_rightClosedAngles + new Vector3(0, 0, m_openAngleOffset));
            }
        }

        public void OpenClaws()
        {
            if (m_leftClaw != null)
            {
                m_leftClaw.DOLocalRotate(m_leftClosedAngles + new Vector3(0, 0, -m_openAngleOffset), 0.3f);
            }
            if (m_rightClaw != null)
            {
                m_rightClaw.DOLocalRotate(m_rightClosedAngles + new Vector3(0, 0, m_openAngleOffset), 0.3f);
            }
        }

        /// <summary>
        /// [기능]: 집게를 열고, 잡고 있던 인형을 해제합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        public void ReleaseDoll()
        {
            OpenClaws();

            if (m_grabbedDoll != null)
            {
                m_grabbedDoll.SetGrabbed(false);
                m_grabbedDoll = null;
                m_fixedFrameCount = 0;
                Debug.Log("[ClawBodyView] 인형 릴리즈 완료.");
            }
        }
        #endregion

        #region 내부 획득 로직 (Private Methods)
        /// <summary>
        /// [기능]: 양쪽 집게가 각각 충돌체에 닿은 뒤, m_gripSensitivity 각도만큼 추가로 조여 단단히 잡을 때까지 물리 프레임 단위로 오므립니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        private async UniTask CloseClawsAsync(System.Threading.CancellationToken token)
        {
            float elapsed = 0f;
            bool leftStopped = false;
            bool rightStopped = false;

            // 접촉 플래그 및 감지 시점 각도 오프셋
            bool leftHit = false;
            bool rightHit = false;
            float leftHitOffset = 0f;
            float rightHitOffset = 0f;

            float leftStartAngle = -m_openAngleOffset;
            float rightStartAngle = m_openAngleOffset;

            // 초기 목표 각도 (최대 닫힘)
            float leftTargetAngle = 0f;
            float rightTargetAngle = 0f;

            float currentLeftOffset = leftStartAngle;
            float currentRightOffset = rightStartAngle;

            while (elapsed < m_closeDuration && (!leftStopped || !rightStopped))
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / m_closeDuration);
                float t = 1f - (1f - progress) * (1f - progress); // Ease Out Quad

                if (!leftStopped && m_leftClaw != null)
                {
                    currentLeftOffset = Mathf.Lerp(leftStartAngle, leftTargetAngle, t);
                    m_leftClaw.localRotation = Quaternion.Euler(m_leftClosedAngles + new Vector3(0, 0, currentLeftOffset));
                    
                    // 목표 압박 각도에 도달 시 갱신 정지
                    if (leftHit && Mathf.Abs(currentLeftOffset - leftTargetAngle) < 0.1f)
                    {
                        leftStopped = true;
                    }
                }

                if (!rightStopped && m_rightClaw != null)
                {
                    currentRightOffset = Mathf.Lerp(rightStartAngle, rightTargetAngle, t);
                    m_rightClaw.localRotation = Quaternion.Euler(m_rightClosedAngles + new Vector3(0, 0, currentRightOffset));
                    
                    // 목표 압박 각도에 도달 시 갱신 정지
                    if (rightHit && Mathf.Abs(currentRightOffset - rightTargetAngle) < 0.1f)
                    {
                        rightStopped = true;
                    }
                }

                // 트랜스폼 물리 월드 강제 동기화 (즉각 판정 필수)
                Physics2D.SyncTransforms();

                // 캡슐 접촉 판정 (접촉 후 추가 압박 각도로 목표 한계치 즉시 업데이트)
                if (!leftHit && IsClawCollidingWithDoll(m_leftColliders))
                {
                    leftHit = true;
                    leftHitOffset = currentLeftOffset;
                    leftTargetAngle = Mathf.Min(leftHitOffset + m_gripSensitivity, 0f);
                    Debug.Log($"[ClawBodyView] 왼쪽 집게발 캡슐 접촉. 감지 각도: {leftHitOffset:F2}, 추가 압력 목표: {leftTargetAngle:F2}");
                }

                if (!rightHit && IsClawCollidingWithDoll(m_rightColliders))
                {
                    rightHit = true;
                    rightHitOffset = currentRightOffset;
                    rightTargetAngle = Mathf.Max(rightHitOffset - m_gripSensitivity, 0f);
                    Debug.Log($"[ClawBodyView] 오른쪽 집게발 캡슐 접촉. 감지 각도: {rightHitOffset:F2}, 추가 압력 목표: {rightTargetAngle:F2}");
                }

                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
            }
        }

        /// <summary>
        /// [기능]: 제공된 콜라이더 배열 중 하나라도 ClawMachineDollView를 가진 물리 객체와 충돌하고 있는지 체크합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// [마지막 수정 작성자]: 윤승종
        /// </summary>
        private bool IsClawCollidingWithDoll(Collider2D[] clawColliders)
        {
            if (clawColliders == null)
            {
                return false;
            }

            ContactFilter2D filter = new ContactFilter2D().NoFilter();
            filter.useTriggers = false; // 물리적 표면 접촉 판정

            Collider2D[] results = new Collider2D[10];

            for (int i = 0; i < clawColliders.Length; i++)
            {
                if (clawColliders[i] == null)
                {
                    continue;
                }

                int count = clawColliders[i].Overlap(filter, results);
                for (int j = 0; j < count; j++)
                {
                    if (results[j] != null)
                    {
                        ClawMachineDollView doll = results[j].GetComponentInParent<ClawMachineDollView>();
                        if (doll != null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// [기능]: grabPoint 범위 내 인형을 탐색하여 Kinematic 전환 + Transform 페어런팅 방식으로 잡습니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-05-24
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: FixedJoint2D 방식 폐기 → Kinematic 전환 + Transform 페어런팅으로 전면 교체
        /// </summary>
        private void TryGrabDoll()
        {
            if (m_grabPoint == null)
            {
                return;
            }

            Collider2D[] colliders = Physics2D.OverlapCircleAll(m_grabPoint.position, m_grabRadius);
            int count = colliders.Length;

            for (int i = 0; i < count; i++)
            {
                Collider2D col = colliders[i];
                if (col == null)
                {
                    continue;
                }

                ClawMachineDollView doll = col.GetComponent<ClawMachineDollView>();
                if (doll != null)
                {
                    m_grabbedDoll = doll;
                    // grabPoint를 전달하여 인형을 해당 트랜스폼의 자식으로 편입시킵니다
                    m_grabbedDoll.SetGrabbed(true, m_grabPoint);

                    Debug.Log($"[ClawBodyView] 인형 획득 성공 (Kinematic 페어런팅 완료): {m_grabbedDoll.DollId}");
                    break;
                }
            }
        }
        #endregion
    }
}
