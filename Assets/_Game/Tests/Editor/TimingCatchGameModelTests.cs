using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GameArifiction.TimingCatch;

namespace GameArifiction.Tests.Editor
{
    /// <summary>
    /// [기능]: 타이밍 캐치 모델의 스테이지별 게이지 속도 선택과 방어 규칙을 검증합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [TestFixture]
    public sealed class TimingCatchGameModelTests
    {
        #region 내부 필드 (Private Fields)
        private TimingCatchGameConfigSO m_config;
        #endregion

        #region 테스트 생명주기 (Test Lifecycle)
        /// <summary>
        /// [기능]: 각 테스트에서 사용할 설정 에셋 인스턴스를 생성합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 스테이지별 속도 모델 테스트 초기화 추가.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            m_config = ScriptableObject.CreateInstance<TimingCatchGameConfigSO>();
        }

        /// <summary>
        /// [기능]: 테스트에서 생성한 설정 에셋 인스턴스를 정리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 테스트 종료 시 Unity 오브젝트 정리 추가.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (m_config != null)
            {
                Object.DestroyImmediate(m_config);
            }
        }
        #endregion

        #region 테스트 (Tests)
        /// <summary>
        /// [기능]: 게임 시작 시 첫 스테이지에 지정된 속도가 적용되는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 첫 스테이지 개별 속도 검증 추가.
        /// </summary>
        [Test]
        public void Constructor_WithStageSpeeds_AppliesFirstStageSpeed()
        {
            SetStageSpeeds(0.25f, 0.75f, 1.5f);

            var model = new TimingCatchGameModel(m_config);

            Assert.AreEqual(0.25f, model.CurrentSpeed, 0.0001f);
            Assert.AreEqual(3, model.MaxStageCount);
        }

        /// <summary>
        /// [기능]: 기본 설정이 기존 7단계 체감 속도를 보존하는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 기본 7단계 및 마지막 속도 2.2 검증 추가.
        /// </summary>
        [Test]
        public void DefaultConfig_HasSevenStagesAndLastStageSpeedIsTwoPointTwo()
        {
            var serializedConfig = new SerializedObject(m_config);
            SerializedProperty speedsProperty = serializedConfig.FindProperty("m_stageGaugeSpeeds");
            Assert.IsNotNull(speedsProperty, "스테이지별 속도 직렬화 필드가 필요합니다.");
            Assert.AreEqual(7, speedsProperty.arraySize);

            var model = new TimingCatchGameModel(m_config);
            for (int i = 1; i < 7; i++)
            {
                model.AdvanceToNextStage();
            }

            Assert.AreEqual(2.2f, model.CurrentSpeed, 0.0001f);
        }

        /// <summary>
        /// [기능]: 스테이지 전환마다 해당 인덱스의 독립 속도가 적용되는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 비선형 스테이지 속도 전환 검증 추가.
        /// </summary>
        [Test]
        public void AdvanceToNextStage_WithNonLinearSpeeds_AppliesMatchingStageSpeed()
        {
            SetStageSpeeds(0.5f, 2.25f, 0.8f);
            var model = new TimingCatchGameModel(m_config);

            model.AdvanceToNextStage();
            Assert.AreEqual(2.25f, model.CurrentSpeed, 0.0001f);

            model.AdvanceToNextStage();
            Assert.AreEqual(0.8f, model.CurrentSpeed, 0.0001f);
        }

        /// <summary>
        /// [기능]: 음수로 설정된 스테이지 속도가 0으로 보정되는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 잘못된 음수 속도 방어 검증 추가.
        /// </summary>
        [Test]
        public void Constructor_WithNegativeSpeed_ClampsSpeedToZero()
        {
            SetStageSpeeds(-3f);

            var model = new TimingCatchGameModel(m_config);

            Assert.AreEqual(0f, model.CurrentSpeed, 0.0001f);
        }

        /// <summary>
        /// [기능]: 빈 속도 배열이 안전 기본값 1과 단일 스테이지로 대체되는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 빈 배열 폴백 검증 추가.
        /// </summary>
        [Test]
        public void Constructor_WithEmptySpeeds_UsesSafeDefaultStage()
        {
            SetStageSpeeds();

            var model = new TimingCatchGameModel(m_config);

            Assert.AreEqual(1f, model.CurrentSpeed, 0.0001f);
            Assert.AreEqual(1, model.MaxStageCount);
        }
        #endregion

        #region 테스트 도우미 (Test Helpers)
        /// <summary>
        /// [기능]: 직렬화 프로퍼티를 통해 테스트용 스테이지 속도 배열을 설정합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 인스펙터 직렬화 경로와 동일한 테스트 설정 도우미 추가.
        /// </summary>
        private void SetStageSpeeds(params float[] speeds)
        {
            var serializedConfig = new SerializedObject(m_config);
            SerializedProperty speedsProperty = serializedConfig.FindProperty("m_stageGaugeSpeeds");
            Assert.IsNotNull(speedsProperty, "스테이지별 속도 직렬화 필드가 필요합니다.");

            speedsProperty.arraySize = speeds.Length;
            for (int i = 0; i < speeds.Length; i++)
            {
                speedsProperty.GetArrayElementAtIndex(i).floatValue = speeds[i];
            }

            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion
    }
}
