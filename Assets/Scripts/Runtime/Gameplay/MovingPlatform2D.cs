using System.Collections.Generic;
using UnityEngine;

namespace VibeCode.Platformer
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class MovingPlatform2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D platformCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform pathRoot;

        [Header("Shape")]
        [SerializeField] private Vector2 platformSize = new Vector2(2.4f, 0.3f);

        [Header("Motion")]
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float waitDurationAtPoint = 0.2f;
        [SerializeField] private float pointReachDistance = 0.03f;
        [SerializeField] private bool pingPong = true;

        [Header("Riding")]
        [SerializeField] private float riderTopTolerance = 0.08f;

        private readonly List<Transform> riders = new List<Transform>();
        private Vector2[] worldWaypoints = System.Array.Empty<Vector2>();
        private int targetWaypointIndex = 1;
        private int travelDirection = 1;
        private float waitTimer;

        public Vector2 CurrentDelta { get; private set; }
        public Vector2 CurrentVelocity { get; private set; }
        public int WaypointCount => worldWaypoints.Length;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            platformCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            pathRoot = transform.Find("Path");

            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            ApplyPlatformShape();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (platformCollider == null)
            {
                platformCollider = GetComponent<Collider2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (pathRoot == null)
            {
                pathRoot = transform.Find("Path");
            }

            ApplyPlatformShape();
            CacheWaypoints();

            if (body != null && worldWaypoints.Length > 0)
            {
                body.position = worldWaypoints[0];
            }
        }

        private void OnEnable()
        {
            CacheWaypoints();
            waitTimer = 0f;
            travelDirection = 1;
            targetWaypointIndex = worldWaypoints.Length > 1 ? 1 : 0;
            CurrentDelta = Vector2.zero;
            CurrentVelocity = Vector2.zero;

            if (body != null && worldWaypoints.Length > 0)
            {
                body.position = worldWaypoints[0];
            }
        }

        private void OnDisable()
        {
            DetachAllRiders();
            CurrentDelta = Vector2.zero;
            CurrentVelocity = Vector2.zero;
        }

        private void OnDestroy()
        {
            DetachAllRiders();
        }

        private void OnValidate()
        {
            platformSize.x = Mathf.Max(0.4f, platformSize.x);
            platformSize.y = Mathf.Max(0.1f, platformSize.y);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            waitDurationAtPoint = Mathf.Max(0f, waitDurationAtPoint);
            pointReachDistance = Mathf.Max(0.005f, pointReachDistance);
            riderTopTolerance = Mathf.Max(0.01f, riderTopTolerance);
        }

        private void FixedUpdate()
        {
            if (body == null || worldWaypoints.Length < 2)
            {
                CurrentDelta = Vector2.zero;
                CurrentVelocity = Vector2.zero;
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer = Mathf.Max(0f, waitTimer - Time.fixedDeltaTime);
                CurrentDelta = Vector2.zero;
                CurrentVelocity = Vector2.zero;
                return;
            }

            Vector2 currentPosition = body.position;
            Vector2 targetPosition = worldWaypoints[targetWaypointIndex];
            Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime);
            CurrentDelta = nextPosition - currentPosition;
            CurrentVelocity = CurrentDelta / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            body.MovePosition(nextPosition);

            if (Vector2.Distance(nextPosition, targetPosition) > pointReachDistance)
            {
                return;
            }

            body.position = targetPosition;
            AdvanceWaypoint();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            UpdateRiderState(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            UpdateRiderState(collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            DetachRider(ResolvePlayerTransform(collision));
        }

        public bool HasRider(PlayerController2D player)
        {
            return player != null && riders.Contains(player.transform);
        }

        private void ApplyPlatformShape()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Sliced;
                spriteRenderer.size = platformSize;
            }

            if (platformCollider is BoxCollider2D boxCollider)
            {
                boxCollider.size = platformSize;
                boxCollider.offset = Vector2.zero;
                boxCollider.isTrigger = false;
            }
        }

        private void CacheWaypoints()
        {
            if (pathRoot == null)
            {
                worldWaypoints = System.Array.Empty<Vector2>();
                return;
            }

            int childCount = pathRoot.childCount;
            if (childCount == 0)
            {
                worldWaypoints = System.Array.Empty<Vector2>();
                return;
            }

            worldWaypoints = new Vector2[childCount];
            for (int index = 0; index < childCount; index++)
            {
                worldWaypoints[index] = pathRoot.GetChild(index).position;
            }
        }

        private void AdvanceWaypoint()
        {
            waitTimer = waitDurationAtPoint;

            if (worldWaypoints.Length < 2)
            {
                targetWaypointIndex = 0;
                return;
            }

            if (pingPong)
            {
                if (targetWaypointIndex >= worldWaypoints.Length - 1)
                {
                    travelDirection = -1;
                }
                else if (targetWaypointIndex <= 0)
                {
                    travelDirection = 1;
                }

                targetWaypointIndex = Mathf.Clamp(targetWaypointIndex + travelDirection, 0, worldWaypoints.Length - 1);
                return;
            }

            targetWaypointIndex = (targetWaypointIndex + 1) % worldWaypoints.Length;
        }

        private void UpdateRiderState(Collision2D collision)
        {
            Transform rider = ResolvePlayerTransform(collision);
            if (rider == null)
            {
                return;
            }

            if (IsTopCollision(collision))
            {
                AttachRider(rider);
                return;
            }

            DetachRider(rider);
        }

        private Transform ResolvePlayerTransform(Collision2D collision)
        {
            if (collision == null)
            {
                return null;
            }

            PlayerController2D player = collision.collider.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
                player = collision.otherCollider.GetComponentInParent<PlayerController2D>();
            }

            return player != null ? player.transform : null;
        }

        private bool IsTopCollision(Collision2D collision)
        {
            if (platformCollider == null || collision == null)
            {
                return false;
            }

            Bounds platformBounds = platformCollider.bounds;
            for (int index = 0; index < collision.contactCount; index++)
            {
                ContactPoint2D contact = collision.GetContact(index);
                if (contact.point.y >= platformBounds.max.y - riderTopTolerance)
                {
                    return true;
                }
            }

            Collider2D playerCollider = collision.otherCollider != null ? collision.otherCollider : collision.collider;
            Bounds playerBounds = playerCollider != null ? playerCollider.bounds : default;
            return playerBounds.min.y >= platformBounds.max.y - riderTopTolerance;
        }

        private void AttachRider(Transform rider)
        {
            if (rider == null || riders.Contains(rider))
            {
                return;
            }

            riders.Add(rider);
        }

        private void DetachRider(Transform rider)
        {
            if (rider == null)
            {
                return;
            }

            riders.Remove(rider);
        }

        private void DetachAllRiders()
        {
            riders.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (pathRoot == null)
            {
                pathRoot = transform.Find("Path");
            }

            if (pathRoot == null || pathRoot.childCount == 0)
            {
                return;
            }

            Gizmos.color = new Color(0.44f, 0.9f, 0.96f, 1f);

            Transform previous = null;
            for (int index = 0; index < pathRoot.childCount; index++)
            {
                Transform point = pathRoot.GetChild(index);
                if (point == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(point.position, 0.1f);
                if (previous != null)
                {
                    Gizmos.DrawLine(previous.position, point.position);
                }

                previous = point;
            }
        }
    }
}
