using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CyclingSpikeHazard2D : MonoBehaviour
    {
        public enum HazardState
        {
            Safe,
            Warning,
            Danger
        }

        [Header("References")]
        [SerializeField] private GravityGardenGameManager gameManager;
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private Transform hazardVisualRoot;
        [SerializeField] private SpriteRenderer indicatorRenderer;

        [Header("Cycle")]
        [SerializeField] private float safeDuration = 1.15f;
        [SerializeField] private float warningDuration = 0.45f;
        [SerializeField] private float dangerDuration = 0.9f;
        [SerializeField] private bool startDangerous;

        [Header("Visuals")]
        [SerializeField] private Color safeColor = new Color(0.46f, 0.85f, 0.6f, 1f);
        [SerializeField] private Color warningColor = new Color(0.96f, 0.82f, 0.3f, 1f);
        [SerializeField] private Color dangerColor = new Color(0.95f, 0.38f, 0.31f, 1f);
        [SerializeField] private Vector3 safeVisualScale = new Vector3(1f, 0.25f, 1f);
        [SerializeField] private Vector3 warningVisualScale = new Vector3(1f, 0.5f, 1f);
        [SerializeField] private Vector3 dangerVisualScale = Vector3.one;
        [SerializeField] private float visualLerpSpeed = 12f;

        [Header("Player Interaction")]
        [SerializeField] private string damagePlayerMessage = "The thorn patch poked you.";
        [SerializeField] private string defeatPlayerMessage = "Too many thorn pokes. Back to safety.";

        private SpriteRenderer[] hazardRenderers = System.Array.Empty<SpriteRenderer>();
        private readonly Collider2D[] overlapHits = new Collider2D[4];
        private HazardState currentState;
        private float stateTimer;

        public HazardState CurrentState => currentState;
        public bool IsDangerous => currentState == HazardState.Danger;
        public float SafeDuration => safeDuration;
        public float WarningDuration => warningDuration;
        public float DangerDuration => dangerDuration;

        private void Reset()
        {
            triggerCollider = GetComponent<Collider2D>();
            hazardVisualRoot = transform.Find("Visuals");
            Transform indicator = transform.Find("Timing Light");
            indicatorRenderer = indicator != null ? indicator.GetComponent<SpriteRenderer>() : null;

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

            if (hazardVisualRoot == null)
            {
                hazardVisualRoot = transform.Find("Visuals");
            }

            if (indicatorRenderer == null)
            {
                Transform indicator = transform.Find("Timing Light");
                indicatorRenderer = indicator != null ? indicator.GetComponent<SpriteRenderer>() : null;
            }

            CacheHazardRenderers();
            ResolveGameManager();
        }

        private void OnEnable()
        {
            SetState(startDangerous ? HazardState.Danger : HazardState.Safe);
        }

        private void OnValidate()
        {
            safeDuration = Mathf.Max(0.1f, safeDuration);
            warningDuration = Mathf.Max(0.05f, warningDuration);
            dangerDuration = Mathf.Max(0.1f, dangerDuration);
            visualLerpSpeed = Mathf.Max(0f, visualLerpSpeed);

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                AdvanceState();
            }

            UpdateVisuals(Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsDangerous)
            {
                return;
            }

            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            ResolveGameManager();
            gameManager?.DamagePlayer(player, damagePlayerMessage, defeatPlayerMessage, transform.position);
        }

        private void CacheHazardRenderers()
        {
            hazardRenderers = hazardVisualRoot != null
                ? hazardVisualRoot.GetComponentsInChildren<SpriteRenderer>(true)
                : System.Array.Empty<SpriteRenderer>();
        }

        private void ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }
        }

        private void AdvanceState()
        {
            switch (currentState)
            {
                case HazardState.Safe:
                    SetState(HazardState.Warning);
                    break;
                case HazardState.Warning:
                    SetState(HazardState.Danger);
                    break;
                default:
                    SetState(HazardState.Safe);
                    break;
            }
        }

        private void SetState(HazardState nextState)
        {
            currentState = nextState;
            stateTimer = GetDurationFor(nextState);

            if (triggerCollider != null)
            {
                triggerCollider.enabled = nextState == HazardState.Danger;
            }

            if (nextState == HazardState.Danger)
            {
                DefeatAnyOverlappingPlayer();
            }

            ApplyColors(GetStateColor(nextState));

            if (hazardVisualRoot != null)
            {
                hazardVisualRoot.localScale = GetStateScale(nextState);
            }
        }

        private float GetDurationFor(HazardState state)
        {
            switch (state)
            {
                case HazardState.Warning:
                    return warningDuration;
                case HazardState.Danger:
                    return dangerDuration;
                default:
                    return safeDuration;
            }
        }

        private Color GetStateColor(HazardState state)
        {
            switch (state)
            {
                case HazardState.Warning:
                    return warningColor;
                case HazardState.Danger:
                    return dangerColor;
                default:
                    return safeColor;
            }
        }

        private Vector3 GetStateScale(HazardState state)
        {
            switch (state)
            {
                case HazardState.Warning:
                    return warningVisualScale;
                case HazardState.Danger:
                    return dangerVisualScale;
                default:
                    return safeVisualScale;
            }
        }

        private void ApplyColors(Color color)
        {
            for (int index = 0; index < hazardRenderers.Length; index++)
            {
                SpriteRenderer renderer = hazardRenderers[index];
                if (renderer != null)
                {
                    renderer.color = color;
                }
            }

            if (indicatorRenderer != null)
            {
                indicatorRenderer.color = color;
            }
        }

        private void UpdateVisuals(float deltaTime)
        {
            if (hazardVisualRoot == null)
            {
                return;
            }

            Vector3 targetScale = GetStateScale(currentState);
            float lerpFactor = 1f - Mathf.Exp(-visualLerpSpeed * deltaTime);
            hazardVisualRoot.localScale = Vector3.Lerp(hazardVisualRoot.localScale, targetScale, lerpFactor);
        }

        private void DefeatAnyOverlappingPlayer()
        {
            if (triggerCollider == null)
            {
                return;
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            int hitCount = triggerCollider.Overlap(filter, overlapHits);

            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = overlapHits[index];
                overlapHits[index] = null;

                if (hit == null)
                {
                    continue;
                }

                PlayerController2D player = hit.GetComponentInParent<PlayerController2D>();
                if (player == null)
                {
                    continue;
                }

                ResolveGameManager();
                gameManager?.DamagePlayer(player, damagePlayerMessage, defeatPlayerMessage, transform.position);
                return;
            }
        }
    }
}
