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
    protected override void Configure(IContainerBuilder builder)
    {
        #region ScriptableObject 등록
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
        #endregion

        #region 핵심 계층 등록
        builder.Register<SoundService>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();

        builder.Register<ITimingJudgeCalculator, TimingCatchJudgeCalculator>(Lifetime.Scoped);

        builder.Register(container =>
        {
            var config = container.Resolve<TimingCatchGameConfigSO>();
            return new TimingCatchGameModel(config);
        }, Lifetime.Scoped);

        builder.Register<TimingCatchGameViewModel>(Lifetime.Scoped);
        #endregion

        #region 뷰 등록
        builder.RegisterComponentInHierarchy<TimingCatchGameView>();
        builder.RegisterComponentInHierarchy<CommonResultPopupView>();

        var settingsPopup = FindFirstObjectByType<CommonSettingsPopupView>(FindObjectsInactive.Include);
        if (settingsPopup != null)
        {
            builder.RegisterComponent(settingsPopup);
        }
        else
        {
            Debug.LogWarning("[TimingCatchGameLifetimeScope] CommonSettingsPopupView가 씬에 없습니다.");
        }

        builder.Register<CommonSettingsViewModel>(Lifetime.Scoped);
        #endregion

        #region EntryPoint
        builder.RegisterEntryPoint<TimingCatchGameFlowController>(Lifetime.Scoped);
        builder.RegisterEntryPoint<TimingCatchGamePresenter>(Lifetime.Scoped);
        builder.RegisterEntryPoint<TimingCatchGameResultPresenter>(Lifetime.Scoped);
        #endregion
    }
    #endregion
}
