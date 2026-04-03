using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlaceholderBurstEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector2 velocity;
        private Vector3 startScale;
        private Vector3 targetScale;
        private Color startColor;
        private Color targetColor;
        private float lifetime = 0.2f;
        private float age;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(
            Sprite sprite,
            Color color,
            int sortingOrder,
            Vector3 initialScale,
            Vector3 endScale,
            Vector2 initialVelocity,
            float duration)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            startScale = initialScale;
            targetScale = endScale;
            velocity = initialVelocity;
            lifetime = Mathf.Max(0.05f, duration);
            startColor = color;
            targetColor = new Color(color.r, color.g, color.b, 0f);

            transform.localScale = initialScale;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);

            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity.x = Mathf.Lerp(velocity.x, 0f, 10f * Time.deltaTime);
            velocity.y = Mathf.Lerp(velocity.y, -0.12f, 8f * Time.deltaTime);

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
