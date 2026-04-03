using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class FloorButton2D : MonoBehaviour
    {
        [SerializeField] private LinkedGate2D linkedGate;
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color idleColor = new Color(0.95f, 0.83f, 0.3f, 1f);
        [SerializeField] private Color activatedColor = new Color(0.48f, 0.89f, 0.62f, 1f);
        [SerializeField] private float pressedHeightScale = 0.55f;

        private Vector3 initialScale;

        public bool IsActivated { get; private set; }
        public LinkedGate2D LinkedGate => linkedGate;

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

            initialScale = transform.localScale;
            RefreshVisuals();
        }

        private void OnValidate()
        {
            pressedHeightScale = Mathf.Clamp(pressedHeightScale, 0.2f, 1f);

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            if (!Application.isPlaying && transform != null)
            {
                initialScale = transform.localScale;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryActivateFrom(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryActivateFrom(other);
        }

        public void Activate()
        {
            if (IsActivated)
            {
                return;
            }

            // Latch after the first press so a single player can solve the gate puzzle cleanly.
            IsActivated = true;
            linkedGate?.SetOpen(true);
            RefreshVisuals();
        }

        private void TryActivateFrom(Collider2D other)
        {
            if (IsActivated || other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            Activate();
        }

        private void RefreshVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = IsActivated ? activatedColor : idleColor;
            }

            if (initialScale == Vector3.zero)
            {
                initialScale = transform.localScale;
            }

            transform.localScale = IsActivated
                ? new Vector3(initialScale.x, initialScale.y * pressedHeightScale, initialScale.z)
                : initialScale;
        }
    }
}
