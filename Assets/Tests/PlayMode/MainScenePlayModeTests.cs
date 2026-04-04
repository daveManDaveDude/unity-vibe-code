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
        public IEnumerator MainSceneLoadsWithGravityGardenSliceObjects()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            GravityGardenGameManager gameManager = Object.FindAnyObjectByType<GravityGardenGameManager>();

            Assert.That(Camera.main, Is.Not.Null, "Expected Main scene to provide a Main Camera.");
            Assert.That(Object.FindAnyObjectByType<PlayerController2D>(), Is.Not.Null,
                "Expected Main scene to contain a PlayerController2D instance.");
            Assert.That(gameManager, Is.Not.Null,
                "Expected Main scene to contain a GravityGardenGameManager instance.");
            Assert.That(Object.FindAnyObjectByType<GravityGardenHud>(), Is.Not.Null,
                "Expected Main scene to contain the basic slice HUD.");
            Assert.That(Object.FindAnyObjectByType<Checkpoint2D>(), Is.Not.Null,
                "Expected Main scene to contain a midpoint checkpoint.");
            Assert.That(GameObject.Find("Garden Backdrop"), Is.Not.Null,
                "Expected Main scene to use a continuous world backdrop behind the slice.");
            Assert.That(Object.FindAnyObjectByType<ExitPortal>(), Is.Not.Null,
                "Expected Main scene to contain an exit portal.");
            Assert.That(Object.FindAnyObjectByType<KillZone2D>(), Is.Not.Null,
                "Expected Main scene to contain a kill zone.");
            Assert.That(Object.FindAnyObjectByType<MovingPlatform2D>(), Is.Not.Null,
                "Expected Main scene to contain a moving platform traversal helper.");
            Assert.That(Object.FindAnyObjectByType<CyclingSpikeHazard2D>(), Is.Not.Null,
                "Expected Main scene to contain a timed hazard.");
            Assert.That(Object.FindObjectsByType<EnergySeedCollectible>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(gameManager.MinimumSeedsToExit),
                "Expected Main scene to contain enough collectible energy seeds to finish the slice.");
        }

        [UnityTest]
        public IEnumerator MovingPlatformCarriesPlayerAlongItsPath()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            MovingPlatform2D movingPlatform = Object.FindAnyObjectByType<MovingPlatform2D>();
            Rigidbody2D playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
            Collider2D platformCollider = movingPlatform != null ? movingPlatform.GetComponent<Collider2D>() : null;
            GameObject leftDock = GameObject.Find("Bridge Dock Left");
            GameObject rightDock = GameObject.Find("Bridge Dock Right");
            GameObject midGround = GameObject.Find("Mid Ground");
            SpriteRenderer leftDockRenderer = leftDock != null ? leftDock.GetComponent<SpriteRenderer>() : null;
            SpriteRenderer rightDockRenderer = rightDock != null ? rightDock.GetComponent<SpriteRenderer>() : null;
            SpriteRenderer midGroundRenderer = midGround != null ? midGround.GetComponent<SpriteRenderer>() : null;

            Assert.That(player, Is.Not.Null);
            Assert.That(movingPlatform, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(platformCollider, Is.Not.Null);
            Assert.That(leftDockRenderer, Is.Not.Null);
            Assert.That(rightDockRenderer, Is.Not.Null);
            Assert.That(midGroundRenderer, Is.Not.Null);

            Bounds platformBounds = platformCollider.bounds;
            Assert.That(platformBounds.min.x, Is.EqualTo(leftDockRenderer.bounds.max.x).Within(0.06f),
                "Expected the moving platform's left stop to line up with the left dock.");

            player.transform.SetParent(null, true);
            playerBody.linearVelocity = Vector2.zero;
            player.transform.position = new Vector3(platformBounds.center.x, platformBounds.max.y + 0.24f, 0f);
            Physics2D.SyncTransforms();

            for (int index = 0; index < 15 && !movingPlatform.HasRider(player); index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(movingPlatform.HasRider(player), Is.True, "Expected the player to register as riding the moving platform.");

            float initialPlatformX = movingPlatform.transform.position.x;
            float initialRelativeOffsetX = player.transform.position.x - movingPlatform.transform.position.x;

            for (int index = 0; index < 60; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(movingPlatform.transform.position.x, Is.GreaterThan(initialPlatformX + 0.5f),
                "Expected the moving platform to progress along its travel path.");
            Assert.That(player.transform.position.x, Is.GreaterThan(initialPlatformX + 0.5f),
                "Expected the player to travel with the platform rather than getting left behind.");

            Bounds finalPlatformBounds = platformCollider.bounds;
            Assert.That(player.transform.position.x, Is.InRange(finalPlatformBounds.min.x - 0.25f, finalPlatformBounds.max.x + 0.25f),
                "Expected the player to remain over the moving platform after the ride.");
            Assert.That(player.transform.position.y, Is.GreaterThan(finalPlatformBounds.max.y - 0.2f),
                "Expected the player to stay on top of the moving platform while it carries them.");
            Assert.That(player.transform.position.x - movingPlatform.transform.position.x, Is.EqualTo(initialRelativeOffsetX).Within(0.08f),
                "Expected an idle player to keep the same relative position while riding the moving platform.");
            Assert.That(playerBody.linearVelocity.x - movingPlatform.CurrentVelocity.x, Is.EqualTo(0f).Within(0.03f),
                "Expected an idle rider to match the platform velocity instead of drifting across it.");

            float timeoutAt = Time.time + 3f;
            while (Time.time < timeoutAt &&
                (movingPlatform.transform.position.x < 1.28f || movingPlatform.CurrentVelocity.x > 0.01f))
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(movingPlatform.transform.position.x, Is.GreaterThanOrEqualTo(1.28f),
                "Expected the moving platform to reach its right dock.");
            Assert.That(platformCollider.bounds.max.x, Is.EqualTo(midGroundRenderer.bounds.min.x).Within(0.08f),
                "Expected the moving platform's right stop to line up with the left edge of the right-hand ground block.");
            Assert.That(rightDockRenderer.bounds.min.x, Is.EqualTo(midGroundRenderer.bounds.min.x).Within(0.08f),
                "Expected the right dock marker to sit at the left edge of the right-hand ground block.");
        }

        [UnityTest]
        public IEnumerator ThornBridgeTurnsDangerousAndRespawnsThePlayer()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            CyclingSpikeHazard2D thornBridge = Object.FindAnyObjectByType<CyclingSpikeHazard2D>();
            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            Rigidbody2D playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
            GameObject respawnObject = GameObject.Find("Respawn Point");

            Assert.That(thornBridge, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(respawnObject, Is.Not.Null);

            player.transform.SetParent(null, true);
            playerBody.linearVelocity = Vector2.zero;
            player.transform.position = thornBridge.transform.position + new Vector3(0f, 0.25f, 0f);
            Physics2D.SyncTransforms();

            float timeoutAt = Time.time + thornBridge.SafeDuration + thornBridge.WarningDuration + 1.0f;
            while (Time.time < timeoutAt && thornBridge.CurrentState != CyclingSpikeHazard2D.HazardState.Danger)
            {
                yield return null;
            }

            Assert.That(thornBridge.CurrentState, Is.EqualTo(CyclingSpikeHazard2D.HazardState.Danger),
                "Expected the thorn bridge to cycle into its dangerous state.");

            for (int index = 0; index < 5; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.transform.position.x, Is.EqualTo(respawnObject.transform.position.x).Within(0.05f));
            Assert.That(player.transform.position.y, Is.EqualTo(respawnObject.transform.position.y).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator LandingOnMovingPlatformStopsResidualRelativeMotion()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            MovingPlatform2D movingPlatform = Object.FindAnyObjectByType<MovingPlatform2D>();
            Rigidbody2D playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
            Collider2D platformCollider = movingPlatform != null ? movingPlatform.GetComponent<Collider2D>() : null;

            Assert.That(player, Is.Not.Null);
            Assert.That(movingPlatform, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(platformCollider, Is.Not.Null);

            Bounds platformBounds = platformCollider.bounds;
            playerBody.linearVelocity = new Vector2(3.25f, -2f);
            player.transform.position = new Vector3(platformBounds.center.x, platformBounds.max.y + 0.9f, 0f);
            Physics2D.SyncTransforms();

            for (int index = 0; index < 60 && !player.IsGrounded; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.IsGrounded, Is.True, "Expected the player to land back on the moving platform.");

            float relativeOffsetAfterLanding = player.transform.position.x - movingPlatform.transform.position.x;

            for (int index = 0; index < 2; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.transform.position.x - movingPlatform.transform.position.x, Is.EqualTo(relativeOffsetAfterLanding).Within(0.12f),
                "Expected landing on the moving platform to clear leftover horizontal drift when no input is held.");
            Assert.That(playerBody.linearVelocity.x - movingPlatform.CurrentVelocity.x, Is.EqualTo(0f).Within(0.03f),
                "Expected landing with no input to settle to the platform velocity without residual sideways drift.");
        }

        [UnityTest]
        public IEnumerator KillZoneRespawnsPlayerAtTheLevelStart()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            KillZone2D killZone = Object.FindAnyObjectByType<KillZone2D>();
            Rigidbody2D playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
            GameObject respawnObject = GameObject.Find("Respawn Point");

            Assert.That(player, Is.Not.Null);
            Assert.That(killZone, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(respawnObject, Is.Not.Null);

            player.transform.SetParent(null, true);
            playerBody.linearVelocity = Vector2.zero;
            player.transform.position = killZone.transform.position;
            Physics2D.SyncTransforms();

            for (int index = 0; index < 5; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.transform.position.x, Is.EqualTo(respawnObject.transform.position.x).Within(0.05f));
            Assert.That(player.transform.position.y, Is.EqualTo(respawnObject.transform.position.y).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator ActivatingCheckpointChangesTheRespawnLocation()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            GravityGardenGameManager gameManager = Object.FindAnyObjectByType<GravityGardenGameManager>();
            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            Checkpoint2D checkpoint = Object.FindAnyObjectByType<Checkpoint2D>();
            KillZone2D killZone = Object.FindAnyObjectByType<KillZone2D>();
            Rigidbody2D playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;

            Assert.That(gameManager, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(checkpoint, Is.Not.Null);
            Assert.That(killZone, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);

            player.transform.SetParent(null, true);
            playerBody.linearVelocity = Vector2.zero;
            player.transform.position = checkpoint.transform.position;
            Physics2D.SyncTransforms();

            for (int index = 0; index < 5 && gameManager.ActiveCheckpoint != checkpoint; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(gameManager.ActiveCheckpoint, Is.SameAs(checkpoint),
                "Expected touching the checkpoint to activate it.");

            player.transform.position = killZone.transform.position;
            Physics2D.SyncTransforms();

            for (int index = 0; index < 5; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.transform.position.x, Is.EqualTo(checkpoint.RespawnPoint.position.x).Within(0.05f));
            Assert.That(player.transform.position.y, Is.EqualTo(checkpoint.RespawnPoint.position.y).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator ExitRequiresMinimumSeedsAndThenAllowsVictory()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            GravityGardenGameManager gameManager = Object.FindAnyObjectByType<GravityGardenGameManager>();
            EnergySeedCollectible[] seeds = Object.FindObjectsByType<EnergySeedCollectible>(FindObjectsSortMode.None);

            Assert.That(gameManager, Is.Not.Null);
            Assert.That(seeds.Length, Is.GreaterThanOrEqualTo(gameManager.MinimumSeedsToExit));
            Assert.That(gameManager.TryReachExit(), Is.False, "Expected the exit to stay locked until enough seeds are collected.");

            for (int index = 0; index < gameManager.MinimumSeedsToExit; index++)
            {
                Assert.That(gameManager.TryCollectSeed(seeds[index]), Is.True);
            }

            Assert.That(gameManager.CanUseExit, Is.True, "Expected enough seeds to unlock the exit.");
            Assert.That(gameManager.TryReachExit(), Is.True, "Expected the exit to succeed once the seed requirement is met.");
            Assert.That(gameManager.HasWon, Is.True, "Expected the slice to mark itself as won.");
        }
    }
}
