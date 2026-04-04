using System;
using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController2D playerController;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer[] flashRenderers = Array.Empty<SpriteRenderer>();

        [Header("Health")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invulnerabilityDuration = 1f;

        [Header("Hit Response")]
        [SerializeField] private float horizontalKnockbackSpeed = 4.25f;
        [SerializeField] private float verticalKnockbackSpeed = 5.75f;
        [SerializeField] private float blinkInterval = 0.1f;

        [SerializeField, HideInInspector] private int currentHealth = 3;

        private float invulnerabilityTimer;
        private float blinkTimer;
        private bool renderersVisible = true;

        public event Action StateChanged;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsInvulnerable => invulnerabilityTimer > 0f;
        public float InvulnerabilityDuration => invulnerabilityDuration;

        private void Reset()
        {
            playerController = GetComponent<PlayerController2D>();
            body = GetComponent<Rigidbody2D>();
            CacheFlashRenderers();
            currentHealth = maxHealth;
        }

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController2D>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (flashRenderers == null || flashRenderers.Length == 0)
            {
                CacheFlashRenderers();
            }

            currentHealth = maxHealth;
            RestoreRendererVisibility();
        }

        private void Update()
        {
            if (!IsInvulnerable)
            {
                return;
            }

            invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - Time.deltaTime);
            blinkTimer -= Time.deltaTime;

            if (blinkTimer <= 0f)
            {
                SetRendererVisibility(!renderersVisible);
                blinkTimer = blinkInterval;
            }

            if (!IsInvulnerable)
            {
                RestoreRendererVisibility();
            }
        }

        private void OnDisable()
        {
            RestoreRendererVisibility();
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            invulnerabilityDuration = Mathf.Max(0f, invulnerabilityDuration);
            horizontalKnockbackSpeed = Mathf.Max(0f, horizontalKnockbackSpeed);
            verticalKnockbackSpeed = Mathf.Max(0f, verticalKnockbackSpeed);
            blinkInterval = Mathf.Max(0.04f, blinkInterval);

            if (!Application.isPlaying)
            {
                currentHealth = maxHealth;
            }
        }

        public void Configure(PlayerController2D targetController, Rigidbody2D targetBody, SpriteRenderer[] targetRenderers)
        {
            playerController = targetController;
            body = targetBody;
            flashRenderers = targetRenderers != null && targetRenderers.Length > 0
                ? targetRenderers
                : Array.Empty<SpriteRenderer>();

            if (!Application.isPlaying)
            {
                currentHealth = maxHealth;
            }
        }

        public bool TryTakeDamage(Vector2 damageSourcePosition)
        {
            if (currentHealth <= 0 || IsInvulnerable)
            {
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - 1);
            invulnerabilityTimer = currentHealth > 0 ? invulnerabilityDuration : 0f;
            blinkTimer = blinkInterval;

            if (currentHealth > 0)
            {
                SetRendererVisibility(false);
            }
            else
            {
                RestoreRendererVisibility();
            }

            ApplyKnockback(damageSourcePosition);
            NotifyStateChanged();
            return true;
        }

        public void RestoreFullHealth()
        {
            currentHealth = maxHealth;
            invulnerabilityTimer = 0f;
            blinkTimer = 0f;
            RestoreRendererVisibility();
            NotifyStateChanged();
        }

        private void ApplyKnockback(Vector2 damageSourcePosition)
        {
            if (playerController == null)
            {
                return;
            }

            float horizontalDirection = transform.position.x - damageSourcePosition.x;
            if (Mathf.Abs(horizontalDirection) < 0.01f)
            {
                horizontalDirection = body != null && Mathf.Abs(body.linearVelocity.x) > 0.01f
                    ? -Mathf.Sign(body.linearVelocity.x)
                    : 1f;
            }

            Vector2 knockbackVelocity = new Vector2(
                Mathf.Sign(horizontalDirection) * horizontalKnockbackSpeed,
                verticalKnockbackSpeed);

            playerController.ApplyExternalImpulse(knockbackVelocity);
        }

        private void CacheFlashRenderers()
        {
            flashRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void SetRendererVisibility(bool visible)
        {
            renderersVisible = visible;

            for (int index = 0; index < flashRenderers.Length; index++)
            {
                SpriteRenderer renderer = flashRenderers[index];
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        private void RestoreRendererVisibility()
        {
            SetRendererVisibility(true);
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
