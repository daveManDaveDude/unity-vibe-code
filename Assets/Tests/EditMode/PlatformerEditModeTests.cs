using System.Linq;
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
    }
}
