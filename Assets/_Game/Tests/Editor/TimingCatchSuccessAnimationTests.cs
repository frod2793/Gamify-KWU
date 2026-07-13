using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameArifiction.Tests.Editor
{
    /// <summary>
    /// [기능]: 피격 클립 바인딩을 기반으로 제작한 타이밍 성공 애니메이션 품질을 검증합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [TestFixture]
    public sealed class TimingCatchSuccessAnimationTests
    {
        #region 상수 (Constants)
        private const string SuccessClipPath = "Assets/_Game/Animations/TimingCatch/TimingCatchSuccess.anim";
        #endregion

        #region 테스트 (Tests)
        /// <summary>
        /// [기능]: 성공 클립의 프레임률·길이·루프·SPUM 바인딩·2회 점프 피크를 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이밍 성공 애니메이션 에셋 품질 검증 추가.
        /// </summary>
        [Test]
        public void SuccessClip_HasTwoJumpReactionAndSpumBindings()
        {
            AnimationClip successClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SuccessClipPath);
            Assert.IsNotNull(successClip, "타이밍 성공 애니메이션 클립이 필요합니다.");
            Assert.AreEqual(60f, successClip.frameRate, 0.01f);
            Assert.AreEqual(0.9f, successClip.length, 0.01f);
            Assert.IsFalse(AnimationUtility.GetAnimationClipSettings(successClip).loopTime);

            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(successClip);
            HashSet<string> bindingPaths = new HashSet<string>();
            for (int i = 0; i < bindings.Length; i++)
            {
                bindingPaths.Add(bindings[i].path);
            }

            Assert.That(bindingPaths, Does.Contain("Root"));
            Assert.That(bindingPaths, Does.Contain("Root/BodySet/P_Body"));
            Assert.That(bindingPaths, Does.Contain("Root/BodySet/P_Body/ArmSet/ArmL/P_LArm"));
            Assert.That(bindingPaths, Does.Contain("Root/BodySet/P_Body/ArmSet/ArmR/P_RArm"));
            Assert.That(bindingPaths, Does.Contain("Root/BodySet/P_Body/HeadSet/P_Head"));
            Assert.That(bindingPaths, Does.Contain("Root/P_LFoot"));
            Assert.That(bindingPaths, Does.Contain("Root/P_RFoot"));
            Assert.That(bindingPaths, Does.Contain("Shadow"));

            AnimationCurve rootPositionY = FindCurve(successClip, bindings, "Root", "m_LocalPosition.y");
            Assert.IsNotNull(rootPositionY, "Root 높이 곡선이 필요합니다.");

            int jumpPeakCount = 0;
            Keyframe[] keys = rootPositionY.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].value >= 0.25f)
                {
                    jumpPeakCount++;
                }
            }

            Assert.GreaterOrEqual(jumpPeakCount, 2, "성공 반응에는 두 번의 점프 피크가 필요합니다.");
            Assert.AreEqual(0f, keys[keys.Length - 1].value, 0.001f);
        }
        #endregion

        #region 테스트 보조 메서드 (Test Helpers)
        /// <summary>
        /// [기능]: 지정 경로와 프로퍼티에 해당하는 애니메이션 곡선을 반환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 성공 클립 Root 높이 곡선 탐색 추가.
        /// </summary>
        private static AnimationCurve FindCurve(
            AnimationClip clip,
            EditorCurveBinding[] bindings,
            string path,
            string propertyName
        )
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i].path == path && bindings[i].propertyName == propertyName)
                {
                    return AnimationUtility.GetEditorCurve(clip, bindings[i]);
                }
            }

            return null;
        }
        #endregion
    }
}
