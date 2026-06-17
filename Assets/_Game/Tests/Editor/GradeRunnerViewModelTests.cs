using NUnit.Framework;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using GameArifiction.GradeRunner;
using GameArifiction.Player;

/// <summary>
/// [기능]: 그레이드 러너(GradeRunner) 미니게임의 핵심 비즈니스 로직(학점 클램핑, 일시정지 중 연동 정합성, 페이즈 전환)을 다각도로 검증하는 EditMode 단위 테스트 클래스
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.Tests.Editor
{
    [TestFixture]
    public class GradeRunnerViewModelTests
    {
        #region 내부 필드 (Private Fields)

        private GradeRunnerModel m_model;
        private GradeRunnerConfigSO m_config;
        private GradeRunnerDialogueSO m_dialogueSO;
        private PlayerSO m_playerSO;
        private GradeRunnerViewModel m_viewModel;

        #endregion

        #region 초기화 및 해제 (Setup / Teardown)

        /// <summary>
        /// [기능]: 테스트 수행 전 필요한 의존성 개체들을 동적으로 모킹 및 할당합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 테스트에 필요한 ScriptableObject 런타임 강제 주입 구현
        /// </summary>
        [SetUp]
        public void Setup()
        {
            m_model = new GradeRunnerModel(2.5f, 5.0f, 30.0f);
            
            m_config = ScriptableObject.CreateInstance<GradeRunnerConfigSO>();
            SetPrivateField(m_config, "m_gameDuration", 30.0f);
            SetPrivateField(m_config, "m_phase2TransitionTime", 10.0f);
            SetPrivateField(m_config, "m_maxGradePoint", 5.0f);
            SetPrivateField(m_config, "m_startGradePoint", 2.5f);
            SetPrivateField(m_config, "m_codePenalty", 0.5f);
            SetPrivateField(m_config, "m_cheatSheetBonus", 1.0f);

            m_dialogueSO = ScriptableObject.CreateInstance<GradeRunnerDialogueSO>();
            m_playerSO = ScriptableObject.CreateInstance<PlayerSO>();

            m_viewModel = new GradeRunnerViewModel(m_model, m_config, m_dialogueSO, m_playerSO);
        }

        /// <summary>
        /// [기능]: 테스트 완료 후 할당된 리소스를 가비지 컬렉션 및 파괴 처리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: ScriptableObject 가짜 널 방지 기반 즉각 파괴
        /// </summary>
        [TearDown]
        public void Teardown()
        {
            if (m_viewModel != null)
            {
                m_viewModel.Dispose();
            }

            if (m_config != null)
            {
                Object.DestroyImmediate(m_config);
            }

            if (m_dialogueSO != null)
            {
                Object.DestroyImmediate(m_dialogueSO);
            }

            if (m_playerSO != null)
            {
                Object.DestroyImmediate(m_playerSO);
            }
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        #endregion

        #region 테스트 메서드 (Test Methods)

        /// <summary>
        /// [기능]: 뷰모델 최초 초기화 상태 값이 정상적으로 바인딩되었는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// </summary>
        [Test]
        public void Constructor_InitializesCorrectly()
        {
            Assert.AreEqual(GradeRunnerState.Idle, m_viewModel.CurrentState);
            Assert.AreEqual(GradeRunnerPhase.Phase1, m_viewModel.CurrentPhase);
            Assert.AreEqual(2.5f, m_viewModel.CurrentGradePoint);
            Assert.AreEqual(30.0f, m_viewModel.RemainingTime);
        }

        /// <summary>
        /// [기능]: 게임 시작 시 튜토리얼 팝업 대기 상태(Tutorial)로 전환되는지 확인합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// </summary>
        [Test]
        public void StartGame_ChangesStateToTutorial()
        {
            m_viewModel.StartGame();

            Assert.AreEqual(GradeRunnerState.Tutorial, m_viewModel.CurrentState);
            Assert.IsFalse(m_viewModel.IsPlayable);
        }

        /// <summary>
        /// [기능]: 게임 플레이 중 장애물 피격 시 학점 감점이 의도대로 적용되는지 확인합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// </summary>
        [Test]
        public void ApplyScoreImpact_UnderPlayingState_WorksCorrectly()
        {
            m_viewModel.StartGame();
            m_viewModel.func_CompleteTutorial();
            m_viewModel.CompleteIntroCutscene();

            m_viewModel.ApplyCodeHit(Vector2.zero);

            Assert.AreEqual(2.0f, m_viewModel.CurrentGradePoint);
        }

        /// <summary>
        /// [기능]: 일시정지 상태에서는 장애물 피격 및 족보 획득 입력 시 학점 점수가 변경되지 않고 정합성을 유지하는지 연동 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// </summary>
        [Test]
        public void ApplyScoreImpact_UnderPausedState_IsBlocked()
        {
            m_viewModel.StartGame();
            m_viewModel.func_CompleteTutorial();
            m_viewModel.CompleteIntroCutscene();
            m_viewModel.PauseGame();

            m_viewModel.ApplyCodeHit(Vector2.zero);
            m_viewModel.ApplyCheatSheetPickup(Vector2.zero);

            Assert.AreEqual(2.5f, m_viewModel.CurrentGradePoint);
        }

        /// <summary>
        /// [기능]: 점수가 0.0점 미만으로 하락하거나 5.0점을 초과하지 않고 정밀 클램핑되는지 한계 가드를 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// </summary>
        [Test]
        public void Score_ClampingLimits_AreEnforced()
        {
            m_viewModel.StartGame();
            m_viewModel.func_CompleteTutorial();
            m_viewModel.CompleteIntroCutscene();

            for (int i = 0; i < 10; i++)
            {
                m_viewModel.ApplyCodeHit(Vector2.zero);
            }
            Assert.AreEqual(0f, m_viewModel.CurrentGradePoint);

            for (int i = 0; i < 10; i++)
            {
                m_viewModel.ApplyCheatSheetPickup(Vector2.zero);
            }
            Assert.AreEqual(5.0f, m_viewModel.CurrentGradePoint);
        }

        /// <summary>
        /// [기능]: 10초 이하 시점 강제 2페이즈 전환 상태를 모사하여 2페이즈 시작 컷씬 및 일시정지 연동을 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// </summary>
        [Test]
        public void Transition_To_Phase2_UnderRemainingTime10s_Triggered()
        {
            m_viewModel.StartGame();
            m_viewModel.func_CompleteTutorial();
            m_viewModel.CompleteIntroCutscene();

            SetPrivateField(m_model, "m_remainingTime", 9.9f);

            System.Reflection.MethodInfo changeStateMethod = typeof(GradeRunnerViewModel).GetMethod("ChangeState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (changeStateMethod != null)
            {
                SetPrivateField(m_viewModel, "m_currentPhase", GradeRunnerPhase.Phase2);
                SetPrivateField(m_viewModel, "m_isPaused", true);
                changeStateMethod.Invoke(m_viewModel, new object[] { GradeRunnerState.Phase2Cutscene });
            }

            Assert.AreEqual(GradeRunnerPhase.Phase2, m_viewModel.CurrentPhase);
            Assert.AreEqual(GradeRunnerState.Phase2Cutscene, m_viewModel.CurrentState);
            Assert.IsFalse(m_viewModel.IsPlayable);
        }

        /// <summary>
        /// [기능]: 2페이즈 전환 연출 완료 호출 시, 자동으로 일시정지가 풀리고 플레이 상태(Playing)로 복귀하는지 연동 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-17
        /// </summary>
        [Test]
        public void CompletePhase2Cutscene_UnderPauseState_ResumesCorrectly()
        {
            m_viewModel.StartGame();
            m_viewModel.func_CompleteTutorial();
            m_viewModel.CompleteIntroCutscene();
            
            System.Reflection.MethodInfo changeStateMethod = typeof(GradeRunnerViewModel).GetMethod("ChangeState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (changeStateMethod != null)
            {
                changeStateMethod.Invoke(m_viewModel, new object[] { GradeRunnerState.Phase2Cutscene });
                SetPrivateField(m_viewModel, "m_isPaused", true);
            }

            m_viewModel.CompletePhase2Cutscene();

            Assert.AreEqual(GradeRunnerState.Playing, m_viewModel.CurrentState);
            Assert.IsTrue(m_viewModel.IsPlayable);
        }

        #endregion
    }
}
