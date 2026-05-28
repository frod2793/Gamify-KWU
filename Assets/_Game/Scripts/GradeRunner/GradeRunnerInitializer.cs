using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner) 씬의 진입점(Composition Root).
///         싱글톤을 배제하고 수동 의존성 주입(DI)을 통해 Model, ViewModel, View 간의 유기적 관계를 수립하며 게임을 가동시킵니다.
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class GradeRunnerInitializer : MonoBehaviour
    {
        #region UI 및 컴포넌트 참조 (Inspector)

        [Header("미니게임 핵심 뷰")]
        [SerializeField]
        [Tooltip("타이머 및 학점 점수바 UI 출력을 제어하는 HUD 뷰입니다.")]
        private GradeRunnerHudView m_hudView;

        [SerializeField]
        [Tooltip("2D 플레이어의 입력 및 충돌 처리를 전담하는 플레이어 뷰입니다.")]
        private GradeRunnerPlayerView m_playerView;

        [SerializeField]
        [Tooltip("낙하하는 코드/족보를 오브젝트 풀 기반으로 스폰하는 스포너 뷰입니다.")]
        private FallingObjectSpawnerView m_spawnerView;

        [SerializeField]
        [Tooltip("게임이 종료되었을 때 결과를 보여주는 전용 결과 팝업 뷰입니다.")]
        private GradeRunnerResultPopupView m_resultPopup;

        [SerializeField]
        [Tooltip("코드를 떨어뜨려 플레이어를 공격하는 씬 내 교수님 캐릭터 뷰입니다.")]
        private ProfessorView m_professorView;

        [Header("게임 설정 리소스")]
        [SerializeField]
        [Tooltip("인스펙터에서 관리할 게임의 주요 밸런스 데이터 설정 자산입니다.")]
        private GradeRunnerConfigSO m_config;

        [SerializeField]
        [Tooltip("교수 대사를 보관하고 있는 ScriptableObject 에셋입니다.")]
        private GradeRunnerDialogueSO m_dialogueSO;

        [Header("세션 데이터")]
        [SerializeField]
        [Tooltip("씬 간 복귀 좌표 및 영구 누적 성적 기록을 보관할 ScriptableObject 에셋입니다.")]
        private Player.PlayerSO m_playerSO;

        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void Start()
        {
            InitializeGradeRunner();
        }

        private void OnDestroy()
        {
            // 백그라운드에서 돌고 있는 비동기 스폰 루프 및 타이머 태스크 소멸 릴리즈
            if (m_viewModel != null)
            {
                m_viewModel.Dispose();
                m_viewModel = null;
            }
        }

        #endregion

        #region 내부 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 밸런스 설정에 입각하여 MVVM 단방향 의존성 결합을 진행하고 게임을 공식 스타트시킵니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void InitializeGradeRunner()
        {
            if (m_config == null)
            {
                Debug.LogError("[GradeRunnerInitializer] GradeRunnerConfigSO 에셋이 인스펙터에 누락되었습니다! 시스템 가동 불능.");
                return;
            }

            // 1. Model 생성 (POCO)
            var model = new GradeRunnerModel(
                m_config.StartGradePoint, 
                m_config.MaxGradePoint, 
                m_config.GameDuration
            );

            // 2. ViewModel 생성 (POCO)
            m_viewModel = new GradeRunnerViewModel(model, m_config, m_dialogueSO, m_playerSO);

            // 3. View 의존성 주입 (Dependency Injection)
            if (m_hudView != null)
            {
                m_hudView.Initialize(m_viewModel);
            }
            else
            {
                Debug.LogWarning("[GradeRunnerInitializer] GradeRunnerHudView 참조가 누락되었습니다.");
            }

            if (m_playerView != null)
            {
                m_playerView.Initialize(m_viewModel);
            }
            else
            {
                Debug.LogWarning("[GradeRunnerInitializer] GradeRunnerPlayerView 참조가 누락되었습니다.");
            }

            if (m_spawnerView != null)
            {
                m_spawnerView.Initialize(m_viewModel);
            }
            else
            {
                Debug.LogWarning("[GradeRunnerInitializer] FallingObjectSpawnerView 참조가 누락되었습니다.");
            }

            if (m_resultPopup != null)
            {
                m_resultPopup.Initialize(m_viewModel);
            }
            else
            {
                Debug.LogWarning("[GradeRunnerInitializer] GradeRunnerResultPopupView 참조가 누락되었습니다.");
            }

            if (m_professorView != null)
            {
                m_professorView.Initialize(m_viewModel);
            }
            else
            {
                Debug.LogWarning("[GradeRunnerInitializer] ProfessorView 참조가 누락되었습니다.");
            }

            // 4. 피하기 미니게임 공식 가동 개시
            m_viewModel.StartGame();
            
            Debug.Log("[GradeRunnerInitializer] 2D 피하기 미니게임(GradeRunner) 모든 MVVM 레이어 의존성 주입 완료 및 게임 시작 성공.");
        }

        #endregion
    }
}
