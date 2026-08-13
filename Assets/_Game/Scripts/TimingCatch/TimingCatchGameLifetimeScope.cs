using VContainer;
using VContainer.Unity;
using UnityEngine;
using GameArifiction.TimingCatch;
using GameArifiction.Player;
using GameArifiction.UI.Common;
using GameArifiction.Core.Audio;
using EasyTransition;

/// <summary>
/// [기능]: 타이밍 게임 씬 DI 컨테이너.
/// [작성자]: 윤승종
/// </summary>
public class TimingCatchGameLifetimeScope : LifetimeScope
{
    #region 인스펙터 참조 (SerializeField)
    [Header("게임 설정")]
    [SerializeField]
    [Tooltip("타이밍 게임 난이도/점수 설정 ScriptableObject 입니다.")]
    private TimingCatchGameConfigSO m_config;

    [Header("플레이어 데이터")]
    [SerializeField]
    [Tooltip("플레이어 상태 저장용 ScriptableObject 입니다.")]
    private PlayerSO m_playerSO;

    [Header("장면 전환 설정")]
    [SerializeField]
    [Tooltip("로비 복귀 트랜지션 설정(선택값).")]
    private TransitionSettings m_transitionSettings;
    #endregion

    #region 의존성 설정 (VContainer Configure)
    /// <summary>
    /// [기능]: 타이밍 게임의 데이터·핵심 로직·View·EntryPoint 등록 순서를 구성합니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-07-13
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 기능 영역별 VContainer 구성 메서드로 분리.
    /// </summary>
    protected override void Configure(IContainerBuilder builder)
    {
        ConfigureData(builder);
        ConfigureCore(builder);
        ConfigureViews(builder);
        ConfigureEntryPoints(builder);
    }
    #endregion

    #region 내부 구성 메서드 (Private Configuration Methods)
    /// <summary>
    /// [기능]: 게임 설정, 플레이어 데이터, 장면 전환 설정을 컨테이너에 등록합니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-07-13
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: ScriptableObject 등록 로직 분리.
    /// </summary>
    private void ConfigureData(IContainerBuilder builder)
    {
        if (m_config != null)
        {
            builder.RegisterInstance(m_config);
        }
        else
        {
            Debug.LogWarning("[TimingCatchGameLifetimeScope] TimingCatchConfigSO 미등록. 기본 설정으로 생성합니다.");
            builder.RegisterInstance(ScriptableObject.CreateInstance<TimingCatchGameConfigSO>());
        }

        if (m_playerSO != null)
        {
            builder.RegisterInstance(m_playerSO);
        }
        else
        {
            Debug.LogWarning("[TimingCatchGameLifetimeScope] PlayerSO 미등록. 임시 객체를 생성합니다.");
            builder.RegisterInstance(ScriptableObject.CreateInstance<PlayerSO>());
        }

        if (m_transitionSettings != null)
        {
            builder.RegisterInstance(m_transitionSettings);
        }
        else
        {
            Debug.LogWarning("[TimingCatchGameLifetimeScope] TransitionSettings가 설정되지 않았습니다.");
        }
    }

    /// <summary>
    /// [기능]: 타이밍 게임의 서비스, 판정기, Model, ViewModel을 등록합니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-07-13
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: ViewModel을 판정 이벤트 소스 인터페이스로 함께 바인딩.
    /// </summary>
    private void ConfigureCore(IContainerBuilder builder)
    {
        // 공통 사운드 시스템 등록 (단독 실행 환경 지원)
        if (Parent == null)
        {
            var soundView = FindFirstObjectByType<SoundPlayerView>();
            if (soundView == null)
            {
                var soundObject = new GameObject("SoundPlayerView");
                soundView = soundObject.AddComponent<SoundPlayerView>();
            }
            builder.RegisterComponent(soundView);
            builder.Register<SoundService>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.RegisterBuildCallback(container => container.Inject(soundView));
        }

        builder.Register<ITimingJudgeCalculator, TimingCatchJudgeCalculator>(Lifetime.Scoped);

        builder.Register(container =>
        {
            TimingCatchGameConfigSO config = container.Resolve<TimingCatchGameConfigSO>();
            return new TimingCatchGameModel(config);
        }, Lifetime.Scoped);

        builder.Register<TimingCatchGameViewModel>(Lifetime.Scoped)
            .AsSelf()
            .AsImplementedInterfaces();

    }

    /// <summary>
    /// [기능]: 씬 하이어라키의 타이밍 게임 View와 캐릭터 반응 View를 자동 탐색하여 등록합니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-07-13
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: TimingCatchCharacterView 인터페이스 바인딩 추가.
    /// </summary>
    private void ConfigureViews(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<TimingCatchGameView>();
        builder.RegisterComponentInHierarchy<CommonResultPopupView>();
        builder.RegisterComponentInHierarchy<TimingCatchCharacterView>()
            .AsImplementedInterfaces();
    }

    /// <summary>
    /// [기능]: 게임 흐름, UI Presenter, 결과 Presenter, 캐릭터 반응 Presenter를 EntryPoint로 등록합니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-07-13
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: TimingCatchCharacterPresenter EntryPoint 추가.
    /// </summary>
    private void ConfigureEntryPoints(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<TimingCatchGameFlowController>(Lifetime.Scoped);
        builder.RegisterEntryPoint<TimingCatchGamePresenter>(Lifetime.Scoped);
        builder.RegisterEntryPoint<TimingCatchGameResultPresenter>(Lifetime.Scoped);
        builder.RegisterEntryPoint<TimingCatchCharacterPresenter>(Lifetime.Scoped);
    }
    #endregion
}
