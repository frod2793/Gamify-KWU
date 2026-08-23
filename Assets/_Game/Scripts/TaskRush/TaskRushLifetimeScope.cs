using GameArifiction.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameArifiction.TaskRush
{
    public sealed class TaskRushLifetimeScope : LifetimeScope
    {
        [SerializeField] private TaskRushConfigSO m_config;
        [SerializeField] private PlayerSO m_playerSO;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(m_config);
            builder.RegisterInstance(m_playerSO);
            builder.Register(container => new TaskRushModel(container.Resolve<TaskRushConfigSO>().GameDuration), Lifetime.Scoped);
            builder.Register<TaskRushViewModel>(Lifetime.Scoped);
            builder.RegisterComponentInHierarchy<TaskRushGameView>();
            builder.RegisterComponentInHierarchy<TaskRushPlayerView>();
        }
    }
}
