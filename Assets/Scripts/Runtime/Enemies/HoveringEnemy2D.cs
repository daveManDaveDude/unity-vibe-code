using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Enemy2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class HoveringEnemy2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Enemy2D enemy;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Transform pathRoot;

        [Header("Motion")]
        [SerializeField] private float moveSpeed = 1.2f;
        [SerializeField] private float waitDurationAtPoint = 0.45f;
        [SerializeField] private float pointReachDistance = 0.03f;
        [SerializeField] private bool pingPong = true;

        private Vector2[] worldWaypoints = System.Array.Empty<Vector2>();
        private int targetWaypointIndex = 1;
        private int travelDirection = 1;
        private float waitTimer;

        public Vector2 CurrentVelocity { get; private set; }

        private void Reset()
        {
            enemy = GetComponent<Enemy2D>();
            body = GetComponent<Rigidbody2D>();
            pathRoot = EnsurePathRoot();

            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void Awake()
        {
            if (enemy == null)
            {
                enemy = GetComponent<Enemy2D>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (pathRoot == null)
            {
                pathRoot = transform.Find("Path");
            }

            CacheWaypoints();
            SnapToFirstWaypoint();
        }

        private void OnEnable()
        {
            CacheWaypoints();
            waitTimer = 0f;
            travelDirection = 1;
            targetWaypointIndex = worldWaypoints.Length > 1 ? 1 : 0;
            CurrentVelocity = Vector2.zero;
            SnapToFirstWaypoint();
        }

        private void OnDisable()
        {
            CurrentVelocity = Vector2.zero;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            waitDurationAtPoint = Mathf.Max(0f, waitDurationAtPoint);
            pointReachDistance = Mathf.Max(0.005f, pointReachDistance);

            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.freezeRotation = true;
            }
        }

        private void FixedUpdate()
        {
            if (enemy != null && enemy.IsDefeated)
            {
                CurrentVelocity = Vector2.zero;
                return;
            }

            if (body == null || worldWaypoints.Length < 2)
            {
                CurrentVelocity = Vector2.zero;
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer = Mathf.Max(0f, waitTimer - Time.fixedDeltaTime);
                CurrentVelocity = Vector2.zero;
                return;
            }

            Vector2 currentPosition = body.position;
            Vector2 targetPosition = worldWaypoints[targetWaypointIndex];
            Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime);
            CurrentVelocity = (nextPosition - currentPosition) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            body.MovePosition(nextPosition);

            if (Mathf.Abs(CurrentVelocity.x) > 0.01f)
            {
                enemy?.SetFacingDirection(CurrentVelocity.x);
            }

            if (Vector2.Distance(nextPosition, targetPosition) > pointReachDistance)
            {
                return;
            }

            body.position = targetPosition;
            CurrentVelocity = Vector2.zero;
            AdvanceWaypoint();
        }

        private void CacheWaypoints()
        {
            if (pathRoot == null || pathRoot.childCount == 0)
            {
                worldWaypoints = System.Array.Empty<Vector2>();
                return;
            }

            worldWaypoints = new Vector2[pathRoot.childCount];
            for (int index = 0; index < pathRoot.childCount; index++)
            {
                worldWaypoints[index] = pathRoot.GetChild(index).position;
            }
        }

        private void SnapToFirstWaypoint()
        {
            if (body == null || worldWaypoints.Length == 0)
            {
                return;
            }

            body.position = worldWaypoints[0];
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

        private Transform EnsurePathRoot()
        {
            Transform existingPath = transform.Find("Path");
            if (existingPath != null)
            {
                return existingPath;
            }

            GameObject pathObject = new GameObject("Path");
            pathObject.transform.SetParent(transform, false);

            GameObject pointA = new GameObject("Point A");
            pointA.transform.SetParent(pathObject.transform, false);
            pointA.transform.localPosition = Vector3.zero;

            GameObject pointB = new GameObject("Point B");
            pointB.transform.SetParent(pathObject.transform, false);
            pointB.transform.localPosition = new Vector3(2.2f, 0f, 0f);

            return pathObject.transform;
        }

        private void OnDrawGizmosSelected()
        {
            if (pathRoot == null || pathRoot.childCount < 2)
            {
                return;
            }

            Gizmos.color = new Color(0.56f, 0.92f, 0.88f, 1f);

            for (int index = 0; index < pathRoot.childCount; index++)
            {
                Transform point = pathRoot.GetChild(index);
                Gizmos.DrawWireSphere(point.position, 0.07f);

                if (index + 1 < pathRoot.childCount)
                {
                    Gizmos.DrawLine(point.position, pathRoot.GetChild(index + 1).position);
                }
            }
        }
    }
}
