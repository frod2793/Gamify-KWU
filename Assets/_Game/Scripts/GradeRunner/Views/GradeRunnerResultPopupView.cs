using DG.Tweening;
using EasyTransition;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner) 종료 시 최종 학점 알파벳 등급과 학점 수치, 게임 소요시간 등의 결과를 출력하고 재시도 및 로비 복귀 연출을 제어하는 팝업 View 컴포넌트
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class GradeRunnerResultPopupView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("텍스트 정보")]
        [SerializeField]
        [Tooltip("최종 학점 등급(A/B/C/D/F)을 큼직하게 노출할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_resultGradeText;

        [SerializeField]
        [Tooltip("최종 학점 수치 점수를 F1 포맷으로 노출할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_resultPointText;

        [SerializeField]
        [Tooltip("게임 소요 시간을 초 단위로 표시할 텍스트 컴포넌트입니다.")]
        private TextMeshProUGUI m_resultTimeText;

        [Header("조작 버튼")]
        [SerializeField]
        [Tooltip("미니게임을 다시 시작할 수 있는 버튼입니다.")]
        private Button m_retryButton;

        [SerializeField]
        [Tooltip("미니게임을 종료하고 로비로 탈출하는 버튼입니다.")]
        private Button m_exitButton;

        [Header("이지 트랜지션 설정")]
        [SerializeField]
        [Tooltip("로비 복귀 시 씬 화면 전환을 수려하게 연출해 줄 이지 트랜지션 설정 자산입니다.")]
        private TransitionSettings m_transitionSettings;

        [SerializeField]
        [Tooltip("트랜지션 전환 효과가 시작되기까지의 대기 지연시간(초)입니다.")]
        private float m_transitionDelay = 0.1f;

        [Header("세션 데이터")]
        [SerializeField]
        [Tooltip("플레이어의 위치 상태를 로비에서 복원하기 위한 ScriptableObject 데이터 자산입니다.")]
        private Player.PlayerSO m_playerSO;

        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: 뷰모델 데이터를 주입하고 팝업 활성화 감지 이벤트를 연동 및 버튼 클릭 콜백을 초기화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Initialize(GradeRunnerViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 결과 송출 이벤트 바인딩
            if (m_viewModel != null)
            {
                m_viewModel.OnGameResult += HandleGameResult;
            }

            // 버튼 리스너 바인딩
            if (m_retryButton != null)
            {
                m_retryButton.onClick.AddListener(func_OnRetryButtonClicked);
            }

            if (m_exitButton != null)
            {
                m_exitButton.onClick.AddListener(func_OnExitButtonClicked);
            }

            // 씬 시작 시에는 숨겨진 상태로 대기
            gameObject.SetActive(false);
            Debug.Log("[GradeRunnerResultPopupView] 결과 팝업 뷰 초기화 및 DI 세팅 완료.");
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnGameResult -= HandleGameResult;
            }

            if (m_retryButton != null)
            {
                m_retryButton.onClick.RemoveListener(func_OnRetryButtonClicked);
            }

            if (m_exitButton != null)
            {
                m_exitButton.onClick.RemoveListener(func_OnExitButtonClicked);
            }
        }

        #endregion

        #region UI 버튼 클릭 이벤트 핸들러 (Public Methods)

        /// <summary>
        /// [기능]: 다시하기 버튼 클릭 시 뷰모델의 재시작 루틴을 트리거하여 씬 초기화 및 게임을 재개합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnRetryButtonClicked()
        {
            Debug.Log("[GradeRunnerResultPopupView] 플레이어가 미니게임 다시하기를 선택했습니다.");

            // 팝업을 닫고 뷰모델 게임 재시작 (낙하 오브젝트들은 뷰모델 재시작 이벤트 송출에 의해 스포너에서 알아서 자동 정리됨)
            gameObject.SetActive(false);
            if (m_viewModel != null)
            {
                m_viewModel.StartGame();
            }
        }

        /// <summary>
        /// [기능]: 나가기 버튼 클릭 시 플레이어 이전 위치 정보를 활성화한 뒤, 이지 트랜지션 연출을 거쳐 로비 씬으로 안전하게 전이시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void func_OnExitButtonClicked()
        {
            Debug.Log("[GradeRunnerResultPopupView] 플레이어가 로비 복귀 나가기 버튼을 선택했습니다.");

            // 로비 씬 복원 활성화 플래그 주입
            if (m_playerSO != null)
            {
                m_playerSO.HasSavedPosition = true;
            }

            // 이지 트랜지션 연출 적용
            if (m_transitionSettings != null)
            {
                TransitionManager manager = FindFirstObjectByType<TransitionManager>();
                if (manager != null)
                {
                    TransitionManager.Instance().Transition("Lobby", m_transitionSettings, m_transitionDelay);
                    return;
                }
            }

            // 트랜지션 유실 시 일반 씬 매니저 다이렉트 전이 폴백
            SceneManager.LoadScene("Lobby");
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        /// <summary>
        /// [기능]: 뷰모델에서 게임 결과를 전달받았을 때 텍스트 데이터를 갱신하고 슬릭한 팝업 확대(OutBack) 도트윈 애니메이션으로 화면에 노출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleGameResult(GradeRunnerResultDTO result)
        {
            if (result == null)
            {
                return;
            }

            // 텍스트 컴포넌트 데이터 반영
            if (m_resultGradeText != null)
            {
                m_resultGradeText.text = result.GradeLetter;
            }

            if (m_resultPointText != null)
            {
                m_resultPointText.text = $"최종 학점: {result.FinalGradePoint:F1} / 5.0";
            }

            if (m_resultTimeText != null)
            {
                m_resultTimeText.text = $"소요 시간: {result.ElapsedTime:F1}초";
            }

            gameObject.SetActive(true);

            // 프리미엄 팝업 모달 등장 연출 (Scale 0.0 -> 1.0 바운싱 기법)
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }

        #endregion
    }
}
