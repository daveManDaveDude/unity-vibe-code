using System;
using System.Collections.Generic;
using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    public class GravityGardenGameManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private PlayerController2D player;
        [SerializeField] private Transform respawnPoint;

        [Header("Rules")]
        [SerializeField] private int minimumSeedsToExit = 3;
        [SerializeField] private float statusMessageDuration = 1.5f;

        private float statusMessageExpiresAt;
        private string currentStatusMessage = string.Empty;
        private readonly HashSet<EnergySeedCollectible> collectedSeeds = new HashSet<EnergySeedCollectible>();
        private Checkpoint2D activeCheckpoint;
        private PlayerHealth2D playerHealth;

        public event Action StateChanged;

        public int CollectedSeeds { get; private set; }
        public int TotalSeedsInLevel { get; private set; }
        public int MinimumSeedsToExit => minimumSeedsToExit;
        public bool CanUseExit => CollectedSeeds >= minimumSeedsToExit;
        public bool HasWon { get; private set; }
        public Checkpoint2D ActiveCheckpoint => activeCheckpoint;
        public int CurrentHealth => playerHealth != null ? playerHealth.CurrentHealth : 0;
        public int MaxHealth => playerHealth != null ? playerHealth.MaxHealth : 0;
        public int SeedsRemainingForExit => Mathf.Max(0, minimumSeedsToExit - CollectedSeeds);
        public string CurrentStatusMessage => Time.unscaledTime <= statusMessageExpiresAt ? currentStatusMessage : string.Empty;

        private void Awake()
        {
            ResolvePlayerReference();
            collectedSeeds.Clear();
            TotalSeedsInLevel = FindObjectsByType<EnergySeedCollectible>(FindObjectsInactive.Exclude).Length;
            NotifyStateChanged();
        }

        private void OnDisable()
        {
            SubscribeToPlayerHealth(null);
        }

        private void OnValidate()
        {
            minimumSeedsToExit = Mathf.Max(1, minimumSeedsToExit);
            statusMessageDuration = Mathf.Max(0f, statusMessageDuration);
        }

        public void Configure(PlayerController2D targetPlayer, Transform targetRespawnPoint, int requiredSeedsToExit)
        {
            AssignPlayer(targetPlayer);
            respawnPoint = targetRespawnPoint;
            minimumSeedsToExit = Mathf.Max(1, requiredSeedsToExit);
            NotifyStateChanged();
        }

        public bool TryCollectSeed(EnergySeedCollectible seed)
        {
            if (seed == null || HasWon || collectedSeeds.Contains(seed))
            {
                return false;
            }

            collectedSeeds.Add(seed);
            CollectedSeeds += Mathf.Max(1, seed.SeedValue);
            ShowStatusMessage("Energy seed collected.");
            return true;
        }

        public bool TryReachExit()
        {
            if (HasWon)
            {
                return true;
            }

            if (!CanUseExit)
            {
                int remaining = SeedsRemainingForExit;
                ShowStatusMessage($"Collect {remaining} more seed{(remaining == 1 ? string.Empty : "s")} to awaken the portal.");
                return false;
            }

            HasWon = true;
            ShowStatusMessage("The garden stirs. You made it home.");
            return true;
        }

        public bool TryActivateCheckpoint(Checkpoint2D checkpoint, PlayerController2D targetPlayer = null)
        {
            if (checkpoint == null || activeCheckpoint == checkpoint)
            {
                return false;
            }

            activeCheckpoint = checkpoint;
            if (targetPlayer != null)
            {
                AssignPlayer(targetPlayer);
            }

            ShowStatusMessage("Checkpoint reached.");
            return true;
        }

        public bool DamagePlayer(
            PlayerController2D targetPlayer = null,
            string damageMessageOverride = null,
            string defeatMessageOverride = null,
            Vector2? damageSource = null)
        {
            PlayerController2D resolvedPlayer = ResolveTargetPlayer(targetPlayer);
            if (resolvedPlayer == null)
            {
                return false;
            }

            PlayerHealth2D resolvedHealth = resolvedPlayer.GetComponent<PlayerHealth2D>();
            if (resolvedHealth == null)
            {
                RespawnPlayer(resolvedPlayer, defeatMessageOverride);
                return true;
            }

            if (!resolvedHealth.TryTakeDamage(damageSource ?? resolvedPlayer.transform.position))
            {
                return false;
            }

            if (resolvedHealth.CurrentHealth <= 0)
            {
                RespawnPlayer(resolvedPlayer, defeatMessageOverride);
                return true;
            }

            ShowStatusMessage(BuildDamageMessage(damageMessageOverride, resolvedHealth.CurrentHealth));
            return true;
        }

        public void DefeatPlayer(PlayerController2D targetPlayer = null, string statusMessageOverride = null)
        {
            RespawnPlayer(targetPlayer, statusMessageOverride);
        }

        public void RespawnPlayer(PlayerController2D targetPlayer = null, string statusMessageOverride = null)
        {
            PlayerController2D resolvedPlayer = ResolveTargetPlayer(targetPlayer);

            Transform activeRespawnPoint = activeCheckpoint != null ? activeCheckpoint.RespawnPoint : respawnPoint;
            if (resolvedPlayer == null || activeRespawnPoint == null)
            {
                return;
            }

            AssignPlayer(resolvedPlayer);
            resolvedPlayer.transform.SetParent(null, true);

            Rigidbody2D body = resolvedPlayer.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            resolvedPlayer.transform.position = activeRespawnPoint.position;
            resolvedPlayer.ResetMotionState();
            playerHealth?.RestoreFullHealth();

            string defaultMessage = activeCheckpoint != null
                ? "You fell out of the garden. Back to the last checkpoint."
                : "You fell out of the garden. Back to the start.";

            ShowStatusMessage(string.IsNullOrWhiteSpace(statusMessageOverride) ? defaultMessage : statusMessageOverride);
        }

        private void ShowStatusMessage(string message)
        {
            currentStatusMessage = message;
            statusMessageExpiresAt = Time.unscaledTime + statusMessageDuration;
            NotifyStateChanged();
        }

        private string BuildDamageMessage(string damageMessageOverride, int remainingHealth)
        {
            string heartsText = $"{remainingHealth} {(remainingHealth == 1 ? "heart" : "hearts")} left.";
            return string.IsNullOrWhiteSpace(damageMessageOverride)
                ? $"Ouch! {heartsText}"
                : $"{damageMessageOverride} {heartsText}";
        }

        private PlayerController2D ResolveTargetPlayer(PlayerController2D targetPlayer)
        {
            if (targetPlayer != null)
            {
                AssignPlayer(targetPlayer);
                return targetPlayer;
            }

            ResolvePlayerReference();
            return player;
        }

        private void ResolvePlayerReference()
        {
            if (player == null)
            {
                AssignPlayer(FindAnyObjectByType<PlayerController2D>());
                return;
            }

            if (playerHealth == null)
            {
                AssignPlayer(player);
            }
        }

        private void AssignPlayer(PlayerController2D targetPlayer)
        {
            if (player == targetPlayer && (targetPlayer == null || playerHealth == targetPlayer.GetComponent<PlayerHealth2D>()))
            {
                return;
            }

            player = targetPlayer;
            PlayerHealth2D targetHealth = player != null ? player.GetComponent<PlayerHealth2D>() : null;
            SubscribeToPlayerHealth(targetHealth);
        }

        private void SubscribeToPlayerHealth(PlayerHealth2D targetHealth)
        {
            if (playerHealth != null)
            {
                playerHealth.StateChanged -= NotifyStateChanged;
            }

            playerHealth = targetHealth;

            if (playerHealth != null)
            {
                playerHealth.StateChanged += NotifyStateChanged;
            }
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
