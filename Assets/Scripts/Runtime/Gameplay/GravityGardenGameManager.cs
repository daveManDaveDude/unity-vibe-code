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

        public event Action StateChanged;

        public int CollectedSeeds { get; private set; }
        public int TotalSeedsInLevel { get; private set; }
        public int MinimumSeedsToExit => minimumSeedsToExit;
        public bool CanUseExit => CollectedSeeds >= minimumSeedsToExit;
        public bool HasWon { get; private set; }
        public Checkpoint2D ActiveCheckpoint => activeCheckpoint;
        public int SeedsRemainingForExit => Mathf.Max(0, minimumSeedsToExit - CollectedSeeds);
        public string CurrentStatusMessage => Time.unscaledTime <= statusMessageExpiresAt ? currentStatusMessage : string.Empty;

        private void Awake()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController2D>();
            }

            collectedSeeds.Clear();
            TotalSeedsInLevel = FindObjectsByType<EnergySeedCollectible>(FindObjectsInactive.Exclude).Length;
            NotifyStateChanged();
        }

        private void OnValidate()
        {
            minimumSeedsToExit = Mathf.Max(1, minimumSeedsToExit);
            statusMessageDuration = Mathf.Max(0f, statusMessageDuration);
        }

        public void Configure(PlayerController2D targetPlayer, Transform targetRespawnPoint, int requiredSeedsToExit)
        {
            player = targetPlayer;
            respawnPoint = targetRespawnPoint;
            minimumSeedsToExit = Mathf.Max(1, requiredSeedsToExit);
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
                player = targetPlayer;
            }

            ShowStatusMessage("Checkpoint reached.");
            return true;
        }

        public void DefeatPlayer(PlayerController2D targetPlayer = null, string statusMessageOverride = null)
        {
            RespawnPlayer(targetPlayer, statusMessageOverride);
        }

        public void RespawnPlayer(PlayerController2D targetPlayer = null, string statusMessageOverride = null)
        {
            PlayerController2D resolvedPlayer = targetPlayer != null ? targetPlayer : player;
            if (resolvedPlayer == null)
            {
                resolvedPlayer = FindAnyObjectByType<PlayerController2D>();
            }

            Transform activeRespawnPoint = activeCheckpoint != null ? activeCheckpoint.RespawnPoint : respawnPoint;
            if (resolvedPlayer == null || activeRespawnPoint == null)
            {
                return;
            }

            player = resolvedPlayer;
            resolvedPlayer.transform.SetParent(null, true);

            Rigidbody2D body = resolvedPlayer.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            resolvedPlayer.transform.position = activeRespawnPoint.position;
            resolvedPlayer.ResetMotionState();

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

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
