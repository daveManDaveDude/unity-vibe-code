using UnityEngine;

namespace VibeCode.Platformer
{
    public static class DesktopWindowSizing
    {
        public const float DefaultScale = 0.7f;

        public static Vector2Int CalculateWindowSize(int screenWidth, int screenHeight, float scale = DefaultScale)
        {
            float clampedScale = Mathf.Clamp01(scale);
            int width = Mathf.Max(1, Mathf.RoundToInt(screenWidth * clampedScale));
            int height = Mathf.Max(1, Mathf.RoundToInt(screenHeight * clampedScale));
            return new Vector2Int(width, height);
        }
    }

    public static class DesktopWindowBootstrap
    {
#if UNITY_STANDALONE && !UNITY_EDITOR
        // Apply the launch window size before the splash screen so desktop builds start at a friendlier scale.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void ApplyInitialWindowSize()
        {
            Resolution currentResolution = Screen.currentResolution;
            Vector2Int targetSize = DesktopWindowSizing.CalculateWindowSize(
                currentResolution.width,
                currentResolution.height);

            Screen.SetResolution(targetSize.x, targetSize.y, FullScreenMode.Windowed);
        }
#endif
    }
}
