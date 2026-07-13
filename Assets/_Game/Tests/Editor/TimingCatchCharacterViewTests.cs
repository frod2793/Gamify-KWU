using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameArifiction.Tests.Editor
{
    /// <summary>
    /// [기능]: SPUM 기반 타이밍 캐치 캐릭터 View의 애니메이션 등록과 반응 출력을 검증합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [TestFixture]
    public sealed class TimingCatchCharacterViewTests
    {
        #region 상수 (Constants)
        private const string CharacterViewTypeName = "GameArifiction.TimingCatch.TimingCatchCharacterView";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";
        private const string DamagedClipPath = "Assets/ThirdParty/SPUM/Resources/Addons/Legacy/0_Unit/1_Animation/03_Damaged/0_Damaged.anim";
        #endregion

        #region 내부 필드 (Private Fields)
        private GameObject m_playerInstance;
        private GameObject m_hitEffectSource;
        #endregion

        #region 테스트 생명주기 (Test Lifecycle)
        /// <summary>
        /// [기능]: 테스트에서 생성한 Unity 오브젝트를 즉시 제거합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: SPUM 캐릭터 View 테스트 정리 추가.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (m_playerInstance != null)
            {
                Object.DestroyImmediate(m_playerInstance);
            }

            if (m_hitEffectSource != null)
            {
                Object.DestroyImmediate(m_hitEffectSource);
            }
        }
        #endregion

        #region 테스트 (Tests)
        /// <summary>
        /// [기능]: 성공 클립이 OTHER 슬롯에 등록되고 성공·실패·대기 반응이 올바른 SPUM 상태를 사용하는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: SPUM 반응 View의 핵심 출력 계약 검증 추가.
        /// </summary>
        [Test]
        public void CharacterView_WithConfiguredSpum_MapsAllReactionStates()
        {
            System.Type viewType = typeof(GameArifiction.TimingCatch.TimingCatchGameViewModel)
                .Assembly
                .GetType(CharacterViewTypeName);
            Assert.IsNotNull(viewType, "SPUM 캐릭터 반응 View가 필요합니다.");

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            AnimationClip reactionClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DamagedClipPath);
            Assert.IsNotNull(playerPrefab, "로비 Player 프리팹을 불러와야 합니다.");
            Assert.IsNotNull(reactionClip, "피격 기준 애니메이션 클립을 불러와야 합니다.");

            m_playerInstance = Object.Instantiate(playerPrefab);
            m_playerInstance.SetActive(false);
            DisableUnrelatedBehaviours(m_playerInstance);

            Component spumPrefab = m_playerInstance.GetComponent("SPUM_Prefabs");
            Assert.IsNotNull(spumPrefab, "Player 프리팹에 SPUM_Prefabs가 필요합니다.");

            Component characterView = m_playerInstance.AddComponent(viewType);
            m_hitEffectSource = new GameObject("TimingCatchHitEffect");
            SetField(viewType, characterView, "m_spumPrefab", spumPrefab);
            SetField(viewType, characterView, "m_successClip", reactionClip);
            SetField(viewType, characterView, "m_hitEffectPrefab", m_hitEffectSource);
            SetField(viewType, characterView, "m_hitEffectAnchor", m_playerInstance.transform);

            m_playerInstance.SetActive(true);

            System.Type spumType = spumPrefab.GetType();
            IList otherClips = GetSpumField<IList>(spumType, spumPrefab, "OTHER_List");
            viewType.GetMethod("PlaySuccessReaction")?.Invoke(characterView, null);
            AnimatorOverrideController overrideController = GetSpumField<AnimatorOverrideController>(
                spumType,
                spumPrefab,
                "OverrideController"
            );
            Assert.Contains(reactionClip, otherClips);
            Assert.AreSame(reactionClip, overrideController["OTHER"]);

            viewType.GetMethod("PlayFailureReaction")?.Invoke(characterView, null);
            IList damagedClips = GetSpumField<IList>(spumType, spumPrefab, "DAMAGED_List");
            Assert.AreSame(damagedClips[0], overrideController["DAMAGED"]);
            Assert.IsNotNull(GetField(viewType, characterView, "m_activeHitEffect"));

            viewType.GetMethod("ResetToIdle")?.Invoke(characterView, null);
            IList idleClips = GetSpumField<IList>(spumType, spumPrefab, "IDLE_List");
            Assert.AreSame(idleClips[0], overrideController["IDLE"]);
        }
        #endregion

        #region 테스트 보조 메서드 (Test Helpers)
        /// <summary>
        /// [기능]: 테스트 대상 외 MonoBehaviour가 활성화 과정에서 실행되지 않도록 비활성화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: SPUM View 격리 테스트 보조 로직 추가.
        /// </summary>
        private static void DisableUnrelatedBehaviours(GameObject playerInstance)
        {
            MonoBehaviour[] behaviours = playerInstance.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i].GetType().Name == "SPUM_Prefabs")
                {
                    continue;
                }

                behaviours[i].enabled = false;
            }
        }

        /// <summary>
        /// [기능]: 리플렉션을 통해 테스트 대상의 직렬화 필드에 값을 설정합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 미구현 타입도 컴파일 가능한 RED 테스트 구성 추가.
        /// </summary>
        private static void SetField(System.Type viewType, Component characterView, string fieldName, Object value)
        {
            FieldInfo field = viewType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"필수 직렬화 필드가 없습니다: {fieldName}");
            field.SetValue(characterView, value);
        }

        /// <summary>
        /// [기능]: 리플렉션을 통해 테스트 대상의 내부 필드 값을 반환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 런타임 피격 효과 생성 여부 검증 추가.
        /// </summary>
        private static object GetField(System.Type viewType, Component characterView, string fieldName)
        {
            FieldInfo field = viewType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"필수 내부 필드가 없습니다: {fieldName}");
            return field.GetValue(characterView);
        }

        /// <summary>
        /// [기능]: 테스트 어셈블리가 직접 참조하지 않는 SPUM 공개 필드 값을 반환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: SPUM 어셈블리 경계를 유지하는 리플렉션 접근 추가.
        /// </summary>
        private static T GetSpumField<T>(System.Type spumType, Component spumPrefab, string fieldName)
        {
            FieldInfo field = spumType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(field, $"SPUM 필수 공개 필드가 없습니다: {fieldName}");
            return (T)field.GetValue(spumPrefab);
        }
        #endregion
    }
}
