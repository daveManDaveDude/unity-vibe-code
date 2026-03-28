using UnityEngine;
using UnityEngine.InputSystem;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CapsuleCollider2D bodyCollider;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float groundAcceleration = 60f;
        [SerializeField] private float airAcceleration = 35f;
        [SerializeField] private float jumpVelocity = 14f;

        [Header("Jump Feel")]
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField] private float fallGravityMultiplier = 2.8f;
        [SerializeField] private float lowJumpGravityMultiplier = 2f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField] private float groundCheckRadius = 0.12f;

        private InputAction moveAction;
        private InputAction jumpAction;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;
        private float defaultGravityScale;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressedTime = float.NegativeInfinity;

        public bool IsGrounded { get; private set; }

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
                bodyCollider.size = new Vector2(0.9f, 1.8f);
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
                lastJumpPressedTime = Time.time;
            }
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();
            ApplyHorizontalMovement(Time.fixedDeltaTime);
            TryJump();
            ApplyGravityTuning();
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
            BindActions();
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
                grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayers) != null;
            }
            else if (bodyCollider != null)
            {
                Bounds bounds = bodyCollider.bounds;
                Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - (groundCheckRadius * 0.5f));
                Vector2 boxSize = new Vector2(bounds.size.x * 0.85f, groundCheckRadius);
                grounded = Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayers) != null;
            }
            else
            {
                grounded = false;
            }

            IsGrounded = grounded;
            if (grounded)
            {
                lastGroundedTime = Time.time;
            }
        }

        private void ApplyHorizontalMovement(float deltaTime)
        {
            if (body == null)
            {
                return;
            }

            float targetVelocityX = moveInput.x * moveSpeed;
            float acceleration = IsGrounded ? groundAcceleration : airAcceleration;
            Vector2 velocity = body.velocity;
            velocity.x = Mathf.MoveTowards(velocity.x, targetVelocityX, acceleration * deltaTime);
            body.velocity = velocity;

            if (spriteRenderer != null && Mathf.Abs(moveInput.x) > 0.01f)
            {
                spriteRenderer.flipX = moveInput.x < 0f;
            }
        }

        private void TryJump()
        {
            if (body == null)
            {
                return;
            }

            bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
            bool canUseGroundedJump = IsGrounded || Time.time - lastGroundedTime <= coyoteTime;
            if (!hasBufferedJump || !canUseGroundedJump)
            {
                return;
            }

            Vector2 velocity = body.velocity;
            velocity.y = jumpVelocity;
            body.velocity = velocity;

            IsGrounded = false;
            lastGroundedTime = float.NegativeInfinity;
            lastJumpPressedTime = float.NegativeInfinity;
        }

        private void ApplyGravityTuning()
        {
            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.velocity;
            bool jumpHeld = jumpAction != null && jumpAction.IsPressed();
            float gravityMultiplier = 1f;

            if (velocity.y < 0f)
            {
                gravityMultiplier = fallGravityMultiplier;
            }
            else if (velocity.y > 0f && !jumpHeld)
            {
                gravityMultiplier = lowJumpGravityMultiplier;
            }

            body.gravityScale = defaultGravityScale * gravityMultiplier;

            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
                body.velocity = velocity;
            }
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
