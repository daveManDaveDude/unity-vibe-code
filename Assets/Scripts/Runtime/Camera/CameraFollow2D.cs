using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private bool followHorizontally = true;
        [SerializeField] private bool followVertically = true;

        private Vector3 velocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = transform.position;

            if (followHorizontally)
            {
                desiredPosition.x = target.position.x + offset.x;
            }

            if (followVertically)
            {
                desiredPosition.y = target.position.y + offset.y;
            }

            desiredPosition.z = offset.z;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }
    }
}
