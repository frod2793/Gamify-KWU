using System.Collections;
using System.Reflection;
using GameArifiction.Player;
using GameArifiction.TaskRush;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GamifyKWU.Tests.PlayMode
{
    public sealed class TaskRushSceneIntegrationTests
    {
        [UnityTest]
        public IEnumerator TaskRushScene_필수구성과UI를구성한다()
        {
            yield return LoadTaskRush();

            TaskRushPlayerView runnerView = Object.FindFirstObjectByType<TaskRushPlayerView>();
            PlayerView lobbyView = Object.FindFirstObjectByType<PlayerView>(FindObjectsInactive.Include);
            Assert.IsNotNull(Object.FindFirstObjectByType<TaskRushGameView>());
            Assert.IsNotNull(runnerView);
            Assert.IsNotNull(lobbyView);
            Assert.IsFalse(lobbyView.enabled);

            RectTransform timerFill = GameObject.Find("Canvas/TimerFrame/TimerMask/TimerFill").GetComponent<RectTransform>();
            RectTransform timerMask = timerFill.parent.GetComponent<RectTransform>();
            RectTransform timerFrame = timerMask.parent.GetComponent<RectTransform>();
            Assert.IsNotNull(timerMask.GetComponent<RectMask2D>(), "TimerFill 전용 RectMask2D가 없습니다.");
            Assert.IsNull(timerFrame.GetComponent<RectMask2D>(), "TimerFrame 자체가 마스킹되어 있습니다.");
            Assert.AreEqual(new Vector2(607.329f, 68.704f), timerMask.sizeDelta);
            Assert.AreEqual(new Vector2(69.88198f, -7.852f), timerMask.anchoredPosition);
            Assert.AreEqual(new Vector2(616.856f, 76f), timerFill.sizeDelta);
            Assert.AreEqual(0.5f, timerFill.anchorMin.y);
            Assert.AreEqual(0.5f, timerFill.anchorMax.y);
            Assert.Less(runnerView.transform.localScale.x, 0f);

            RectTransform lobbyButton = Object.FindFirstObjectByType<Canvas>().transform
                .Find("ResultPanel/LobbyButton").GetComponent<RectTransform>();
            Assert.AreEqual(0f, lobbyButton.anchorMin.y);
            Assert.AreEqual(70f, lobbyButton.anchoredPosition.y);
        }

        [UnityTest]
        public IEnumerator Jump_아이템곡선보다높이상승하고착지한다()
        {
            yield return LoadTaskRush();
            yield return new WaitForSeconds(1f);

            TaskRushPlayerView runnerView = Object.FindFirstObjectByType<TaskRushPlayerView>();
            Rigidbody2D body = runnerView.GetComponent<Rigidbody2D>();
            Collider2D playerCollider = runnerView.GetComponent<Collider2D>();
            float groundY = runnerView.transform.position.y;
            float highestY = groundY;
            float highestPlayerBottom = playerCollider.bounds.min.y;

            TaskRushItemView obstacle = null;
            for (float elapsed = 0f; elapsed < 3f && obstacle == null; elapsed += Time.deltaTime)
            {
                foreach (TaskRushItemView item in Object.FindObjectsByType<TaskRushItemView>(FindObjectsSortMode.None))
                {
                    if (item.Type == TaskRushItemType.Obstacle)
                    {
                        obstacle = item;
                        break;
                    }
                }
                yield return null;
            }
            Assert.IsNotNull(obstacle, "점프 높이를 비교할 장애물이 생성되지 않았습니다.");
            float obstacleTop = obstacle.GetComponent<Collider2D>().bounds.max.y;

            runnerView.func_Jump();
            Assert.AreEqual(Resources.FindObjectsOfTypeAll<TaskRushConfigSO>()[0].JumpForce, body.linearVelocity.y, 0.001f);
            for (float elapsed = 0f; elapsed < 1.5f; elapsed += Time.deltaTime)
            {
                highestY = Mathf.Max(highestY, runnerView.transform.position.y);
                highestPlayerBottom = Mathf.Max(highestPlayerBottom, playerCollider.bounds.min.y);
                yield return null;
            }

            Assert.GreaterOrEqual(highestY - groundY, 2.1f, "점프가 점수 아이템 곡선 높이에 닿지 않습니다.");
            Assert.Greater(highestPlayerBottom, obstacleTop, "점프했을 때 플레이어가 장애물 상단을 넘지 못합니다.");
            Assert.AreEqual(groundY, runnerView.transform.position.y, 0.1f, "점프 후 지면에 착지하지 않았습니다.");
            Assert.AreEqual(0f, body.linearVelocity.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator ObstacleHit_피격후달리기로복귀한다()
        {
            yield return LoadTaskRush();
            yield return new WaitForSeconds(1f);

            TaskRushGameView gameView = Object.FindFirstObjectByType<TaskRushGameView>();
            TaskRushPlayerView runnerView = Object.FindFirstObjectByType<TaskRushPlayerView>();
            var obstacle = new GameObject("TestObstacle");
            BoxCollider2D obstacleCollider = obstacle.AddComponent<BoxCollider2D>();
            obstacleCollider.isTrigger = true;
            obstacle.AddComponent<TaskRushItemView>().Initialize(gameView, TaskRushItemType.Obstacle);

            runnerView.SendMessage("OnTriggerEnter2D", obstacleCollider);
            yield return null;

            Animator animator = runnerView.GetComponentInChildren<Animator>();
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("DAMAGED"));
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("MOVE"));
        }

        [UnityTest]
        public IEnumerator Course_점수아이템곡선과장애물을연속생성한다()
        {
            yield return LoadTaskRush();
            yield return new WaitForSeconds(3.2f);

            TaskRushItemView[] items = Object.FindObjectsByType<TaskRushItemView>(FindObjectsSortMode.None);
            int scoreCount = 0;
            int obstacleCount = 0;
            float lowestScoreY = float.PositiveInfinity;
            float highestScoreY = float.NegativeInfinity;
            foreach (TaskRushItemView item in items)
            {
                if (item.Type == TaskRushItemType.Obstacle)
                {
                    obstacleCount++;
                    continue;
                }

                if (item.Type == TaskRushItemType.Score)
                {
                    scoreCount++;
                    lowestScoreY = Mathf.Min(lowestScoreY, item.transform.position.y);
                    highestScoreY = Mathf.Max(highestScoreY, item.transform.position.y);
                }
            }

            Assert.GreaterOrEqual(scoreCount, 8, "점수 아이템이 끊임없이 생성되지 않습니다.");
            Assert.GreaterOrEqual(obstacleCount, 1, "점수 아이템 경로에 장애물이 생성되지 않습니다.");
            Assert.Less(lowestScoreY, -2f);
            Assert.Greater(highestScoreY, -1f, "점수 아이템이 점프 곡선을 만들지 않습니다.");
        }

        [UnityTest]
        public IEnumerator RealPlay_Space입력으로첫장애물을뛰어넘는다()
        {
            yield return LoadTaskRush();
            yield return new WaitForSeconds(1f);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            TaskRushPlayerView runnerView = Object.FindFirstObjectByType<TaskRushPlayerView>();
            Collider2D playerCollider = runnerView.GetComponent<Collider2D>();
            Rigidbody2D body = runnerView.GetComponent<Rigidbody2D>();
            Animator animator = runnerView.GetComponentInChildren<Animator>();
            float groundY = runnerView.transform.position.y;

            TaskRushItemView obstacle = null;
            for (float elapsed = 0f; elapsed < 4f && obstacle == null; elapsed += Time.deltaTime)
            {
                foreach (TaskRushItemView item in Object.FindObjectsByType<TaskRushItemView>(FindObjectsSortMode.None))
                {
                    if (item.Type == TaskRushItemType.Obstacle)
                    {
                        obstacle = item;
                        break;
                    }
                }
                yield return null;
            }
            Assert.IsNotNull(obstacle, "실제 플레이할 장애물이 생성되지 않았습니다.");

            while (obstacle != null && obstacle.transform.position.x - runnerView.transform.position.x > 4f)
            {
                yield return null;
            }

            float jumpDistance = obstacle.transform.position.x - runnerView.transform.position.x;

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.RemoveDevice(keyboard);
            TestContext.WriteLine($"jumpDistance={jumpDistance:F2}, velocityAfterInput={body.linearVelocity.y:F2}");

            bool clearedObstacle = false;
            bool playedDamage = false;
            while (obstacle != null && obstacle.transform.position.x > runnerView.transform.position.x - 1.5f)
            {
                Collider2D obstacleCollider = obstacle.GetComponent<Collider2D>();
                if (Mathf.Abs(obstacle.transform.position.x - runnerView.transform.position.x)
                    <= playerCollider.bounds.extents.x + obstacleCollider.bounds.extents.x)
                {
                    clearedObstacle |= playerCollider.bounds.min.y > obstacleCollider.bounds.max.y;
                }
                playedDamage |= animator.GetCurrentAnimatorStateInfo(0).IsName("DAMAGED");
                yield return null;
            }

            for (float elapsed = 0f; elapsed < 2f
                && (Mathf.Abs(runnerView.transform.position.y - groundY) > 0.1f || Mathf.Abs(body.linearVelocity.y) > 0.1f);
                elapsed += Time.deltaTime)
            {
                yield return null;
            }
            for (float elapsed = 0f; elapsed < 0.6f && !animator.GetCurrentAnimatorStateInfo(0).IsName("MOVE"); elapsed += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(clearedObstacle, "Space 입력 점프로 장애물 상단을 완전히 넘지 못했습니다.");
            Assert.IsNotNull(obstacle, "장애물과 충돌해 오브젝트가 소비됐습니다.");
            Assert.IsFalse(playedDamage, "장애물을 넘는 동안 피격 애니메이션이 재생됐습니다.");
            Assert.AreEqual(groundY, runnerView.transform.position.y, 0.1f);
            Assert.AreEqual(0f, body.linearVelocity.y, 0.1f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("MOVE"));
        }

        [UnityTest]
        public IEnumerator GravityScale_설정값을플레이어물리에적용한다()
        {
            yield return LoadTaskRush();

            TaskRushPlayerView runnerView = Object.FindFirstObjectByType<TaskRushPlayerView>();
            TaskRushGameView gameView = Object.FindFirstObjectByType<TaskRushGameView>();
            TaskRushConfigSO source = Resources.FindObjectsOfTypeAll<TaskRushConfigSO>()[0];
            TaskRushConfigSO config = Object.Instantiate(source);
            FieldInfo gravityField = typeof(TaskRushConfigSO).GetField("m_gravityScale", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(gravityField, "TaskRushConfigSO에 중력 설정이 없습니다.");
            gravityField.SetValue(config, 2.5f);
            runnerView.Construct(config, gameView);

            Assert.AreEqual(2.5f, config.GravityScale, 0.001f);
            Assert.AreEqual(2.5f, runnerView.GetComponent<Rigidbody2D>().gravityScale, 0.001f);
            Object.Destroy(config);
        }

        private static IEnumerator LoadTaskRush()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("TaskRush", LoadSceneMode.Single);
            Assert.IsNotNull(load);
            while (!load.isDone)
            {
                yield return null;
            }
        }
    }
}
