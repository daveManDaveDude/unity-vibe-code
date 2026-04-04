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
        [SerializeField] private bool stopHorizontalMotionOnLanding = true;
        [SerializeField] private bool stabilizeIdleOnMovingPlatform = true;
        [SerializeField] private float landingStopInputThreshold = 0.1f;
        [SerializeField] private int maxJumpCount = 2;
        [SerializeField] private float jumpVelocity = 9.2f;

        [Header("Jump Feel")]
        [SerializeField] private float lowJumpGravityMultiplier = 2f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Ledge Assist")]
        [SerializeField] private bool enableLedgeAssist = true;
        [SerializeField] private float ledgeAssistPauseDuration = 0.08f;
        [SerializeField] private float ledgeAssistHorizontalDistance = 0.18f;
        [SerializeField] private float ledgeAssistTopSearchHeight = 0.65f;
        [SerializeField] private float ledgeAssistStandClearance = 0.02f;
        [SerializeField] private float ledgeAssistMaxRiseSpeed = 4f;
        [SerializeField] private float ledgeAssistMaxFallSpeed = 6f;
        [SerializeField] private float ledgeAssistMinInput = 0.2f;

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
        private readonly Collider2D[] overlapHits = new Collider2D[8];
        private readonly RaycastHit2D[] raycastHits = new RaycastHit2D[8];
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
        private MovingPlatform2D currentGroundPlatform;
        private bool ledgeAssistActive;
        private float ledgeAssistTimer;
        private Vector2 ledgeAssistTargetPosition;
        private float ledgeAssistDirection;
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
            CancelLedgeAssist();
            moveAction?.Disable();
            jumpAction?.Disable();
        }

        private void OnValidate()
        {
            maxJumpCount = Mathf.Max(1, maxJumpCount);
            landingStopInputThreshold = Mathf.Clamp01(landingStopInputThreshold);
            ledgeAssistPauseDuration = Mathf.Max(0f, ledgeAssistPauseDuration);
            ledgeAssistHorizontalDistance = Mathf.Max(0.05f, ledgeAssistHorizontalDistance);
            ledgeAssistTopSearchHeight = Mathf.Max(0.1f, ledgeAssistTopSearchHeight);
            ledgeAssistStandClearance = Mathf.Max(0f, ledgeAssistStandClearance);
            ledgeAssistMaxRiseSpeed = Mathf.Max(0f, ledgeAssistMaxRiseSpeed);
            ledgeAssistMaxFallSpeed = Mathf.Max(0f, ledgeAssistMaxFallSpeed);
            ledgeAssistMinInput = Mathf.Clamp01(ledgeAssistMinInput);
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
            ApplyGroundPlatformMotion();
            if (!ledgeAssistActive)
            {
                TryStartLedgeAssist();
            }

            if (ledgeAssistActive)
            {
                UpdateLedgeAssist(Time.fixedDeltaTime);
            }
            else
            {
                HandleGroundTransitions();
                ApplyHorizontalMovement(Time.fixedDeltaTime);
                TryJump();
                ApplyGravityTuning();
                StabilizeIdleOnMovingPlatform();
            }

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
            CancelLedgeAssist();
            moveInput = Vector2.zero;
            jumpQueued = false;
            jumpsUsed = 0;
            IsGrounded = false;
            lastVerticalVelocity = 0f;
            wasGroundedLastStep = false;
            currentGroundPlatform = null;

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

            CancelLedgeAssist();
            Vector2 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, upwardVelocity);
            body.linearVelocity = velocity;
            body.gravityScale = defaultGravityScale;
            IsGrounded = false;
            jumpQueued = false;
        }

        public void ApplyExternalImpulse(Vector2 velocity)
        {
            if (body == null)
            {
                return;
            }

            CancelLedgeAssist();
            Vector2 currentVelocity = body.linearVelocity;
            currentVelocity.x = velocity.x;
            currentVelocity.y = Mathf.Max(currentVelocity.y, velocity.y);
            body.linearVelocity = currentVelocity;
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
            Collider2D groundCollider;

            if (groundCheck != null)
            {
                grounded = TryGetGroundContact(groundCheck.position, groundCheckRadius, out groundCollider);
            }
            else if (bodyCollider != null)
            {
                Bounds bounds = bodyCollider.bounds;
                Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - (groundCheckRadius * 0.5f));
                Vector2 boxSize = new Vector2(bounds.size.x * 0.85f, groundCheckRadius);
                grounded = TryGetGroundContact(boxCenter, boxSize, out groundCollider);
            }
            else
            {
                grounded = false;
                groundCollider = null;
            }

            IsGrounded = grounded;
            currentGroundPlatform = grounded && groundCollider != null
                ? groundCollider.GetComponentInParent<MovingPlatform2D>()
                : null;

            if (IsGrounded)
            {
                jumpsUsed = 0;
            }
        }

        private bool HasGroundContact(Vector2 center, float radius)
        {
            return TryGetGroundContact(center, radius, out _);
        }

        private bool TryGetGroundContact(Vector2 center, float radius, out Collider2D groundCollider)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;

            int hitCount = Physics2D.OverlapCircle(center, radius, filter, groundHits);
            return ContainsValidGroundHit(hitCount, out groundCollider);
        }

        private bool HasGroundContact(Vector2 center, Vector2 size)
        {
            return TryGetGroundContact(center, size, out _);
        }

        private bool TryGetGroundContact(Vector2 center, Vector2 size, out Collider2D groundCollider)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;

            int hitCount = Physics2D.OverlapBox(center, size, 0f, filter, groundHits);
            return ContainsValidGroundHit(hitCount, out groundCollider);
        }

        private bool ContainsValidGroundHit(int hitCount, out Collider2D validGround)
        {
            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = groundHits[index];
                groundHits[index] = null;

                if (!IsSelfCollider(hit))
                {
                    validGround = hit;
                    return true;
                }
            }

            validGround = null;
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

            Vector2 velocity = body.linearVelocity;
            float groundPlatformVelocityX = GetGroundPlatformVelocityX();
            float currentRelativeVelocityX = velocity.x - groundPlatformVelocityX;
            float targetRelativeVelocityX = moveInput.x * moveSpeed;
            float targetVelocityX = groundPlatformVelocityX + targetRelativeVelocityX;
            float acceleration = IsGrounded ? groundAcceleration : airAcceleration;
            float deceleration = IsGrounded ? groundDeceleration : airDeceleration;
            bool isStopping = Mathf.Abs(targetRelativeVelocityX) < 0.01f;
            bool isReversing = Mathf.Abs(currentRelativeVelocityX) > 0.01f
                && Mathf.Abs(targetRelativeVelocityX) > 0.01f
                && Mathf.Sign(targetRelativeVelocityX) != Mathf.Sign(currentRelativeVelocityX);
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

        private void ApplyGroundPlatformMotion()
        {
            // Horizontal platform carry is handled through matched player velocity in ApplyHorizontalMovement.
        }

        private bool TryStartLedgeAssist()
        {
            if (!enableLedgeAssist || body == null || bodyCollider == null || IsGrounded)
            {
                return false;
            }

            if (Mathf.Abs(moveInput.x) < ledgeAssistMinInput)
            {
                return false;
            }

            Vector2 velocity = body.linearVelocity;
            if (velocity.y > ledgeAssistMaxRiseSpeed || velocity.y < -ledgeAssistMaxFallSpeed)
            {
                return false;
            }

            float direction = Mathf.Sign(moveInput.x);
            if (!HasGroundContact(GetLedgeLowerProbe(direction), groundCheckRadius))
            {
                return false;
            }

            if (HasGroundContact(GetLedgeUpperProbe(direction), groundCheckRadius))
            {
                return false;
            }

            if (!TryCalculateLedgeAssistTarget(direction, out Vector2 targetPosition))
            {
                return false;
            }

            ledgeAssistActive = true;
            ledgeAssistTimer = ledgeAssistPauseDuration;
            ledgeAssistTargetPosition = targetPosition;
            ledgeAssistDirection = direction;
            jumpQueued = false;
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
            return true;
        }

        private void UpdateLedgeAssist(float deltaTime)
        {
            if (body == null)
            {
                CancelLedgeAssist();
                return;
            }

            // Hold for a beat so the near-miss reads before the player settles safely on top.
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
            ledgeAssistTimer -= deltaTime;

            if (ledgeAssistTimer > 0f)
            {
                return;
            }

            body.position = ledgeAssistTargetPosition;
            body.linearVelocity = new Vector2(ledgeAssistDirection * moveSpeed * 0.35f, 0f);
            body.gravityScale = defaultGravityScale;
            ledgeAssistActive = false;
            UpdateGroundedState();
        }

        private void CancelLedgeAssist()
        {
            ledgeAssistActive = false;
            ledgeAssistTimer = 0f;

            if (body != null)
            {
                body.gravityScale = defaultGravityScale;
            }
        }

        private Vector2 GetLedgeLowerProbe(float direction)
        {
            Bounds bounds = bodyCollider.bounds;
            return new Vector2(
                bounds.center.x + (direction * (bounds.extents.x + groundCheckRadius)),
                bounds.center.y);
        }

        private Vector2 GetLedgeUpperProbe(float direction)
        {
            Bounds bounds = bodyCollider.bounds;
            return new Vector2(
                bounds.center.x + (direction * (bounds.extents.x + groundCheckRadius)),
                bounds.max.y + groundCheckRadius);
        }

        private bool TryCalculateLedgeAssistTarget(float direction, out Vector2 targetPosition)
        {
            targetPosition = body.position;

            Vector2 centerOffset = (Vector2)bodyCollider.bounds.center - body.position;
            float targetCenterX = bodyCollider.bounds.center.x + (direction * ledgeAssistHorizontalDistance);
            float targetProbeX = groundCheck != null
                ? groundCheck.position.x + (direction * ledgeAssistHorizontalDistance)
                : targetCenterX;
            Vector2 searchOrigin = new Vector2(targetProbeX, bodyCollider.bounds.max.y + ledgeAssistTopSearchHeight);

            if (!TryGetGroundBelow(searchOrigin, ledgeAssistTopSearchHeight + bodyCollider.bounds.size.y, out RaycastHit2D groundHit))
            {
                return false;
            }

            float targetCenterY = groundHit.point.y + ledgeAssistStandClearance + bodyCollider.bounds.extents.y;
            targetPosition = new Vector2(targetCenterX - centerOffset.x, targetCenterY - centerOffset.y);
            return CanOccupyPosition(targetPosition);
        }

        private bool TryGetGroundBelow(Vector2 origin, float distance, out RaycastHit2D validHit)
        {
            ContactFilter2D filter = CreateGroundFilter();
            int hitCount = Physics2D.Raycast(origin, Vector2.down, filter, raycastHits, distance);

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = raycastHits[index];
                raycastHits[index] = default;

                if (IsSelfCollider(hit.collider))
                {
                    continue;
                }

                validHit = hit;
                return true;
            }

            validHit = default;
            return false;
        }

        private bool CanOccupyPosition(Vector2 targetPosition)
        {
            ContactFilter2D filter = CreateGroundFilter();
            Vector2 positionOffset = targetPosition - body.position;
            Bounds bounds = bodyCollider.bounds;
            Vector2 overlapCenter = (Vector2)bounds.center + positionOffset;
            Vector2 overlapSize = bounds.size * 0.9f;
            int hitCount = Physics2D.OverlapBox(overlapCenter, overlapSize, 0f, filter, overlapHits);

            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = overlapHits[index];
                overlapHits[index] = null;

                if (!IsSelfCollider(hit))
                {
                    return false;
                }
            }

            return true;
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

            if (stopHorizontalMotionOnLanding && body != null && Mathf.Abs(moveInput.x) <= landingStopInputThreshold)
            {
                Vector2 velocity = body.linearVelocity;
                velocity.x = GetGroundPlatformVelocityX();
                body.linearVelocity = velocity;
            }

            if (lastVerticalVelocity <= -landingDustMinSpeed)
            {
                SpawnDustBurst(1f, 0.15f, 0.28f);
            }
        }

        private void StabilizeIdleOnMovingPlatform()
        {
            if (!stabilizeIdleOnMovingPlatform || body == null)
            {
                return;
            }

            if (!IsGrounded || currentGroundPlatform == null || Mathf.Abs(moveInput.x) > landingStopInputThreshold)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = currentGroundPlatform.CurrentVelocity.x;
            body.linearVelocity = velocity;
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

            return Mathf.Abs(GetVisualHorizontalVelocity()) > 0.15f
                ? PlayerVisualState.Run
                : PlayerVisualState.Idle;
        }

        private float GetVisualHorizontalVelocity()
        {
            if (body == null)
            {
                return moveInput.x;
            }

            return body.linearVelocity.x - GetGroundPlatformVelocityX();
        }

        private float GetGroundPlatformVelocityX()
        {
            return IsGrounded && currentGroundPlatform != null
                ? currentGroundPlatform.CurrentVelocity.x
                : 0f;
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

        private ContactFilter2D CreateGroundFilter()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            return filter;
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
