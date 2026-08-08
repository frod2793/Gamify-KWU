using GameArifiction.Player;
using GameArifiction.TimingCatch;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameArifiction.Tests.Editor
{
    /// <summary>
    /// [기능]: TimingCatch 씬의 SPUM 연출 캐릭터 배치와 직렬화 참조를 검증합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [TestFixture]
    public sealed class TimingCatchSceneCharacterTests
    {
        #region 상수 (Constants)
        private const string ScenePath = "Assets/_Game/Scenes/TimingCatch.unity";
        #endregion

        #region 테스트 (Tests)
        /// <summary>
        /// [기능]: 씬에 로비 Player 기반 연출 캐릭터와 성공·피격 참조가 올바르게 구성됐는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: TimingCatch 씬 캐릭터 통합 구성 검증 추가.
        /// </summary>
        [Test]
        public void TimingCatchScene_HasDistinctHudTextAndSlideBindings()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject canvas = FindRoot(scene, "Canvas");
                Assert.IsNotNull(canvas, "TimingCatch Canvas가 필요합니다.");
                TimingCatchGameView view = canvas.GetComponentInChildren<TimingCatchGameView>(true);
                Assert.IsNotNull(view, "TimingCatchGameView가 필요합니다.");

                SerializedObject serializedView = new SerializedObject(view);
                string[] textFields = { "m_roundText", "m_turnText", "m_difficultyText", "m_scoreText", "m_judgeText", "m_dialogueText", "m_bonusText", "m_stateText" };
                var references = new Object[textFields.Length];
                for (int i = 0; i < textFields.Length; i++)
                {
                    references[i] = serializedView.FindProperty(textFields[i]).objectReferenceValue;
                    Assert.IsNotNull(references[i], $"{textFields[i]} 참조가 필요합니다.");
                }
                Assert.AreEqual(references.Length, new System.Collections.Generic.HashSet<Object>(references).Count, "HUD 텍스트는 별도 TMP 오브젝트여야 합니다.");
                Assert.IsFalse(((TMPro.TMP_Text)serializedView.FindProperty("m_stateText").objectReferenceValue).gameObject.activeSelf, "중복 StateText는 숨겨야 합니다.");

                Assert.IsNotNull(serializedView.FindProperty("m_slideImage").objectReferenceValue, "슬라이드 Image 참조가 필요합니다.");
                Assert.AreEqual(6, serializedView.FindProperty("m_slideSprites").arraySize, "슬라이드 Sprite 6장이 필요합니다.");
                Canvas canvasComponent = canvas.GetComponent<Canvas>();
                Assert.AreEqual(RenderMode.ScreenSpaceCamera, canvasComponent.renderMode);
                Assert.IsNotNull(canvasComponent.worldCamera, "캐릭터와 함께 그릴 Canvas 카메라가 필요합니다.");
                Image slideImage = (Image)serializedView.FindProperty("m_slideImage").objectReferenceValue;
                Assert.IsTrue(canvasComponent.overrideSorting);
                Assert.AreEqual(1000, canvasComponent.sortingOrder);
                Assert.AreEqual(100f, canvasComponent.planeDistance, .001f);
                Canvas backgroundCanvas = ((Image)serializedView.FindProperty("m_backgroundImage").objectReferenceValue).GetComponent<Canvas>();
                Assert.IsNotNull(backgroundCanvas, "배경은 독립 Canvas여야 캐릭터 뒤에 그려집니다.");
                Assert.IsTrue(backgroundCanvas.overrideSorting);
                Assert.AreEqual(-1000, backgroundCanvas.sortingOrder);
                RectTransform slide = slideImage.rectTransform;
                Assert.AreEqual(RenderMode.ScreenSpaceCamera, backgroundCanvas.renderMode);
                Assert.AreSame(canvasComponent.worldCamera, backgroundCanvas.worldCamera);
                Assert.AreEqual(90f, backgroundCanvas.planeDistance, .001f);
                Assert.AreEqual(16f / 9f, slide.rect.width / slide.rect.height, .001f);
                Assert.IsTrue(slideImage.preserveAspect);
                Slider gauge = (Slider)serializedView.FindProperty("m_gaugeSlider").objectReferenceValue;
                RectTransform gaugeRect = gauge.GetComponent<RectTransform>();
                Assert.LessOrEqual(gaugeRect.anchorMin.x, .05f);
                Assert.GreaterOrEqual(gaugeRect.anchorMax.x, .95f);
                RectTransform judge = ((TMPro.TMP_Text)serializedView.FindProperty("m_judgeText").objectReferenceValue).rectTransform;
                RectTransform button = ((Button)serializedView.FindProperty("m_timingButton").objectReferenceValue).GetComponent<RectTransform>();
                Assert.Greater(judge.anchorMin.y, gaugeRect.anchorMin.y);
                Assert.Less(button.anchorMin.y, gaugeRect.anchorMin.y);
                RectTransform greatZone = serializedView.FindProperty("m_greatZone").objectReferenceValue as RectTransform;
                Assert.IsNotNull(greatZone);
                Assert.AreEqual("GreatZone", greatZone.name);
                RectTransform cursor = serializedView.FindProperty("m_gaugePointer").objectReferenceValue as RectTransform;
                Assert.AreEqual(cursor.rect.width, cursor.rect.height, .001f);
                Assert.IsTrue(cursor.GetComponent<Image>().preserveAspect);
                RectTransform dialogue = ((TMPro.TMP_Text)serializedView.FindProperty("m_dialogueText").objectReferenceValue).rectTransform;
                RectTransform star = ((Image)serializedView.FindProperty("m_starImage").objectReferenceValue).rectTransform;
                Assert.Greater(dialogue.anchorMin.x, .25f);
                Assert.Greater(star.anchorMin.y, .3f);
                RectTransform bonus = ((TMPro.TMP_Text)serializedView.FindProperty("m_bonusText").objectReferenceValue).rectTransform;
                Assert.Less(bonus.anchorMin.x, .5f);
                Assert.Greater(bonus.anchorMin.y, .35f);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TimingCatchScene_HasConfiguredReactionCharacter()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject characterRoot = FindRoot(scene, "TimingCatchCharacterRoot");
                Assert.IsNotNull(characterRoot, "타이밍 연출 캐릭터 Root가 필요합니다.");
                Assert.AreEqual(new Vector3(-5.5f, -2.2f, 0f), characterRoot.transform.position);

                TimingCatchCharacterView characterView = characterRoot.GetComponent<TimingCatchCharacterView>();
                Assert.IsNotNull(characterView, "타이밍 캐릭터 반응 View가 필요합니다.");

                SerializedObject serializedView = new SerializedObject(characterView);
                Assert.IsNotNull(serializedView.FindProperty("m_spumPrefab").objectReferenceValue);
                Assert.IsNotNull(serializedView.FindProperty("m_successClip").objectReferenceValue);
                Assert.IsNotNull(serializedView.FindProperty("m_hitEffectPrefab").objectReferenceValue);
                Assert.IsNotNull(serializedView.FindProperty("m_hitEffectAnchor").objectReferenceValue);

                Transform playerTransform = characterRoot.transform.Find("Player");
                Assert.IsNotNull(playerTransform, "로비 Player 프리팹 인스턴스가 필요합니다.");
                PlayerView playerView = playerTransform.GetComponent<PlayerView>();
                Assert.IsNotNull(playerView);
                Assert.IsFalse(playerView.enabled, "연출 캐릭터의 이동 View는 비활성화해야 합니다.");

                Rigidbody2D rigidbody = playerTransform.GetComponent<Rigidbody2D>();
                if (rigidbody != null)
                {
                    Assert.IsFalse(rigidbody.simulated, "연출 캐릭터 물리는 비활성화해야 합니다.");
                }

                Collider2D[] colliders = playerTransform.GetComponentsInChildren<Collider2D>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Assert.IsFalse(colliders[i].enabled, "연출 캐릭터 충돌체는 비활성화해야 합니다.");
                }

                GameObject playerSource = PrefabUtility.GetCorrespondingObjectFromSource(playerTransform.gameObject);
                Assert.IsNotNull(playerSource, "Player는 프리팹 연결을 유지해야 합니다.");
                Assert.AreEqual("Assets/_Game/Prefabs/Player/Player.prefab", AssetDatabase.GetAssetPath(playerSource));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        #endregion

        #region 테스트 보조 메서드 (Test Helpers)
        /// <summary>
        /// [기능]: 지정 씬의 최상위 오브젝트 중 이름이 일치하는 항목을 반환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Additive 씬 Root 탐색 추가.
        /// </summary>
        private static GameObject FindRoot(Scene scene, string rootName)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i].name == rootName)
                {
                    return rootObjects[i];
                }
            }

            return null;
        }
        #endregion
    }
}
