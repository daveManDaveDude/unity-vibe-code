using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Enemy2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GravityGardenGameManager gameManager;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Player Interaction")]
        [SerializeField] private bool canBeStomped = true;
        [SerializeField] private float stompBounceVelocity = 8.5f;
        [SerializeField] private float stompMinFallSpeed = 0.1f;
        [SerializeField] private float stompContactPadding = 0.08f;
        [SerializeField] private string defeatPlayerMessage = "A garden critter got you.";

        [Header("Placeholder Effects")]
        [SerializeField] private bool spawnDefeatBurst = true;
        [SerializeField] private Color defeatBurstColor = new Color(1f, 0.78f, 0.32f, 0.95f);

        public bool IsDefeated { get; private set; }
        public Rigidbody2D Body => body;
        public Collider2D BodyCollider => bodyCollider;
        public SpriteRenderer Visual => spriteRenderer;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (body != null)
            {
                body.gravityScale = 4f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            ResolveGameManager();
        }

        private void OnValidate()
        {
            stompBounceVelocity = Mathf.Max(0f, stompBounceVelocity);
            stompMinFallSpeed = Mathf.Max(0f, stompMinFallSpeed);
            stompContactPadding = Mathf.Max(0f, stompContactPadding);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerCollision(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandlePlayerCollision(collision);
        }

        private void HandlePlayerCollision(Collision2D collision)
        {
            if (IsDefeated)
            {
                return;
            }

            PlayerController2D player = collision.otherCollider.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
                player = collision.collider.GetComponentInParent<PlayerController2D>();
            }

            if (player == null)
            {
                return;
            }

            if (IsStompCollision(player, collision))
            {
                player.Bounce(stompBounceVelocity);
                Defeat();
                return;
            }

            ResolveGameManager();
            gameManager?.DefeatPlayer(player, defeatPlayerMessage);
        }

        public void SetFacingDirection(float direction)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction < 0f;
            }
        }

        public void Defeat()
        {
            if (IsDefeated)
            {
                return;
            }

            IsDefeated = true;
            SpawnDefeatEffect();

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            if (body != null)
            {
                body.simulated = false;
            }

            Destroy(gameObject);
        }

        private bool IsStompCollision(PlayerController2D player, Collision2D collision)
        {
            if (!canBeStomped || player == null)
            {
                return false;
            }

            float strongestDownwardVelocity = Mathf.Min(player.VerticalVelocity, player.PreviousVerticalVelocity);
            if (strongestDownwardVelocity > -stompMinFallSpeed)
            {
                return false;
            }

            Bounds enemyBounds = bodyCollider != null
                ? bodyCollider.bounds
                : new Bounds(transform.position, Vector3.zero);

            float stompLine = enemyBounds.center.y + stompContactPadding;
            Bounds playerBounds = player.CollisionBounds;
            bool playerAboveEnemy = playerBounds.center.y >= enemyBounds.center.y;

            if (playerBounds.min.y >= stompLine && playerAboveEnemy)
            {
                return true;
            }

            for (int index = 0; index < collision.contactCount; index++)
            {
                ContactPoint2D contact = collision.GetContact(index);
                if (contact.point.y >= stompLine && playerAboveEnemy)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }
        }

        private void SpawnDefeatEffect()
        {
            if (!spawnDefeatBurst || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Vector2 burstSize = bodyCollider != null
                ? bodyCollider.bounds.size
                : new Vector2(0.35f, 0.35f);

            GameObject effectObject = new GameObject($"{name} Burst");
            effectObject.transform.position = transform.position;

            PlaceholderBurstEffect effect = effectObject.AddComponent<PlaceholderBurstEffect>();
            effect.Initialize(
                spriteRenderer.sprite,
                defeatBurstColor,
                spriteRenderer.sortingOrder,
                new Vector3(burstSize.x * 0.65f, burstSize.y * 0.65f, 1f),
                new Vector3(burstSize.x * 1.2f, burstSize.y * 1.2f, 1f),
                new Vector2(0f, 1.2f),
                0.22f);
        }
    }
}
