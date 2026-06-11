using UnityEngine;
using VContainer;
using VContainer.Unity;
using GameArifiction.Player;
using GamifyKWU.CraneGame.Data;
using GameArifiction.QuizClassic;
using GameArifiction.Core.Audio;
using GameArifiction.UI.Common;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기(ClawMachine) 및 연계 클래식 퀴즈(QuizClassic) 통합 미니게임 씬의 VContainer 의존성 설정 스코프 클래스입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawGameLifetimeScope : LifetimeScope
    {
        #region 물리 및 데이터 참조 (Inspector)
        [SerializeField]
        [Tooltip("인게임 세션 정보가 담긴 ScriptableObject 데이터 자산입니다.")]
        private PlayerSO m_playerSO;

        [SerializeField]
        [Tooltip("인스펙터에 할당할 퀴즈 데이터베이스 스크립터블 오브젝트입니다.")]
        private QuizDatabaseSO m_quizDatabase;

        [SerializeField]
        [Tooltip("생성된 인형(캡슐)들이 담길 부모 Transform 오브젝트입니다.")]
        private Transform m_dollsContainer;

        [SerializeField]
        [Tooltip("인형이 스폰될 영역을 나타내는 BoxCollider2D 컴포넌트입니다.")]
        private BoxCollider2D m_spawnAreaCollider;

        [SerializeField]
        [Tooltip("인형으로 스폰할 캡슐 프리팹입니다.")]
        private GameObject m_capsulePrefab;

        [SerializeField]
        [Tooltip("인형뽑기 월드 루트 오브젝트입니다. (결과 화면 전환 시 비활성화 목적)")]
        private GameObject m_clawMachineWorld;
        #endregion

        #region 의존성 설정 (VContainer Configure)
        protected override void Configure(IContainerBuilder builder)
        {
            // 1. 공용 데이터 자산 및 모델/뷰모델 바인딩
            if (m_playerSO != null)
            {
                builder.RegisterInstance(m_playerSO);
            }
            else
            {
                Debug.LogWarning("[ClawGameLifetimeScope] PlayerSO가 설정되지 않았습니다. 씬 하이어라키를 확인하십시오.");
            }

            if (m_quizDatabase != null)
            {
                builder.RegisterInstance(m_quizDatabase);
            }
            else
            {
                Debug.LogWarning("[ClawGameLifetimeScope] QuizDatabaseSO가 설정되지 않았습니다. 씬 하이어라키를 확인하십시오.");
            }

            // 2. 물리 씬 데이터 참조 DTO 생성 및 등록
            ClawSceneReferencesDTO sceneReferences = new ClawSceneReferencesDTO(
                m_dollsContainer,
                m_spawnAreaCollider,
                m_capsulePrefab,
                m_clawMachineWorld
            );
            builder.RegisterInstance(sceneReferences);

            // A. [인형뽑기(ClawMachine) 의존성 결합]
            builder.Register(container =>
            {
                var context = new ClawGameContextDTO(5, 120f, null);
                return new ClawMachineModel(context.MaxPlayCount, context.TimeLimitPerPlay);
            }, Lifetime.Scoped);

            builder.Register<ClawGameViewModel>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();

            // B. [뷰 레이어 자동 감지 및 등록 (수동 직렬화 배제 - 규칙 9번 준수)]
            ConfigureViews(builder);

            // C. [C# EntryPoint 진입점 제어 등록]
            builder.RegisterEntryPoint<ClawGameFlowController>();
            builder.Register<QuizClassicFlowController>(Lifetime.Scoped);

            // D. [공통 사운드 시스템 등록 (단독 실행 환경 지원)]
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
        /// <summary>
        /// [기능]: 씬 내 하이어라키에서 뷰 컴포넌트들을 찾아 VContainer에 등록합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-11
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: CommonPausePopupView 수동 탐색 등록 로직 제거 (인스펙터 직접 바인딩 전환)
        /// </summary>
        private void ConfigureViews(IContainerBuilder builder)
        {
            // 인형뽑기(ClawMachine) 뷰
            builder.RegisterComponentInHierarchy<ClawGameView>();
            builder.RegisterComponentInHierarchy<QuizUI_View>();
            builder.RegisterComponentInHierarchy<ClawMachineExitView>();
            builder.RegisterComponentInHierarchy<ClawGameResultPopupView>();
            builder.RegisterComponentInHierarchy<ClawGameTutorialPopupView>();

            // 클래식 퀴즈(QuizClassic) 뷰
            builder.RegisterComponentInHierarchy<QuizClassicView>();

            var commonPopup = FindAnyObjectByType<CommonResultPopupView>();
            if (commonPopup != null)
            {
                builder.RegisterComponent(commonPopup);
                Debug.Log("[ClawGameLifetimeScope] CommonResultPopupView 컨테이너 등록 완료.");
            }
            else
            {
                Debug.LogWarning("[ClawGameLifetimeScope] CommonResultPopupView가 씬에 존재하지 않습니다! 수동 배치가 필요합니다.");
            }
        }
        #endregion
    }
}
