using UnityEngine;
using VContainer;
using VContainer.Unity;
using GamifyKWU.UI.Title;
using GameArifiction.Player;
using GameArifiction.Interaction;

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
    /// </summary>
    /// <param name="builder">VContainer 빌더 컨테이너</param>
    protected override void Configure(IContainerBuilder builder)
    {
        Debug.Log("[LobbyLifetimeScope] 로비 씬의 의존성 바인딩 구성을 개시합니다.");

        // 1. 하이어라키 내의 뷰들 중 PlayerView를 먼저 자동 탐색 및 등록
        var playerView = FindFirstObjectByType<PlayerView>();
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
        builder.RegisterComponentInHierarchy<TitleView>();
        builder.RegisterComponentInHierarchy<IntroCutsceneController>();

        // 4. 로비 씬 중앙 진입점 (LobbyFlowController) 엔트리포인트 등록
        builder.RegisterEntryPoint<GamifyKWU.Lobby.LobbyFlowController>(Lifetime.Scoped);
    }

    #endregion
}
