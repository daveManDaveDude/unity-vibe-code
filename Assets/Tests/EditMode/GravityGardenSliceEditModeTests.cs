using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [Test]
        public void MainSceneContainsPatrollingEnemyEncounter()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid() && scene.isLoaded, Is.True, "Expected the Main scene to open for slice validation.");

            PatrollingEnemy2D patrollingEnemy = Object.FindAnyObjectByType<PatrollingEnemy2D>();

            Assert.That(patrollingEnemy, Is.Not.Null, "Expected the Main scene to contain the patrolling enemy encounter.");
            Assert.That(patrollingEnemy.GetComponent<Enemy2D>(), Is.Not.Null,
                "Expected the scene enemy encounter to use the shared enemy interaction component.");
            Assert.That(patrollingEnemy.GetComponent<Collider2D>(), Is.Not.Null,
                "Expected the scene enemy encounter to keep a collider for patrol and player defeat.");
            Assert.That(patrollingEnemy.GetComponentsInChildren<SpriteRenderer>(true), Is.Not.Empty,
                "Expected the patrolling enemy encounter to expose visible placeholder art.");
        }

        [Test]
        public void MainSceneContainsHoveringEnemyEncounter()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid() && scene.isLoaded, Is.True, "Expected the Main scene to open for slice validation.");

            HoveringEnemy2D hoveringEnemy = Object.FindAnyObjectByType<HoveringEnemy2D>();

            Assert.That(hoveringEnemy, Is.Not.Null, "Expected the Main scene to contain the hovering enemy encounter.");
            Assert.That(hoveringEnemy.GetComponent<Enemy2D>(), Is.Not.Null,
                "Expected the hovering enemy encounter to use the shared enemy interaction component.");
            Assert.That(hoveringEnemy.GetComponentsInChildren<SpriteRenderer>(true), Is.Not.Empty,
                "Expected the hovering enemy encounter to expose visible placeholder art.");
        }

        [Test]
        public void MainSceneContainsGatePuzzleWiring()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid() && scene.isLoaded, Is.True, "Expected the Main scene to open for slice validation.");

            FloorButton2D floorButton = Object.FindAnyObjectByType<FloorButton2D>();
            LinkedGate2D linkedGate = Object.FindAnyObjectByType<LinkedGate2D>();

            Assert.That(floorButton, Is.Not.Null, "Expected the Main scene to contain the floor button puzzle trigger.");
            Assert.That(linkedGate, Is.Not.Null, "Expected the Main scene to contain the linked gate.");
            Assert.That(floorButton.LinkedGate, Is.SameAs(linkedGate),
                "Expected the floor button to be wired to the scene's linked gate.");

            SerializedObject gateSerializedObject = new SerializedObject(linkedGate);
            SerializedObject buttonSerializedObject = new SerializedObject(floorButton);

            Assert.That(gateSerializedObject.FindProperty("blockingCollider").objectReferenceValue, Is.Not.Null,
                "Expected the linked gate to keep its blocking collider reference.");
            Assert.That(gateSerializedObject.FindProperty("statusIndicatorRenderer").objectReferenceValue, Is.Not.Null,
                "Expected the linked gate to keep its status light reference for clear open/locked feedback.");
            Assert.That(buttonSerializedObject.FindProperty("triggerCollider").objectReferenceValue, Is.Not.Null,
                "Expected the floor button to keep its trigger collider reference.");
            Assert.That(buttonSerializedObject.FindProperty("spriteRenderer").objectReferenceValue, Is.Not.Null,
                "Expected the floor button to keep its placeholder visual reference.");
        }
    }
}
