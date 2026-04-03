using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnergySeedCollectible : MonoBehaviour
    {
        [SerializeField] private GravityGardenGameManager gameManager;
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private int seedValue = 1;

        private bool collected;

        public int SeedValue => seedValue;

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

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }
        }

        private void OnValidate()
        {
            seedValue = Mathf.Max(1, seedValue);

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollectFrom(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryCollectFrom(other);
        }

        private void TryCollectFrom(Collider2D other)
        {
            if (collected)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }

            if (gameManager == null || !gameManager.TryCollectSeed(this))
            {
                return;
            }

            collected = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            gameObject.SetActive(false);
        }
    }
}
