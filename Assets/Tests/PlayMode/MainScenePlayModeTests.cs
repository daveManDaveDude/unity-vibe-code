using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VibeCode.Platformer;

namespace VibeCode.Tests.PlayMode
{
    public class MainScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator MainSceneLoadsWithCameraAndPlayer()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            Assert.That(Camera.main, Is.Not.Null, "Expected Main scene to provide a Main Camera.");
            Assert.That(Object.FindAnyObjectByType<PlayerController2D>(), Is.Not.Null,
                "Expected Main scene to contain a PlayerController2D instance.");
            Assert.That(Object.FindAnyObjectByType<GravityGardenGameManager>(), Is.Not.Null,
                "Expected Main scene to contain a GravityGardenGameManager instance.");
            Assert.That(Object.FindAnyObjectByType<Checkpoint2D>(), Is.Not.Null,
                "Expected Main scene to contain a reusable checkpoint.");
            Assert.That(Object.FindAnyObjectByType<ExitPortal>(), Is.Not.Null,
                "Expected Main scene to contain an exit portal.");
            Assert.That(Object.FindAnyObjectByType<KillZone2D>(), Is.Not.Null,
                "Expected Main scene to contain a kill zone.");
            Assert.That(Object.FindAnyObjectByType<PatrollingEnemy2D>(), Is.Not.Null,
                "Expected Main scene to contain at least one patrolling enemy.");
            Assert.That(Object.FindObjectsByType<EnergySeedCollectible>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(3),
                "Expected Main scene to contain several collectible energy seeds.");
        }

        [UnityTest]
        public IEnumerator GravityGardenSliceSupportsWinAndRespawnFlow()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            GravityGardenGameManager gameManager = Object.FindAnyObjectByType<GravityGardenGameManager>();
            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            Checkpoint2D checkpoint = Object.FindAnyObjectByType<Checkpoint2D>();
            GameObject respawnObject = GameObject.Find("Respawn Point");
            EnergySeedCollectible[] seeds = Object.FindObjectsByType<EnergySeedCollectible>(FindObjectsSortMode.None);

            Assert.That(gameManager, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(checkpoint, Is.Not.Null);
            Assert.That(respawnObject, Is.Not.Null);
            Assert.That(seeds.Length, Is.GreaterThanOrEqualTo(gameManager.MinimumSeedsToExit));

            player.transform.position = new Vector3(4f, -6f, 0f);
            gameManager.RespawnPlayer(player);

            Assert.That(player.transform.position.x, Is.EqualTo(respawnObject.transform.position.x).Within(0.01f));
            Assert.That(player.transform.position.y, Is.EqualTo(respawnObject.transform.position.y).Within(0.01f));

            Assert.That(gameManager.TryActivateCheckpoint(checkpoint, player), Is.True);

            player.transform.position = new Vector3(4f, -6f, 0f);
            gameManager.RespawnPlayer(player);

            Assert.That(player.transform.position.x, Is.EqualTo(checkpoint.transform.position.x).Within(0.01f));
            Assert.That(player.transform.position.y, Is.EqualTo(checkpoint.transform.position.y).Within(0.01f));

            for (int index = 0; index < gameManager.MinimumSeedsToExit; index++)
            {
                Assert.That(gameManager.TryCollectSeed(seeds[index]), Is.True);
            }

            Assert.That(gameManager.CanUseExit, Is.True, "Expected enough seeds to unlock the exit.");
            Assert.That(gameManager.TryReachExit(), Is.True, "Expected the exit to succeed after collecting the minimum seeds.");
            Assert.That(gameManager.HasWon, Is.True, "Expected the manager to mark the slice as won.");
        }
    }
}
