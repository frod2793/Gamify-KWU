using UnityEngine;
using VContainer;
using VContainer.Unity;
using GameArifiction.Player;

namespace GameArifiction.CardMatch
{
    /// <summary>
    /// [기능]: 카드 맞추기 씬의 VContainer 의존성 주입 컨테이너입니다.
    /// [작성자]: 김지연
    /// </summary>
    public class CardMatchLifetimeScope : LifetimeScope
    {
        #region Private Fields (인스펙터 할당 필드)
        [SerializeField]
        [Tooltip("플레이어 데이터가 담긴 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;

        [SerializeField]
        [Tooltip("카드 맞추기 게임의 난이도 및 학점 기준이 설정된 ScriptableObject 데이터 자산입니다.")]
        private CardMatchSettingsSO m_settings;
        #endregion

        #region 의존성 설정 (VContainer Configure)
        protected override void Configure(IContainerBuilder builder)
        {
            // 1. 공용 데이터 자산 등록
            if (m_playerSO != null)
            {
                builder.RegisterInstance(m_playerSO);
            }
            else
            {
                Debug.LogWarning("[CardMatchLifetimeScope] PlayerSO가 설정되지 않았습니다.");
            }

            if (m_settings != null)
            {
                builder.RegisterInstance(m_settings);
            }
            else
            {
                Debug.LogWarning("[CardMatchLifetimeScope] CardMatchSettingsSO가 설정되지 않았습니다.");
            }

            // 2. 뷰 컴포넌트 자동 탐색 및 등록
            builder.RegisterComponentInHierarchy<CardMatchView>();
            builder.RegisterComponentInHierarchy<CardMatchTitleView>();
            builder.RegisterComponentInHierarchy<CardMatchResultPopupView>();

            // 3. EntryPoint (C# 진입점) 및 씬 제어 등록
            builder.RegisterEntryPoint<CardMatchFlowController>();
        }
        #endregion
    }
}
