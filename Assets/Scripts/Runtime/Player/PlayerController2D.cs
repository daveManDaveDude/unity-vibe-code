using UnityEngine;
using UnityEngine.InputSystem;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        private enum PlayerVisualState
        {
            Idle,
            Run,
            Jump
        }

        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CapsuleCollider2D bodyCollider;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float groundAcceleration = 45f;
        [SerializeField] private float groundDeceleration = 60f;
        [SerializeField] private float airAcceleration = 26.25f;
        [SerializeField] private float airDeceleration = 30f;
        [SerializeField] private int maxJumpCount = 2;
        [SerializeField] private float jumpVelocity = 9.2f;

        [Header("Jump Feel")]
        [SerializeField] private float lowJumpGravityMultiplier = 2f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField] private float groundCheckRadius = 0.03f;

        [Header("Placeholder Visuals")]
        [SerializeField] private Color idleColor = new Color(0.93f, 0.53f, 0.38f, 1f);
        [SerializeField] private Color runColor = new Color(0.99f, 0.62f, 0.4f, 1f);
        [SerializeField] private Color jumpColor = new Color(1f, 0.83f, 0.52f, 1f);
        [SerializeField] private Vector2 idleVisualSize = new Vector2(0.225f, 0.45f);
        [SerializeField] private Vector2 runVisualSize = new Vector2(0.245f, 0.435f);
        [SerializeField] private Vector2 jumpVisualSize = new Vector2(0.21f, 0.4875f);
        [SerializeField] private float runBounceAmount = 0.05f;
        [SerializeField] private float visualLerpSpeed = 14f;

        [Header("Placeholder Effects")]
        [SerializeField] private bool spawnPlaceholderDust = true;
        [SerializeField] private float landingDustMinSpeed = 7f;
        [SerializeField] private Color dustColor = new Color(1f, 0.93f, 0.78f, 0.9f);

        private readonly Collider2D[] groundHits = new Collider2D[8];
        private InputAction moveAction;
        private InputAction jumpAction;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;
        private bool jumpQueued;
        private float defaultGravityScale;
        private int jumpsUsed;
        private float lastVerticalVelocity;
        private float facingDirection = 1f;
        private bool wasGroundedLastStep;
        private PlayerVisualState currentVisualState;

        public bool IsGrounded { get; private set; }
        public float VerticalVelocity => body != null ? body.linearVelocity.y : 0f;
        public float PreviousVerticalVelocity => lastVerticalVelocity;
        public Bounds CollisionBounds => bodyCollider != null
            ? bodyCollider.bounds
            : new Bounds(transform.position, Vector3.zero);

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (body != null)
            {
                body.gravityScale = 4f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (bodyCollider != null)
            {
                bodyCollider.direction = CapsuleDirection2D.Vertical;
                bodyCollider.size = new Vector2(0.225f, 0.45f);
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
                bodyCollider = GetComponent<CapsuleCollider2D>();
            }

            if (groundCheck == null)
            {
                Transform existingGroundCheck = transform.Find("GroundCheck");
                if (existingGroundCheck != null)
                {
                    groundCheck = existingGroundCheck;
                }
            }

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            defaultGravityScale = body != null ? body.gravityScale : 1f;
            EnsureVisualSetup();
            BindActions();
        }

        private void OnEnable()
        {
            BindActions();
            moveAction?.Enable();
            jumpAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
        }

        private void OnValidate()
        {
            maxJumpCount = Mathf.Max(1, maxJumpCount);
        }

        private void Update()
        {
            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }
            else
            {
                moveInput = Vector2.zero;
            }

            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                jumpQueued = true;
            }
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();
            HandleGroundTransitions();
            ApplyHorizontalMovement(Time.fixedDeltaTime);
            TryJump();
            ApplyGravityTuning();
            UpdateVisuals(Time.fixedDeltaTime);

            wasGroundedLastStep = IsGrounded;
            if (body != null)
            {
                lastVerticalVelocity = body.linearVelocity.y;
            }
        }

        public void Configure(Rigidbody2D targetBody, CapsuleCollider2D targetCollider, Transform targetGroundCheck, InputActionAsset actions, LayerMask groundMask)
        {
            body = targetBody;
            bodyCollider = targetCollider;
            groundCheck = targetGroundCheck;
            inputActions = actions;
            groundLayers = groundMask;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            defaultGravityScale = body != null ? body.gravityScale : defaultGravityScale;
            EnsureVisualSetup();
            BindActions();
        }

        public void ResetMotionState()
        {
            moveInput = Vector2.zero;
            jumpQueued = false;
            jumpsUsed = 0;
            IsGrounded = false;
            lastVerticalVelocity = 0f;
            wasGroundedLastStep = false;

            if (body != null)
            {
                body.gravityScale = defaultGravityScale;
            }

            UpdateGroundedState();
            UpdateVisuals(0f, true);
        }

        public void Bounce(float upwardVelocity)
        {
            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, upwardVelocity);
            body.linearVelocity = velocity;
            body.gravityScale = defaultGravityScale;
            IsGrounded = false;
            jumpQueued = false;
        }

        private void BindActions()
        {
            if (inputActions == null)
            {
                moveAction = null;
                jumpAction = null;
                return;
            }

            InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogWarning($"Could not find action map '{actionMapName}' on '{inputActions.name}'.", this);
                moveAction = null;
                jumpAction = null;
                return;
            }

            moveAction = actionMap.FindAction(moveActionName, false);
            jumpAction = actionMap.FindAction(jumpActionName, false);

            if (moveAction == null || jumpAction == null)
            {
                Debug.LogWarning($"Could not find '{moveActionName}' or '{jumpActionName}' actions on '{actionMapName}'.", this);
            }
        }

        private void UpdateGroundedState()
        {
            bool grounded;

            if (groundCheck != null)
            {
                grounded = HasGroundContact(groundCheck.position, groundCheckRadius);
            }
            else if (bodyCollider != null)
            {
                Bounds bounds = bodyCollider.bounds;
                Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - (groundCheckRadius * 0.5f));
                Vector2 boxSize = new Vector2(bounds.size.x * 0.85f, groundCheckRadius);
                grounded = HasGroundContact(boxCenter, boxSize);
            }
            else
            {
                grounded = false;
            }

            IsGrounded = grounded;

            if (IsGrounded)
            {
                jumpsUsed = 0;
            }
        }

        private bool HasGroundContact(Vector2 center, float radius)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;

            int hitCount = Physics2D.OverlapCircle(center, radius, filter, groundHits);
            return ContainsValidGroundHit(hitCount);
        }

        private bool HasGroundContact(Vector2 center, Vector2 size)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;

            int hitCount = Physics2D.OverlapBox(center, size, 0f, filter, groundHits);
            return ContainsValidGroundHit(hitCount);
        }

        private bool ContainsValidGroundHit(int hitCount)
        {
            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = groundHits[index];
                groundHits[index] = null;

                if (!IsSelfCollider(hit))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSelfCollider(Collider2D hit)
        {
            if (hit == null)
            {
                return true;
            }

            if (hit == bodyCollider)
            {
                return true;
            }

            if (body != null && hit.attachedRigidbody == body)
            {
                return true;
            }

            Transform hitTransform = hit.transform;
            return hitTransform == transform || hitTransform.IsChildOf(transform);
        }

        private void ApplyHorizontalMovement(float deltaTime)
        {
            if (body == null)
            {
                return;
            }

            float targetVelocityX = moveInput.x * moveSpeed;
            Vector2 velocity = body.linearVelocity;
            float acceleration = IsGrounded ? groundAcceleration : airAcceleration;
            float deceleration = IsGrounded ? groundDeceleration : airDeceleration;
            bool isStopping = Mathf.Abs(targetVelocityX) < 0.01f;
            bool isReversing = Mathf.Abs(velocity.x) > 0.01f
                && Mathf.Abs(targetVelocityX) > 0.01f
                && Mathf.Sign(targetVelocityX) != Mathf.Sign(velocity.x);
            float moveRate = (isStopping || isReversing ? deceleration : acceleration) * deltaTime;
            velocity.x = Mathf.MoveTowards(velocity.x, targetVelocityX, moveRate);
            body.linearVelocity = velocity;

            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                facingDirection = moveInput.x < 0f ? -1f : 1f;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = facingDirection < 0f;
            }
        }

        private void TryJump()
        {
            if (!jumpQueued)
            {
                return;
            }

            jumpQueued = false;

            if (body == null)
            {
                return;
            }

            if (!CanJump())
            {
                return;
            }

            bool groundJump = IsGrounded;
            Vector2 velocity = body.linearVelocity;
            velocity.y = jumpVelocity;
            body.linearVelocity = velocity;

            jumpsUsed = Mathf.Min(jumpsUsed + 1, maxJumpCount);

            if (groundJump)
            {
                SpawnDustBurst(0.7f, 0.35f, 0.22f);
            }

            IsGrounded = false;
        }

        private bool CanJump()
        {
            return IsGrounded || jumpsUsed < maxJumpCount;
        }

        private void ApplyGravityTuning()
        {
            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            bool jumpHeld = jumpAction != null && jumpAction.IsPressed();
            float gravityMultiplier = velocity.y > 0f && !jumpHeld ? lowJumpGravityMultiplier : 1f;

            body.gravityScale = defaultGravityScale * gravityMultiplier;

            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
                body.linearVelocity = velocity;
            }
        }

        private void EnsureVisualSetup()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (spriteRenderer.drawMode == SpriteDrawMode.Simple)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            }

            spriteRenderer.color = idleColor;
            spriteRenderer.size = idleVisualSize;
            spriteRenderer.flipX = facingDirection < 0f;
        }

        private void HandleGroundTransitions()
        {
            if (!IsGrounded || wasGroundedLastStep)
            {
                return;
            }

            if (lastVerticalVelocity <= -landingDustMinSpeed)
            {
                SpawnDustBurst(1f, 0.15f, 0.28f);
            }
        }

        private void UpdateVisuals(float deltaTime, bool snap = false)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            currentVisualState = ResolveVisualState();

            Color targetColor = idleColor;
            Vector2 targetSize = idleVisualSize;

            switch (currentVisualState)
            {
                case PlayerVisualState.Run:
                    targetColor = runColor;
                    targetSize = GetRunVisualSize();
                    break;
                case PlayerVisualState.Jump:
                    targetColor = jumpColor;
                    targetSize = jumpVisualSize;
                    break;
            }

            if (snap || deltaTime <= 0f)
            {
                spriteRenderer.color = targetColor;
                spriteRenderer.size = targetSize;
                return;
            }

            float blend = 1f - Mathf.Exp(-visualLerpSpeed * deltaTime);
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, blend);
            spriteRenderer.size = Vector2.Lerp(spriteRenderer.size, targetSize, blend);
        }

        private PlayerVisualState ResolveVisualState()
        {
            if (!IsGrounded)
            {
                return PlayerVisualState.Jump;
            }

            return Mathf.Abs(body != null ? body.linearVelocity.x : moveInput.x) > 0.15f
                ? PlayerVisualState.Run
                : PlayerVisualState.Idle;
        }

        private Vector2 GetRunVisualSize()
        {
            float bounce = (Mathf.Sin(Time.time * 18f) * 0.5f) + 0.5f;
            float width = Mathf.Lerp(runVisualSize.x - runBounceAmount, runVisualSize.x + runBounceAmount, bounce);
            float height = Mathf.Lerp(runVisualSize.y + runBounceAmount, runVisualSize.y - runBounceAmount, bounce);
            return new Vector2(width, height);
        }

        private void SpawnDustBurst(float widthScale, float upwardVelocity, float lifetime)
        {
            if (!spawnPlaceholderDust || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Vector3 origin = groundCheck != null
                ? groundCheck.position
                : new Vector3(transform.position.x, bodyCollider != null ? bodyCollider.bounds.min.y : transform.position.y, transform.position.z);

            SpawnDustParticle(origin + new Vector3(-0.12f * facingDirection, -0.03f, 0f), new Vector2(-0.7f * facingDirection, upwardVelocity), widthScale, lifetime);
            SpawnDustParticle(origin + new Vector3(0.12f * facingDirection, -0.03f, 0f), new Vector2(0.7f * facingDirection, upwardVelocity * 1.1f), widthScale, lifetime);
        }

        private void SpawnDustParticle(Vector3 position, Vector2 velocity, float scaleMultiplier, float lifetime)
        {
            GameObject effectObject = new GameObject("Player Dust");
            effectObject.transform.position = position;

            PlaceholderBurstEffect effect = effectObject.AddComponent<PlaceholderBurstEffect>();
            effect.Initialize(
                spriteRenderer.sprite,
                dustColor,
                spriteRenderer.sortingOrder - 1,
                Vector3.one * (0.12f * scaleMultiplier),
                new Vector3(0.34f * scaleMultiplier, 0.2f * scaleMultiplier, 1f),
                velocity,
                lifetime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            if (groundCheck != null)
            {
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
            else if (bodyCollider != null)
            {
                Bounds bounds = bodyCollider.bounds;
                Vector3 boxCenter = new Vector3(bounds.center.x, bounds.min.y - (groundCheckRadius * 0.5f), 0f);
                Vector3 boxSize = new Vector3(bounds.size.x * 0.85f, groundCheckRadius, 0f);
                Gizmos.DrawWireCube(boxCenter, boxSize);
            }
        }
    }
}
