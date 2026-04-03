using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Enemy2D))]
    public class PatrollingEnemy2D : MonoBehaviour
    {
        private const string EdgeCheckName = "Edge Check";
        private const string WallCheckName = "Wall Check";

        [Header("References")]
        [SerializeField] private Enemy2D enemy;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Transform edgeCheck;
        [SerializeField] private Transform wallCheck;

        [Header("Patrol")]
        [SerializeField] private LayerMask blockerLayers = 1;
        [SerializeField] private float moveSpeed = 1.6f;
        [SerializeField] private float edgeCheckRadius = 0.05f;
        [SerializeField] private float wallCheckRadius = 0.05f;
        [SerializeField] private bool startFacingRight = false;

        private float facingDirection = -1f;

        private void Reset()
        {
            enemy = GetComponent<Enemy2D>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            edgeCheck = EnsureSensor(EdgeCheckName, new Vector3(0.28f, -0.2f, 0f));
            wallCheck = EnsureSensor(WallCheckName, new Vector3(0.34f, -0.01f, 0f));
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

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider2D>();
            }

            if (edgeCheck == null)
            {
                edgeCheck = transform.Find(EdgeCheckName);
            }

            if (wallCheck == null)
            {
                wallCheck = transform.Find(WallCheckName);
            }
        }

        private void OnEnable()
        {
            facingDirection = startFacingRight ? 1f : -1f;
            ApplyFacing();
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            edgeCheckRadius = Mathf.Max(0.01f, edgeCheckRadius);
            wallCheckRadius = Mathf.Max(0.01f, wallCheckRadius);
        }

        private void FixedUpdate()
        {
            if (enemy != null && enemy.IsDefeated)
            {
                return;
            }

            if (body == null)
            {
                return;
            }

            if (ShouldTurnAround())
            {
                facingDirection *= -1f;
                ApplyFacing();
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = moveSpeed * facingDirection;
            body.linearVelocity = velocity;
        }

        private bool ShouldTurnAround()
        {
            if (edgeCheck == null || wallCheck == null)
            {
                return false;
            }

            bool hasGroundAhead = FindBlockingCollider(GetMirroredSensorPosition(edgeCheck), edgeCheckRadius) != null;
            bool wallAhead = FindBlockingCollider(GetMirroredSensorPosition(wallCheck), wallCheckRadius) != null;
            return !hasGroundAhead || wallAhead;
        }

        private Collider2D FindBlockingCollider(Vector2 position, float radius)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, blockerLayers);
            for (int index = 0; index < hits.Length; index++)
            {
                Collider2D hit = hits[index];
                if (hit == null || hit.isTrigger)
                {
                    continue;
                }

                if (bodyCollider != null && hit == bodyCollider)
                {
                    continue;
                }

                if (hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.GetComponentInParent<PlayerController2D>() != null)
                {
                    continue;
                }

                return hit;
            }

            return null;
        }

        private void ApplyFacing()
        {
            enemy?.SetFacingDirection(facingDirection);
        }

        private Vector2 GetMirroredSensorPosition(Transform sensor)
        {
            Vector3 localPosition = sensor.localPosition;
            localPosition.x = Mathf.Abs(localPosition.x) * facingDirection;
            return transform.TransformPoint(localPosition);
        }

        private Transform EnsureSensor(string sensorName, Vector3 localPosition)
        {
            Transform sensor = transform.Find(sensorName);
            if (sensor == null)
            {
                GameObject sensorObject = new GameObject(sensorName);
                sensor = sensorObject.transform;
                sensor.SetParent(transform, false);
            }

            sensor.localPosition = localPosition;
            return sensor;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.25f, 1f);

            if (edgeCheck != null)
            {
                Gizmos.DrawWireSphere(GetMirroredSensorPosition(edgeCheck), edgeCheckRadius);
            }

            if (wallCheck != null)
            {
                Gizmos.DrawWireSphere(GetMirroredSensorPosition(wallCheck), wallCheckRadius);
            }
        }
    }
}
