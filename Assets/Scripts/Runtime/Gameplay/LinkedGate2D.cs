using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class LinkedGate2D : MonoBehaviour
    {
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer statusIndicatorRenderer;
        [SerializeField] private Color lockedColor = new Color(0.74f, 0.28f, 0.24f, 1f);
        [SerializeField] private Color openColor = new Color(0.45f, 0.9f, 0.61f, 0.12f);
        [SerializeField] private Color lockedIndicatorColor = new Color(0.97f, 0.36f, 0.26f, 1f);
        [SerializeField] private Color openIndicatorColor = new Color(0.45f, 0.9f, 0.61f, 1f);

        public bool IsOpen { get; private set; }

        private void Reset()
        {
            blockingCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (blockingCollider != null)
            {
                blockingCollider.isTrigger = false;
            }
        }

        private void Awake()
        {
            if (blockingCollider == null)
            {
                blockingCollider = GetComponent<Collider2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            RefreshState();
        }

        private void OnValidate()
        {
            if (blockingCollider != null)
            {
                blockingCollider.isTrigger = false;
            }

            RefreshState();
        }

        public void SetOpen(bool open)
        {
            if (IsOpen == open)
            {
                RefreshState();
                return;
            }

            IsOpen = open;
            RefreshState();
        }

        private void RefreshState()
        {
            if (blockingCollider != null)
            {
                blockingCollider.enabled = !IsOpen;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = IsOpen ? openColor : lockedColor;
            }

            if (statusIndicatorRenderer != null)
            {
                statusIndicatorRenderer.color = IsOpen ? openIndicatorColor : lockedIndicatorColor;
            }
        }
    }
}
