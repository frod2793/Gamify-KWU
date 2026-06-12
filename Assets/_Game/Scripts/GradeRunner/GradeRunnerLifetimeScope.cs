using UnityEngine;
using VContainer;
using VContainer.Unity;
using GameArifiction.Player;
using GameArifiction.GradeRunner;
using GameArifiction.UI.Common;
using EasyTransition;
using GameArifiction.Core.Audio;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)의 VContainer 의존성 설정 스코프 클래스입니다.
///         싱글톤을 제거하고 LifetimeScope을 사용하여 Model, ViewModel, View 컴포넌트를 구성 및 바인딩합니다.
/// [작성자]: 윤승종
/// [수정 날짜]: 2026-06-12
/// [마지막 수정 작성자]: 윤승종
/// [수정 내용]: 비활성화된 공통 결과 팝업 뷰 탐색 방안 보완 (FindObjectsInactive.Include 추가)
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

    [Header("이지 트랜지션 설정")]
    [SerializeField]
    [Tooltip("로비 복귀 시 화면 전환을 위한 이지 트랜지션 설정 자산입니다.")]
    private TransitionSettings m_transitionSettings;
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

        if (m_transitionSettings != null)
        {
            builder.RegisterInstance(m_transitionSettings);
        }
        else
        {
            Debug.LogWarning("[GradeRunnerLifetimeScope] TransitionSettings가 누락되었습니다.");
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

        // 공용 설정 뷰모델 등록
        builder.Register<CommonSettingsViewModel>(Lifetime.Scoped);

        // 4. Presenter 등록 (POCO EntryPoint)
        builder.RegisterEntryPoint<GradeRunnerResultPresenter>(Lifetime.Scoped)
            .AsSelf();

        builder.RegisterEntryPoint<GradeRunnerAudioPresenter>(Lifetime.Scoped)
            .AsSelf();

        // 5. View 레이어 자동 탐색 및 바인딩 (규칙 10 - RegisterComponentInHierarchy 활용)
        ConfigureViews(builder);

        // 6. 공통 사운드 시스템 등록 (단독 실행 환경 지원)
        if (Parent == null)
        {
            var soundView = FindFirstObjectByType<SoundPlayerView>();
            if (soundView == null)
            {
                var go = new GameObject("SoundPlayerView");
                soundView = go.AddComponent<SoundPlayerView>();
            }
            builder.RegisterComponent(soundView);
            builder.Register<SoundService>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.RegisterBuildCallback(container => container.Inject(soundView));
        }
    }
    #endregion

    #region 내부 헬퍼 메서드 (Private Methods)
    private void ConfigureViews(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<GradeRunnerHudView>();
        builder.RegisterComponentInHierarchy<GradeRunnerPlayerView>();
        builder.RegisterComponentInHierarchy<FallingObjectSpawnerView>();
        builder.RegisterComponentInHierarchy<ProfessorView>();
        builder.RegisterComponentInHierarchy<GradeRunnerTutorialPopupView>();
        builder.RegisterComponentInHierarchy<CommonSettingsPopupView>();
        builder.RegisterComponentInHierarchy<CommonPausePopupView>();

        // 공통 결과 팝업 뷰가 씬에 존재할 경우 찾아 컨테이너에 등록(캐싱) (비활성화 상태의 오브젝트도 포함하여 탐색하도록 방어 수정)
        var commonPopup = FindFirstObjectByType<CommonResultPopupView>(FindObjectsInactive.Include);
        if (commonPopup != null)
        {
            builder.RegisterComponent(commonPopup);
            // Debug.Log("[GradeRunnerLifetimeScope] 공통 결과 팝업 뷰 컨테이너 등록 완료.");
        }
        else
        {
            Debug.LogWarning("[GradeRunnerLifetimeScope] CommonResultPopupView가 씬에 존재하지 않습니다! 수동 배치가 필요합니다.");
        }

    }
    #endregion
}
