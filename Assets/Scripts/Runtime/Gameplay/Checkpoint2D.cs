using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Checkpoint2D : MonoBehaviour
    {
        [SerializeField] private GravityGardenGameManager gameManager;
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private Color inactiveColor = new Color(0.47f, 0.66f, 0.9f, 1f);
        [SerializeField] private Color activeColor = new Color(1f, 0.95f, 0.55f, 1f);

        public Transform RespawnPoint => respawnPoint != null ? respawnPoint : transform;

        private void Reset()
        {
            triggerCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            ResolveGameManager();
            RefreshVisuals();
        }

        private void OnEnable()
        {
            ResolveGameManager();
            if (gameManager != null)
            {
                gameManager.StateChanged += RefreshVisuals;
            }

            RefreshVisuals();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= RefreshVisuals;
            }
        }

        private void OnValidate()
        {
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            ResolveGameManager();
            if (gameManager != null && gameManager.TryActivateCheckpoint(this, player))
            {
                RefreshVisuals();
            }
        }

        private void ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }
        }

        private void RefreshVisuals()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            bool isActiveCheckpoint = gameManager != null && gameManager.ActiveCheckpoint == this;
            spriteRenderer.color = isActiveCheckpoint ? activeColor : inactiveColor;
        }
    }
}
