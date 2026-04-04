using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VibeCode.Platformer;

namespace VibeCode.Tests.EditMode
{
    public class GravityGardenSliceEditModeTests
    {
        [Test]
        public void MovingPlatformPrefabExists()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/MovingPlatform.prefab");

            Assert.That(prefab, Is.Not.Null, "Expected a reusable moving platform prefab to exist.");
            Assert.That(prefab.GetComponent<MovingPlatform2D>(), Is.Not.Null,
                "Expected the moving platform prefab to include the runtime movement component.");
            Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null,
                "Expected the moving platform prefab to include a rigidbody for kinematic movement.");
        }

        [Test]
        public void CyclingSpikeHazardPrefabExists()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/CyclingSpikeHazard.prefab");

            Assert.That(prefab, Is.Not.Null, "Expected a reusable timed hazard prefab to exist.");
            Assert.That(prefab.GetComponent<CyclingSpikeHazard2D>(), Is.Not.Null,
                "Expected the timed hazard prefab to include the runtime cycling hazard component.");
            Assert.That(prefab.GetComponent<Collider2D>(), Is.Not.Null,
                "Expected the timed hazard prefab to include a trigger collider for player defeat.");
        }

        [Test]
        public void PlaceholderSpriteAssetResolvesToSpriteSubAsset()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Sprites/PlaceholderSquare.png");
            Sprite sprite = null;

            for (int index = 0; index < assets.Length; index++)
            {
                sprite = assets[index] as Sprite;
                if (sprite != null)
                {
                    break;
                }
            }

            Assert.That(sprite, Is.Not.Null, "Expected the placeholder art texture to expose a Sprite sub-asset for runtime visuals.");
        }
    }
}
