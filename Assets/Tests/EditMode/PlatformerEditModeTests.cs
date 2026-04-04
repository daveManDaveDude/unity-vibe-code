using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VibeCode.Platformer;

namespace VibeCode.Tests.EditMode
{
    public class PlatformerEditModeTests
    {
        [Test]
        public void AddingPlayerControllerAddsRequiredPhysicsComponents()
        {
            var player = new GameObject("Player Under Test");

            try
            {
                PlayerController2D controller = player.AddComponent<PlayerController2D>();

                Assert.That(controller, Is.Not.Null);
                Assert.That(player.GetComponent<Rigidbody2D>(), Is.Not.Null);
                Assert.That(player.GetComponent<CapsuleCollider2D>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerControllerDefaultsToDoubleJumpSupport()
        {
            var player = new GameObject("Player Under Test");

            try
            {
                PlayerController2D controller = player.AddComponent<PlayerController2D>();
                SerializedObject serializedObject = new SerializedObject(controller);

                Assert.That(serializedObject.FindProperty("maxJumpCount").intValue, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerHealthDefaultsToThreeHits()
        {
            var player = new GameObject("Player Under Test");

            try
            {
                player.AddComponent<Rigidbody2D>();
                player.AddComponent<CapsuleCollider2D>();
                player.AddComponent<PlayerController2D>();

                PlayerHealth2D health = player.AddComponent<PlayerHealth2D>();
                SerializedObject serializedObject = new SerializedObject(health);

                Assert.That(serializedObject.FindProperty("maxHealth").intValue, Is.EqualTo(3));
                Assert.That(health.CurrentHealth, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GroundCheckIgnoresPlayerOwnCollider()
        {
            var player = new GameObject("Player Under Test");

            try
            {
                player.layer = 0;

                Rigidbody2D body = player.AddComponent<Rigidbody2D>();
                CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Vertical;
                collider.size = new Vector2(0.225f, 0.45f);

                var groundCheck = new GameObject("GroundCheck").transform;
                groundCheck.SetParent(player.transform, false);
                groundCheck.localPosition = new Vector3(0f, -0.24f, 0f);

                PlayerController2D controller = player.AddComponent<PlayerController2D>();
                ConfigureControllerForGroundCheck(controller, body, collider, groundCheck);

                InvokeUpdateGroundedState(controller);

                Assert.That(controller.IsGrounded, Is.False,
                    "Expected the player ground check to ignore the player's own collider while airborne.");
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GroundCheckStillDetectsActualGround()
        {
            var player = new GameObject("Player Under Test");
            var ground = new GameObject("Ground Under Test");

            try
            {
                player.layer = 0;
                ground.layer = 0;

                Rigidbody2D body = player.AddComponent<Rigidbody2D>();
                CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Vertical;
                collider.size = new Vector2(0.225f, 0.45f);

                BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
                groundCollider.size = new Vector2(2f, 1f);
                ground.transform.position = new Vector3(0f, -0.5f, 0f);

                var groundCheck = new GameObject("GroundCheck").transform;
                groundCheck.SetParent(player.transform, false);
                groundCheck.localPosition = new Vector3(0f, -0.24f, 0f);

                PlayerController2D controller = player.AddComponent<PlayerController2D>();
                ConfigureControllerForGroundCheck(controller, body, collider, groundCheck);

                InvokeUpdateGroundedState(controller);

                Assert.That(controller.IsGrounded, Is.True,
                    "Expected the player ground check to keep detecting real ground underfoot.");
            }
            finally
            {
                Object.DestroyImmediate(ground);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void LedgeAssistStartsWhenPlayerBarelyCatchesPlatformEdge()
        {
            var player = new GameObject("Player Under Test");
            var platform = new GameObject("Platform Under Test");

            try
            {
                player.layer = 0;
                platform.layer = 0;

                Rigidbody2D body = player.AddComponent<Rigidbody2D>();
                body.gravityScale = 4f;

                CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Vertical;
                collider.size = new Vector2(0.225f, 0.45f);

                BoxCollider2D platformCollider = platform.AddComponent<BoxCollider2D>();
                platformCollider.size = new Vector2(1.2f, 0.2f);
                platform.transform.position = new Vector3(0.55f, 0f, 0f);

                var groundCheck = new GameObject("GroundCheck").transform;
                groundCheck.SetParent(player.transform, false);
                groundCheck.localPosition = new Vector3(0f, -0.24f, 0f);

                player.transform.position = new Vector3(-0.18f, -0.02f, 0f);
                Physics2D.SyncTransforms();

                PlayerController2D controller = player.AddComponent<PlayerController2D>();
                ConfigureControllerForGroundCheck(controller, body, collider, groundCheck);
                SetPrivateField(controller, "moveInput", new Vector2(1f, 0f));
                body.linearVelocity = new Vector2(1.5f, -0.5f);

                bool started = InvokeTryStartLedgeAssist(controller);

                Assert.That(started, Is.True, "Expected the ledge assist to trigger when the player barely catches the platform corner.");
            }
            finally
            {
                Object.DestroyImmediate(platform);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void LedgeAssistDoesNotStartAgainstTallWall()
        {
            var player = new GameObject("Player Under Test");
            var wall = new GameObject("Wall Under Test");

            try
            {
                player.layer = 0;
                wall.layer = 0;

                Rigidbody2D body = player.AddComponent<Rigidbody2D>();
                body.gravityScale = 4f;

                CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Vertical;
                collider.size = new Vector2(0.225f, 0.45f);

                BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
                wallCollider.size = new Vector2(1f, 2f);
                wall.transform.position = new Vector3(0.55f, 0.5f, 0f);

                var groundCheck = new GameObject("GroundCheck").transform;
                groundCheck.SetParent(player.transform, false);
                groundCheck.localPosition = new Vector3(0f, -0.24f, 0f);

                player.transform.position = new Vector3(-0.18f, -0.02f, 0f);
                Physics2D.SyncTransforms();

                PlayerController2D controller = player.AddComponent<PlayerController2D>();
                ConfigureControllerForGroundCheck(controller, body, collider, groundCheck);
                SetPrivateField(controller, "moveInput", new Vector2(1f, 0f));
                body.linearVelocity = new Vector2(1.5f, -0.5f);

                bool started = InvokeTryStartLedgeAssist(controller);

                Assert.That(started, Is.False, "Expected the ledge assist to stay off when the player is pushing into a full wall.");
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void MainSceneAssetExists()
        {
            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main.unity");

            Assert.That(mainScene, Is.Not.Null, "Expected Assets/Scenes/Main.unity to exist.");
        }

        [Test]
        public void MainSceneIsEnabledInBuildSettings()
        {
            bool mainSceneIsEnabled = EditorBuildSettings.scenes
                .Any(scene => scene.enabled && scene.path == "Assets/Scenes/Main.unity");

            Assert.That(mainSceneIsEnabled, Is.True, "Expected Main.unity to be enabled in Build Settings.");
        }

        [Test]
        public void PatrollingEnemyPrefabExists()
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/PatrollingEnemy.prefab");

            Assert.That(enemyPrefab, Is.Not.Null, "Expected a reusable patrolling enemy prefab to exist.");
            Assert.That(enemyPrefab.GetComponent<Enemy2D>(), Is.Not.Null, "Expected the prefab to include the reusable enemy interaction component.");
            Assert.That(enemyPrefab.GetComponent<PatrollingEnemy2D>(), Is.Not.Null, "Expected the prefab to include the patrol movement component.");
        }

        [Test]
        public void HoveringEnemyPrefabExists()
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/HoveringEnemy.prefab");

            Assert.That(enemyPrefab, Is.Not.Null, "Expected a reusable hovering enemy prefab to exist.");
            Assert.That(enemyPrefab.GetComponent<Enemy2D>(), Is.Not.Null, "Expected the prefab to include the reusable enemy interaction component.");
            Assert.That(enemyPrefab.GetComponent<HoveringEnemy2D>(), Is.Not.Null, "Expected the prefab to include the hovering movement component.");
        }

        [Test]
        public void DesktopWindowSizingUsesSeventyPercentOfScreenResolution()
        {
            Vector2Int target = DesktopWindowSizing.CalculateWindowSize(1920, 1080);

            Assert.That(target, Is.EqualTo(new Vector2Int(1344, 756)));
        }

        [Test]
        public void RespawnUsesCheckpointWhenActivated()
        {
            var managerObject = new GameObject("Game Manager");
            var playerObject = new GameObject("Player Under Test");
            var defaultRespawnObject = new GameObject("Default Respawn");
            var checkpointObject = new GameObject("Checkpoint");

            try
            {
                GravityGardenGameManager gameManager = managerObject.AddComponent<GravityGardenGameManager>();
                PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
                Checkpoint2D checkpoint = checkpointObject.AddComponent<Checkpoint2D>();

                defaultRespawnObject.transform.position = new Vector3(-2f, 1.5f, 0f);
                checkpointObject.transform.position = new Vector3(4.5f, -0.25f, 0f);

                gameManager.Configure(player, defaultRespawnObject.transform, 3);

                gameManager.RespawnPlayer(player);
                Assert.That(player.transform.position, Is.EqualTo(defaultRespawnObject.transform.position));

                Assert.That(gameManager.TryActivateCheckpoint(checkpoint, player), Is.True);

                player.transform.position = Vector3.zero;
                gameManager.RespawnPlayer(player);

                Assert.That(player.transform.position, Is.EqualTo(checkpointObject.transform.position));
            }
            finally
            {
                Object.DestroyImmediate(checkpointObject);
                Object.DestroyImmediate(defaultRespawnObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void DamagePlayerIgnoresRepeatHitsWhileInvulnerable()
        {
            var managerObject = new GameObject("Game Manager");
            var playerObject = new GameObject("Player Under Test");
            var respawnObject = new GameObject("Respawn");

            try
            {
                GravityGardenGameManager gameManager = managerObject.AddComponent<GravityGardenGameManager>();
                playerObject.AddComponent<Rigidbody2D>();
                playerObject.AddComponent<CapsuleCollider2D>();
                PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
                PlayerHealth2D health = playerObject.AddComponent<PlayerHealth2D>();

                gameManager.Configure(player, respawnObject.transform, 3);

                Assert.That(gameManager.DamagePlayer(player, damageSource: Vector2.left), Is.True);
                Assert.That(health.CurrentHealth, Is.EqualTo(2));
                Assert.That(gameManager.DamagePlayer(player, damageSource: Vector2.left), Is.False);
                Assert.That(health.CurrentHealth, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(respawnObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(managerObject);
            }
        }

        private static void InvokeUpdateGroundedState(PlayerController2D controller)
        {
            MethodInfo updateGroundedState = typeof(PlayerController2D).GetMethod(
                "UpdateGroundedState",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(updateGroundedState, Is.Not.Null, "Expected to find the private UpdateGroundedState method.");
            updateGroundedState.Invoke(controller, null);
        }

        private static bool InvokeTryStartLedgeAssist(PlayerController2D controller)
        {
            MethodInfo tryStartLedgeAssist = typeof(PlayerController2D).GetMethod(
                "TryStartLedgeAssist",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(tryStartLedgeAssist, Is.Not.Null, "Expected to find the private TryStartLedgeAssist method.");
            return (bool)tryStartLedgeAssist.Invoke(controller, null);
        }

        private static void ConfigureControllerForGroundCheck(
            PlayerController2D controller,
            Rigidbody2D body,
            CapsuleCollider2D collider,
            Transform groundCheck)
        {
            var serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("body").objectReferenceValue = body;
            serializedObject.FindProperty("bodyCollider").objectReferenceValue = collider;
            serializedObject.FindProperty("groundCheck").objectReferenceValue = groundCheck;
            serializedObject.FindProperty("groundLayers").intValue = 1 << 0;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPrivateField<T>(PlayerController2D controller, string fieldName, T value)
        {
            FieldInfo field = typeof(PlayerController2D).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Expected to find private field '{fieldName}'.");
            field.SetValue(controller, value);
        }
    }
}
