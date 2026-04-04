using UnityEngine;

namespace VibeCode.Platformer
{
    [DisallowMultipleComponent]
    public class GravityGardenHud : MonoBehaviour
    {
        [SerializeField] private GravityGardenGameManager gameManager;
        [SerializeField] private string gameTitle = "Gravity Garden";
        [SerializeField] private float margin = 16f;
        [SerializeField] private int titleFontSize = 28;
        [SerializeField] private int bodyFontSize = 15;
        [SerializeField] private int winFontSize = 24;
        [SerializeField] private Color titleColor = new Color(0.9f, 0.97f, 0.93f, 1f);
        [SerializeField] private Color bodyColor = new Color(0.9f, 0.96f, 1f, 1f);
        [SerializeField] private Color accentColor = new Color(0.66f, 0.96f, 0.75f, 1f);

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle objectiveStyle;
        private GUIStyle statusStyle;
        private GUIStyle winStyle;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }
        }

        private void OnGUI()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GravityGardenGameManager>();
            }

            if (gameManager == null)
            {
                return;
            }

            EnsureStyles();

            GUI.Label(new Rect(0f, margin, Screen.width, titleFontSize + 8f), gameTitle, titleStyle);

            Rect seedPanel = new Rect(margin, margin + 44f, 280f, 68f);
            GUI.Box(seedPanel, GUIContent.none);

            float textX = seedPanel.x + 12f;
            GUI.Label(
                new Rect(textX, seedPanel.y + 8f, seedPanel.width - 24f, bodyFontSize + 8f),
                $"Seeds: {gameManager.CollectedSeeds}/{gameManager.TotalSeedsInLevel}",
                bodyStyle);

            string objectiveText = gameManager.HasWon
                ? "The exit is complete. Slice clear."
                : gameManager.CanUseExit
                    ? "Portal ready. No key needed."
                    : $"No key in this slice. Need {gameManager.SeedsRemainingForExit} more seed{(gameManager.SeedsRemainingForExit == 1 ? string.Empty : "s")} to open the exit.";

            GUI.Label(
                new Rect(textX, seedPanel.y + 32f, seedPanel.width - 24f, bodyFontSize + 8f),
                objectiveText,
                objectiveStyle);

            string statusText = gameManager.HasWon ? "Garden Restored!" : gameManager.CurrentStatusMessage;
            if (string.IsNullOrEmpty(statusText))
            {
                return;
            }

            Rect statusPanel = new Rect((Screen.width * 0.5f) - 190f, Screen.height - 118f, 380f, 56f);
            GUI.Box(statusPanel, GUIContent.none);
            GUI.Label(
                new Rect(statusPanel.x + 12f, statusPanel.y + 10f, statusPanel.width - 24f, statusPanel.height - 20f),
                statusText,
                gameManager.HasWon ? winStyle : statusStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.UpperCenter;
            titleStyle.fontSize = titleFontSize;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = titleColor;

            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.alignment = TextAnchor.UpperLeft;
            bodyStyle.fontSize = bodyFontSize;
            bodyStyle.fontStyle = FontStyle.Bold;
            bodyStyle.normal.textColor = bodyColor;

            objectiveStyle = new GUIStyle(bodyStyle);
            objectiveStyle.fontStyle = FontStyle.Normal;
            objectiveStyle.normal.textColor = accentColor;

            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.alignment = TextAnchor.MiddleCenter;
            statusStyle.fontSize = bodyFontSize;
            statusStyle.fontStyle = FontStyle.Bold;
            statusStyle.wordWrap = true;
            statusStyle.normal.textColor = bodyColor;

            winStyle = new GUIStyle(statusStyle);
            winStyle.fontSize = winFontSize;
            winStyle.normal.textColor = accentColor;
        }
    }
}
