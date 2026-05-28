using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)의 플레이어 이동 제어, 화면 이탈 방지 경계 제한 및 낙하물 충돌 처리를 전담하는 View 컴포넌트.
///         플레이어의 가로 이동 한계를 화면 전체 대신 지정된 땅(Ground) 오브젝트의 좌우 실제 너비 영역으로 제한합니다.
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    [RequireComponent(typeof(Collider2D))]
    public class GradeRunnerPlayerView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("이동 제한 바닥")]
        [SerializeField]
        [Tooltip("플레이어의 가로 이동 한계 좌우 영역을 제한할 땅(Ground) 오브젝트의 Collider2D입니다.")]
        private Collider2D m_groundCollider;

        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;
        private float m_minX;
        private float m_maxX;
        private bool m_isInitialized = false;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Update()
        {
            if (!m_isInitialized)
            {
                return;
            }

            if (m_viewModel == null)
            {
                return;
            }

            if (m_viewModel.CurrentState != GradeRunnerState.Playing && m_viewModel.CurrentState != GradeRunnerState.Phase2Cutscene)
            {
                return;
            }

            HandleMovement();
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: 뷰모델 의존성을 주입하고 땅(Ground) 너비 기준 또는 화면 기준의 가로 이동 물리 한계 경계를 산정합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Initialize(GradeRunnerViewModel viewModel)
        {
            m_viewModel = viewModel;
            CalculateMovementBounds();
            m_isInitialized = true;
            Debug.Log($"[GradeRunnerPlayerView] 플레이어 뷰 초기화 완료. 최종 이동 제한 경계(땅 기준): [{m_minX:F2} ~ {m_maxX:F2}]");
        }

        #endregion

        #region 내부 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 지정된 바닥(Ground) 콜라이더의 좌우 경계 좌표를 정밀 파악하고, 플레이어 스프라이트의 가로 반절 크기를 오프셋 삼아 최종 이동 한계를 계산합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void CalculateMovementBounds()
        {
            if (m_groundCollider != null)
            {
                Bounds groundBounds = m_groundCollider.bounds;

                // 플레이어의 반절 가로 크기를 고려하여 한계선 안착 오프셋 설정
                float padding = 0.5f; 
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    padding = sr.bounds.extents.x;
                }

                m_minX = groundBounds.min.x + padding;
                m_maxX = groundBounds.max.x - padding;

                // 극단적으로 좁은 땅 영역 충돌 처리 예방 가드
                if (m_minX >= m_maxX)
                {
                    m_minX = groundBounds.min.x;
                    m_maxX = groundBounds.max.x;
                }
            }
            else
            {
                Debug.LogWarning("[GradeRunnerPlayerView] m_groundCollider가 지정되지 않아 화면 끝 전체 영역을 기준으로 가로 경계를 산출합니다.");
                CalculateCameraScreenBounds();
            }
        }

        /// <summary>
        /// [기능]: 바닥 콜라이더 누락 시의 안전한 폴백용 카메라 메인 뷰포트 월드 경계 좌표 계산 기법입니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void CalculateCameraScreenBounds()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                float zDistance = Mathf.Abs(cam.transform.position.z - transform.position.z);
                if (zDistance <= 0f)
                {
                    zDistance = 10f;
                }

                Vector3 leftBottom = cam.ViewportToWorldPoint(new Vector3(0f, 0f, zDistance));
                Vector3 rightBottom = cam.ViewportToWorldPoint(new Vector3(1f, 0f, zDistance));

                float padding = 0.5f;
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    padding = sr.bounds.extents.x;
                }

                m_minX = leftBottom.x + padding;
                m_maxX = rightBottom.x - padding;

                if (m_minX >= m_maxX)
                {
                    m_minX = leftBottom.x;
                    m_maxX = rightBottom.x;
                    if (m_minX >= m_maxX)
                    {
                        m_minX = -8f;
                        m_maxX = 8f;
                    }
                }
            }
            else
            {
                m_minX = -8f;
                m_maxX = 8f;
            }
        }

        /// <summary>
        /// [기능]: 새 Input System 패키지를 통해 좌우(A/D, 화살표) 입력을 감지하고 정밀 속도 비례 가로 이동 및 화면 영역 클램프를 수행합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleMovement()
        {
            float inputX = 0f;

            // 새 Input System의 키보드 정밀 입력 감지
            Keyboard currentKeyboard = Keyboard.current;
            if (currentKeyboard != null)
            {
                if (currentKeyboard.aKey.isPressed || currentKeyboard.leftArrowKey.isPressed)
                {
                    inputX = -1f;
                }
                else if (currentKeyboard.dKey.isPressed || currentKeyboard.rightArrowKey.isPressed)
                {
                    inputX = 1f;
                }
            }

            if (Mathf.Approximately(inputX, 0f))
            {
                return;
            }

            // 가로 전체 편도 폭 계산
            float screenWidth = m_maxX - m_minX;
            if (screenWidth <= 0f)
            {
                screenWidth = 16f; // 안전 복구 폴백
            }
            
            // 뷰모델을 통해 현재 프레임당 스피드 취득
            float speed = m_viewModel.GetPlayerMoveSpeed(screenWidth);

            // 이동 반영 및 위치 제한
            Vector3 pos = transform.position;
            pos.x += inputX * speed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, m_minX, m_maxX);
            
            transform.position = pos;
        }

        /// <summary>
        /// [기능]: 2D 트리거 충돌 시 족보/코드 태그를 분별하여 뷰모델로 데이터를 이전 처리합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_viewModel == null || (m_viewModel.CurrentState != GradeRunnerState.Playing && m_viewModel.CurrentState != GradeRunnerState.Phase2Cutscene))
            {
                return;
            }

            FallingObjectView objView = collision.GetComponent<FallingObjectView>();
            if (objView != null)
            {
                Vector2 contactPos = collision.transform.position;

                if (objView.ObjectType == FallingObjectType.Code)
                {
                    m_viewModel.ApplyCodeHit(contactPos);
                }
                else if (objView.ObjectType == FallingObjectType.CheatSheet)
                {
                    m_viewModel.ApplyCheatSheetPickup(contactPos);
                }

                // 충돌된 낙하 오브젝트는 풀로 회수 처리
                objView.func_Deactivate();
            }
        }

        #endregion
    }
}
