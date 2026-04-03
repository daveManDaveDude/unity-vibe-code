using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ExitPortal : MonoBehaviour
    {
        [SerializeField] private GravityGardenGameManager gameManager;
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color lockedColor = new Color(0.42f, 0.3f, 0.63f, 1f);
        [SerializeField] private Color readyColor = new Color(0.48f, 0.92f, 0.73f, 1f);
        [SerializeField] private Color wonColor = new Color(0.98f, 0.96f, 0.66f, 1f);

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
            if (other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            ResolveGameManager();
            gameManager?.TryReachExit();
            RefreshVisuals();
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

            if (gameManager == null)
            {
                spriteRenderer.color = lockedColor;
                return;
            }

            spriteRenderer.color = gameManager.HasWon
                ? wonColor
                : gameManager.CanUseExit ? readyColor : lockedColor;
        }
    }
}
