using UnityEngine;
using VContainer;
using VContainer.Unity;
using GamifyKWU.UI.Title;
using GameArifiction.Player;
using GameArifiction.Interaction;
using GameArifiction.Map;
using GamifyKWU.UI.Dashboard.Models;
using GamifyKWU.UI.Dashboard.ViewModels;
using GamifyKWU.UI.Dashboard.Views;
using GameArifiction.UI.FinalResult;
using GameArifiction.UI.Common;

/// <summary>
/// [기능]: 로비 씬의 모든 의존성(PlayerSO, UIManager, TitleView, IntroCutsceneController, PlayerView 등)을 VContainer 컨테이너에 자동 추출 및 등록하는 수명주기 스코프 클래스
/// [작성자]: 윤승종
/// </summary>
public class LobbyLifetimeScope : LifetimeScope
{
    #region 의존성 설정 (Configure)

    /// <summary>
    /// [기능]: 씬 컴포넌트 및 비즈니스 로직 클래스들의 의존성 바인딩을 수행합니다.
    /// [작성자]: 윤승종
    /// [수정 날짜]: 2026-06-12
    /// [마지막 수정 작성자]: 윤승종
    /// [수정 내용]: 씬 내의 모든 PortalView 컴포넌트에 PlayerSO가 자동으로 주입되도록 수동 의존성 주입 콜백 추가
    /// </summary>
    /// <param name="builder">VContainer 빌더 컨테이너</param>
    protected override void Configure(IContainerBuilder builder)
    {
        Debug.Log("[LobbyLifetimeScope] 로비 씬의 의존성 바인딩 구성을 개시합니다.");

        // 1. 하이어라키 내의 뷰들 중 인게임 PlayerView를 태그로 명시적 탐색 및 등록
        PlayerView playerView = null;
        GameObject playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            playerView = playerGo.GetComponent<PlayerView>();
        }
        else
        {
            playerView = FindFirstObjectByType<PlayerView>();
        }

        if (playerView != null)
        {
            builder.RegisterComponent(playerView);
            
            // 2. PlayerView에 부착되어 있는 PlayerSO 데이터 자산을 자동 주입 인스턴스로 등록 (수동 드래그 불필요!)
            if (playerView.PlayerSO != null)
            {
                builder.RegisterInstance(playerView.PlayerSO);
            }
            else
            {
                Debug.LogWarning("[LobbyLifetimeScope] PlayerView 내부에 PlayerSO 자산이 할당되어 있지 않습니다.");
            }
        }
        else
        {
            Debug.LogError("[LobbyLifetimeScope] 씬 내에서 PlayerView를 찾을 수 없습니다.");
        }

        // 3. 나머지 씬 하이어라키 뷰 자동 등록
        builder.RegisterComponentInHierarchy<UIManager>();
        
        // 뷰들이 비활성화(Inactive) 상태로 씬에 배치될 경우 VContainer 탐색 실패 예방
        SafeRegisterComponent<TitleView>(builder);
        SafeRegisterComponent<IntroCutsceneController>(builder);
        SafeRegisterComponent<MapView>(builder);
        SafeRegisterComponent<DashboardView>(builder);
        SafeRegisterComponent<InteractionUI_View>(builder);
        SafeRegisterComponent<CommonSettingsPopupView>(builder);

        // 4. 로비 씬 중앙 진입점 (LobbyFlowController) 엔트리포인트 등록
        builder.RegisterEntryPoint<GamifyKWU.Lobby.LobbyFlowController>(Lifetime.Scoped);

        // 4-1. 도메인 모델, 뷰모델 및 팩토리 등록
        builder.Register<PlayerViewModelFactory>(Lifetime.Scoped);
        builder.Register<MapModel>(Lifetime.Scoped);
        builder.Register<MapViewModel>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        
        // 추가 등록: Title 도메인
        builder.Register(container => new TitleModel("Lobby"), Lifetime.Scoped);
        builder.Register<TitleViewModel>(Lifetime.Scoped);

        // 추가 등록: Dashboard 도메인
        builder.Register(container => 
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("Localization/zh_CN");
            string jsonText = jsonAsset != null ? jsonAsset.text : "{}";
            return new DashboardModel(jsonText);
        }, Lifetime.Scoped);
        builder.Register<DashboardViewModel>(Lifetime.Scoped);

        // 추가 등록: FinalResult 도메인
        builder.Register<FinalResultModel>(Lifetime.Scoped);
        builder.Register<FinalResultViewModel>(Lifetime.Scoped);
        builder.Register<GameEndingViewModel>(Lifetime.Scoped);
        SafeRegisterComponent<FinalResultPopupView>(builder);
        SafeRegisterComponent<GameEndingView>(builder);
        SafeRegisterComponent<FinalResultInteractableView>(builder);

        // 추가 등록: Interaction 도메인
        builder.Register<InteractionUI_Model>(Lifetime.Scoped);
        builder.Register<InteractionUI_ViewModel>(Lifetime.Scoped);

        // 추가 등록: 공통 설정 도메인
        builder.Register<CommonSettingsViewModel>(Lifetime.Scoped);

        // 5. 공통 사운드 시스템 등록 (전역 유지)
        var soundView = FindFirstObjectByType<GameArifiction.Core.Audio.SoundPlayerView>();
        if (soundView == null)
        {
            var go = new GameObject("SoundPlayerView");
            soundView = go.AddComponent<GameArifiction.Core.Audio.SoundPlayerView>();
        }
        builder.RegisterComponent(soundView);
        builder.Register<GameArifiction.Core.Audio.SoundService>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        
        // 씬 생성/전환 시 의존성을 수동 주입하여 갱신
        builder.RegisterBuildCallback(container =>
        {
            if (soundView != null)
            {
                container.Inject(soundView);
            }

            // 씬 내의 타이틀용 연출 캐릭터(PlayerView)를 찾아 수동으로 의존성 주입 실행하여 팩토리 오류 제거
            var titlePlayerGo = GameObject.Find("Player_TitleIntro");
            if (titlePlayerGo != null)
            {
                var titlePlayerView = titlePlayerGo.GetComponent<PlayerView>();
                if (titlePlayerView != null)
                {
                    container.Inject(titlePlayerView);
                }
            }
            else
            {
                // 비활성화 상태인 경우를 대비하여 하이어라키 전체 검색 (격리 구역 내)
                var titlePlayerView = FindFirstObjectByType<PlayerView>(FindObjectsInactive.Include);
                if (titlePlayerView != null && titlePlayerView.gameObject.name == "Player_TitleIntro")
                {
                    container.Inject(titlePlayerView);
                }
            }

            // 씬 내의 모든 PortalView 인스턴스를 탐색하여 수동으로 의존성 주입 실행
            PortalView[] portals = UnityEngine.Object.FindObjectsByType<PortalView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < portals.Length; i++)
            {
                if (portals[i] != null)
                {
                    container.Inject(portals[i]);
                }
            }
        });
    }

    #endregion

    #region 내부 도우미 메서드 (Helper)
    /// <summary>
    /// [기능]: 씬에서 비활성화된 상태의 컴포넌트라도 찾아 VContainer에 안전하게 등록합니다.
    /// </summary>
    private void SafeRegisterComponent<T>(IContainerBuilder builder) where T : MonoBehaviour
    {
        var comp = FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (comp != null)
        {
            builder.RegisterComponent(comp);
        }
        else
        {
            Debug.LogWarning($"[LobbyLifetimeScope] 씬 내에 {typeof(T).Name} 컴포넌트가 존재하지 않아 등록을 건너뜁니다. (아직 배치하지 않았다면 무시해도 됩니다)");
        }
    }
    #endregion
}
