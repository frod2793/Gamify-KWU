using UnityEngine;
using VContainer;
using VContainer.Unity;
using GameArifiction.Player;
using GameArifiction.GradeRunner;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)의 VContainer 의존성 설정 스코프 클래스입니다.
///         싱글톤을 제거하고 LifetimeScope을 사용하여 Model, ViewModel, View 컴포넌트를 구성 및 바인딩합니다.
/// [작성자]: 윤승종
/// </summary>
public class GradeRunnerLifetimeScope : LifetimeScope
{
    #region 인스펙터 참조 (SerializeField)
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
    private PlayerSO m_playerSO;
    #endregion

    #region 의존성 설정 (VContainer Configure)
    protected override void Configure(IContainerBuilder builder)
    {
        // 1. ScriptableObject 에셋 등록
        if (m_config != null)
        {
            builder.RegisterInstance(m_config);
        }
        else
        {
            Debug.LogError("[GradeRunnerLifetimeScope] GradeRunnerConfigSO가 누락되었습니다.");
        }

        if (m_dialogueSO != null)
        {
            builder.RegisterInstance(m_dialogueSO);
        }
        else
        {
            Debug.LogError("[GradeRunnerLifetimeScope] GradeRunnerDialogueSO가 누락되었습니다.");
        }

        if (m_playerSO != null)
        {
            builder.RegisterInstance(m_playerSO);
        }
        else
        {
            Debug.LogError("[GradeRunnerLifetimeScope] PlayerSO가 누락되었습니다.");
        }

        // 2. Model 등록 (Factory)
        builder.Register(container =>
        {
            var config = container.Resolve<GradeRunnerConfigSO>();
            return new GradeRunnerModel(
                config.StartGradePoint,
                config.MaxGradePoint,
                config.GameDuration
            );
        }, Lifetime.Scoped);

        // 3. ViewModel 등록
        builder.RegisterEntryPoint<GradeRunnerViewModel>(Lifetime.Scoped)
            .AsSelf();

        // 4. View 레이어 자동 탐색 및 바인딩 (규칙 10 - RegisterComponentInHierarchy 활용)
        ConfigureViews(builder);
    }
    #endregion

    #region 내부 헬퍼 메서드 (Private Methods)
    private void ConfigureViews(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<GradeRunnerHudView>();
        builder.RegisterComponentInHierarchy<GradeRunnerPlayerView>();
        builder.RegisterComponentInHierarchy<FallingObjectSpawnerView>();
        builder.RegisterComponentInHierarchy<GradeRunnerResultPopupView>();
        builder.RegisterComponentInHierarchy<ProfessorView>();
    }
    #endregion
}
