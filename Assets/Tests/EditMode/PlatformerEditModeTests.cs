using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VibeCode.Platformer;

namespace VibeCode.Tests.EditMode
{
    public class PlatformerEditModeTests
    {
        [Test]
        public void AddingPlayerControllerAddsRequiredPhysicsComponents()
        {
            var player = new GameObject("Player Under Test");

            try
            {
                PlayerController2D controller = player.AddComponent<PlayerController2D>();

                Assert.That(controller, Is.Not.Null);
                Assert.That(player.GetComponent<Rigidbody2D>(), Is.Not.Null);
                Assert.That(player.GetComponent<CapsuleCollider2D>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void MainSceneAssetExists()
        {
            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main.unity");

            Assert.That(mainScene, Is.Not.Null, "Expected Assets/Scenes/Main.unity to exist.");
        }

        [Test]
        public void MainSceneIsEnabledInBuildSettings()
        {
            bool mainSceneIsEnabled = EditorBuildSettings.scenes
                .Any(scene => scene.enabled && scene.path == "Assets/Scenes/Main.unity");

            Assert.That(mainSceneIsEnabled, Is.True, "Expected Main.unity to be enabled in Build Settings.");
        }

        [Test]
        public void DesktopWindowSizingUsesSeventyPercentOfScreenResolution()
        {
            Vector2Int target = DesktopWindowSizing.CalculateWindowSize(1920, 1080);

            Assert.That(target, Is.EqualTo(new Vector2Int(1344, 756)));
        }
    }
}
