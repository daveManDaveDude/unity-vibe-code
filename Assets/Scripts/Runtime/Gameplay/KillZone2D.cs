using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class KillZone2D : MonoBehaviour
    {
        [SerializeField] private GravityGardenGameManager gameManager;
        [SerializeField] private Collider2D triggerCollider;

        private void Reset()
        {
            triggerCollider = GetComponent<Collider2D>();

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

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }
        }

        private void OnValidate()
        {
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }

            gameManager?.RespawnPlayer(player);
        }
    }
}
