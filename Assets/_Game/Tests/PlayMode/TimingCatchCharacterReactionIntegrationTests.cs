using System.Collections;
using System.Reflection;
using GameArifiction.TimingCatch;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameArifiction.Tests.PlayMode
{
    /// <summary>
    /// [기능]: TimingCatch 씬 런타임에서 SPUM 성공·실패 캐릭터 반응이 정상 실행되는지 검증합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [TestFixture]
    public sealed class TimingCatchCharacterReactionIntegrationTests
    {
        #region 테스트 (Tests)
        /// <summary>
        /// [기능]: TimingCatch 씬을 로드해 성공 반응과 피격 효과 생성·정리를 순서대로 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 캐릭터 반응 PlayMode 통합 검증 추가.
        /// </summary>
        [UnityTest]
        public IEnumerator ReactionView_InTimingCatchScene_PlaysSuccessAndFailure()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("TimingCatch", LoadSceneMode.Single);
            Assert.IsNotNull(loadOperation, "TimingCatch 씬 로드 작업을 시작해야 합니다.");
            while (loadOperation.isDone == false)
            {
                yield return null;
            }

            yield return null;

            TimingCatchCharacterView characterView = Object.FindFirstObjectByType<TimingCatchCharacterView>();
            Assert.IsNotNull(characterView, "씬에 타이밍 캐릭터 반응 View가 필요합니다.");

            characterView.PlaySuccessReaction();
            yield return null;

            characterView.PlayFailureReaction();
            yield return null;

            FieldInfo activeEffectField = typeof(TimingCatchCharacterView).GetField(
                "m_activeHitEffect",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(activeEffectField);
            Assert.IsNotNull(activeEffectField.GetValue(characterView), "실패 반응에서 피격 효과가 생성되어야 합니다.");

            characterView.ResetToIdle();
            yield return null;

            Assert.IsNull(activeEffectField.GetValue(characterView), "IDLE 복귀 시 피격 효과가 정리되어야 합니다.");
        }
        #endregion
    }
}
