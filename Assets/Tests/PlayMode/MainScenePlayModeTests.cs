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
            Assert.That(Object.FindAnyObjectByType<FloorButton2D>(), Is.Not.Null,
                "Expected Main scene to contain a floor button puzzle switch.");
            Assert.That(Object.FindAnyObjectByType<LinkedGate2D>(), Is.Not.Null,
                "Expected Main scene to contain a linked gate puzzle object.");
            Assert.That(Object.FindAnyObjectByType<KillZone2D>(), Is.Not.Null,
                "Expected Main scene to contain a kill zone.");
            Assert.That(Object.FindAnyObjectByType<PatrollingEnemy2D>(), Is.Not.Null,
                "Expected Main scene to contain at least one patrolling enemy.");
            Assert.That(Object.FindObjectsByType<EnergySeedCollectible>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(3),
                "Expected Main scene to contain several collectible energy seeds.");
        }

        [UnityTest]
        public IEnumerator FloorButtonOpensLinkedGate()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            FloorButton2D floorButton = Object.FindAnyObjectByType<FloorButton2D>();
            LinkedGate2D linkedGate = Object.FindAnyObjectByType<LinkedGate2D>();
            Collider2D gateCollider = linkedGate != null ? linkedGate.GetComponent<Collider2D>() : null;
            Rigidbody2D playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;

            Assert.That(player, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(floorButton, Is.Not.Null);
            Assert.That(linkedGate, Is.Not.Null);
            Assert.That(floorButton.LinkedGate, Is.SameAs(linkedGate));
            Assert.That(gateCollider, Is.Not.Null);
            Assert.That(floorButton.IsActivated, Is.False, "Expected the switch to begin unpressed.");
            Assert.That(linkedGate.IsOpen, Is.False, "Expected the gate to begin locked.");
            Assert.That(gateCollider.enabled, Is.True, "Expected the locked gate to block the player.");

            playerBody.linearVelocity = Vector2.zero;
            player.transform.position = floorButton.transform.position + new Vector3(0f, 0.4f, 0f);

            for (int index = 0; index < 5 && !floorButton.IsActivated; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(floorButton.IsActivated, Is.True, "Expected touching the button to latch it on.");
            Assert.That(linkedGate.IsOpen, Is.True, "Expected the linked gate to open once the button is pressed.");
            Assert.That(gateCollider.enabled, Is.False, "Expected the opened gate to stop blocking the player.");
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

        [UnityTest]
        public IEnumerator LandingOnEnemyDefeatsIt()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            PatrollingEnemy2D enemyPatrol = Object.FindAnyObjectByType<PatrollingEnemy2D>();

            Assert.That(player, Is.Not.Null);
            Assert.That(enemyPatrol, Is.Not.Null);

            enemyPatrol.enabled = false;

            Enemy2D enemy = enemyPatrol.GetComponent<Enemy2D>();
            Rigidbody2D enemyBody = enemy != null ? enemy.Body : null;
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();

            Assert.That(enemy, Is.Not.Null);
            Assert.That(enemyBody, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);

            enemyBody.linearVelocity = Vector2.zero;
            player.transform.position = enemy.transform.position + new Vector3(0f, 0.75f, 0f);
            playerBody.linearVelocity = new Vector2(0f, -6f);

            for (int index = 0; index < 30 && enemy != null; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            yield return null;

            Assert.That(enemy == null, Is.True, "Expected landing on the enemy from above to defeat it.");
            Assert.That(player, Is.Not.Null, "Expected the player to remain active after stomping the enemy.");
        }

        [UnityTest]
        public IEnumerator PlayerCanStillCollectSeedsAfterDefeatingEnemy()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            GravityGardenGameManager gameManager = Object.FindAnyObjectByType<GravityGardenGameManager>();
            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            PatrollingEnemy2D enemyPatrol = Object.FindAnyObjectByType<PatrollingEnemy2D>();
            EnergySeedCollectible[] seeds = Object.FindObjectsByType<EnergySeedCollectible>(FindObjectsSortMode.None);

            Assert.That(gameManager, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(enemyPatrol, Is.Not.Null);
            Assert.That(seeds.Length, Is.GreaterThan(0));

            enemyPatrol.enabled = false;

            Enemy2D enemy = enemyPatrol.GetComponent<Enemy2D>();
            Rigidbody2D enemyBody = enemy != null ? enemy.Body : null;
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();

            Assert.That(enemy, Is.Not.Null);
            Assert.That(enemyBody, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);

            enemyBody.linearVelocity = Vector2.zero;
            player.transform.position = enemy.transform.position + new Vector3(0f, 0.75f, 0f);
            playerBody.linearVelocity = new Vector2(0f, -6f);

            for (int index = 0; index < 30 && enemy != null; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(enemy == null, Is.True, "Expected landing on the enemy from above to defeat it.");

            EnergySeedCollectible seed = seeds[0];
            int collectedBefore = gameManager.CollectedSeeds;

            playerBody.linearVelocity = Vector2.zero;
            player.transform.position = seed.transform.position;

            for (int index = 0; index < 3 && gameManager.CollectedSeeds == collectedBefore; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(gameManager.CollectedSeeds, Is.GreaterThan(collectedBefore),
                "Expected the player to keep collecting seeds after defeating an enemy.");
            Assert.That(seed.gameObject.activeSelf, Is.False, "Expected the collected seed to deactivate.");
        }
    }
}
