using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VibeCode.Platformer;

public static class StarterSceneBuilder
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
    private const string PlaceholderSpritePath = "Assets/Art/Sprites/PlaceholderSquare.png";
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/PatrollingEnemy.prefab";
    private static readonly Vector2 PlayerSize = new Vector2(0.225f, 0.45f);
    private static readonly Vector2 EnemySize = new Vector2(0.45f, 0.35f);
    private static readonly float PlayerGroundedY = -2.3375f;
    private static readonly Vector2 PlatformSize = new Vector2(1f, 0.1875f);
    private static readonly Vector2 SmallPlatformSize = new Vector2(0.75f, 0.1875f);

    [MenuItem("Vibe/Build Platformer Starter Scene")]
    public static void BuildPlatformerStarterScene()
    {
        EnsureFolders();
        Sprite placeholderSprite = EnsurePlaceholderSprite();
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions == null)
        {
            Debug.LogError($"Could not load input actions at '{InputActionsPath}'.");
            return;
        }

        GameObject enemyPrefab = EnsurePatrollingEnemyPrefab(placeholderSprite);
        GameObject player = CreateOrUpdatePlayer(placeholderSprite, inputActions);
        CreateOrUpdateLevel(placeholderSprite);
        CreateOrUpdateGameplay(player, placeholderSprite, enemyPrefab);
        ConfigureCamera(player.transform);
        EnsureBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = player;
        Debug.Log("Gravity Garden slice created. Press Play to collect seeds, avoid the fall zone, and reach the portal.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Art/Sprites");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Enemies");
        EnsureFolder("Assets/Scripts");
        EnsureFolder("Assets/Scripts/Editor");
        EnsureFolder("Assets/Scripts/Runtime");
        EnsureFolder("Assets/Scripts/Runtime/Camera");
        EnsureFolder("Assets/Scripts/Runtime/Enemies");
        EnsureFolder("Assets/Scripts/Runtime/Gameplay");
        EnsureFolder("Assets/Scripts/Runtime/Player");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
    }

    private static Sprite EnsurePlaceholderSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[32 * 32];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        File.WriteAllBytes(ToAbsoluteAssetPath(PlaceholderSpritePath), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(PlaceholderSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
    }

    private static string ToAbsoluteAssetPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(projectRoot, assetPath);
    }

    private static void CreateOrUpdateLevel(Sprite sprite)
    {
        GameObject levelRoot = FindOrCreateRoot("Level");
        CreateOrUpdatePlatform(levelRoot.transform, "Ground", sprite, new Vector2(7f, -2.75f), new Vector2(40f, 0.375f), new Color(0.16f, 0.22f, 0.33f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform A", sprite, new Vector2(-7.5f, -1.65f), PlatformSize, new Color(0.27f, 0.45f, 0.65f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform B", sprite, new Vector2(-2.25f, -0.65f), PlatformSize, new Color(0.27f, 0.45f, 0.65f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform C", sprite, new Vector2(3.25f, 0.25f), SmallPlatformSize, new Color(0.27f, 0.45f, 0.65f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform D", sprite, new Vector2(8.75f, 1f), PlatformSize, new Color(0.27f, 0.45f, 0.65f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform E", sprite, new Vector2(14.25f, 1.8f), PlatformSize, new Color(0.27f, 0.45f, 0.65f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform F", sprite, new Vector2(19.75f, 1.1f), SmallPlatformSize, new Color(0.27f, 0.45f, 0.65f));
    }

    private static void CreateOrUpdateGameplay(GameObject player, Sprite sprite, GameObject enemyPrefab)
    {
        GameObject gameplayRoot = FindOrCreateRoot("Gameplay");
        Transform respawnPoint = EnsureRespawnPoint(gameplayRoot.transform);
        PlayerController2D playerController = player.GetComponent<PlayerController2D>();

        GravityGardenGameManager gameManager = GetOrAddComponent<GravityGardenGameManager>(gameplayRoot);
        ConfigureGameManager(gameManager, playerController, respawnPoint, 3);

        GravityGardenHud hud = GetOrAddComponent<GravityGardenHud>(gameplayRoot);
        ConfigureHud(hud, gameManager);

        GameObject sliceObjectsRoot = FindOrCreateRoot("Slice Objects");
        CreateOrUpdateStartMarker(sliceObjectsRoot.transform, sprite);
        CreateOrUpdateCheckpoint(sliceObjectsRoot.transform, sprite, gameManager);
        CreateOrUpdateExitPortal(sliceObjectsRoot.transform, sprite, gameManager);
        CreateOrUpdateSeeds(sliceObjectsRoot.transform, sprite, gameManager);
        CreateOrUpdateEnemies(sliceObjectsRoot.transform, sprite, enemyPrefab);
        CreateOrUpdateKillZone(sliceObjectsRoot.transform, gameManager);
    }

    private static GameObject EnsurePatrollingEnemyPrefab(Sprite sprite)
    {
        GameObject enemyRoot = new GameObject("Patrolling Enemy");

        try
        {
            SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(enemyRoot);
            renderer.sprite = sprite;
            renderer.color = new Color(0.76f, 0.19f, 0.17f, 1f);
            renderer.sortingOrder = 4;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = EnemySize;

            Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(enemyRoot);
            body.gravityScale = 4f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(enemyRoot);
            collider.size = EnemySize;
            collider.offset = Vector2.zero;

            Enemy2D enemy = GetOrAddComponent<Enemy2D>(enemyRoot);
            PatrollingEnemy2D patrol = GetOrAddComponent<PatrollingEnemy2D>(enemyRoot);

            CreateOrUpdateEnemyVisuals(enemyRoot.transform, sprite);

            Transform edgeCheck = EnsureChildMarker(enemyRoot.transform, "Edge Check", new Vector3(0.28f, -0.2f, 0f));
            Transform wallCheck = EnsureChildMarker(enemyRoot.transform, "Wall Check", new Vector3(0.34f, -0.01f, 0f));

            ConfigureEnemy(enemy, body, collider, renderer);
            ConfigurePatrollingEnemy(patrol, enemy, body, collider, edgeCheck, wallCheck, 1 << 0);

            PrefabUtility.SaveAsPrefabAsset(enemyRoot, EnemyPrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            Object.DestroyImmediate(enemyRoot);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
    }

    private static void CreateOrUpdatePlatform(Transform parent, string objectName, Sprite sprite, Vector2 position, Vector2 size, Color color)
    {
        GameObject platform = FindOrCreateChild(parent, objectName);
        platform.transform.position = new Vector3(position.x, position.y, 0f);
        platform.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(platform);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.size = Vector2.one;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(platform);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
    }

    private static GameObject CreateOrUpdatePlayer(Sprite sprite, InputActionAsset inputActions)
    {
        GameObject player = FindOrCreateRoot("Player");
        player.transform.position = new Vector3(-12.5f, PlayerGroundedY, 0f);
        player.transform.localScale = Vector3.one;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(player);
        renderer.sprite = sprite;
        renderer.color = new Color(0.93f, 0.53f, 0.38f);
        renderer.sortingOrder = 5;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = PlayerSize;

        Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(player);
        body.gravityScale = 4f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D bodyCollider = GetOrAddComponent<CapsuleCollider2D>(player);
        bodyCollider.direction = CapsuleDirection2D.Vertical;
        bodyCollider.size = PlayerSize;
        bodyCollider.offset = Vector2.zero;

        Transform groundCheck = EnsureGroundCheck(player.transform);
        PlayerController2D controller = GetOrAddComponent<PlayerController2D>(player);
        controller.Configure(body, bodyCollider, groundCheck, inputActions, 1 << 0);
        ConfigurePlayerController(controller);

        return player;
    }

    private static Transform EnsureRespawnPoint(Transform parent)
    {
        GameObject respawnPoint = FindOrCreateChild(parent, "Respawn Point");
        respawnPoint.transform.position = new Vector3(-12.5f, PlayerGroundedY, 0f);
        respawnPoint.transform.localScale = Vector3.one;
        return respawnPoint.transform;
    }

    private static void CreateOrUpdateStartMarker(Transform parent, Sprite sprite)
    {
        GameObject startMarker = FindOrCreateChild(parent, "Start Area");
        startMarker.transform.position = new Vector3(-12.5f, -2.5f, 0f);
        startMarker.transform.localScale = new Vector3(0.7f, 0.07f, 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(startMarker);
        renderer.sprite = sprite;
        renderer.color = new Color(0.42f, 0.78f, 0.47f, 1f);
        renderer.sortingOrder = 1;
        renderer.drawMode = SpriteDrawMode.Simple;
    }

    private static void CreateOrUpdateCheckpoint(Transform parent, Sprite sprite, GravityGardenGameManager gameManager)
    {
        GameObject checkpoint = FindOrCreateChild(parent, "Checkpoint 1");
        checkpoint.transform.position = new Vector3(6.25f, PlayerGroundedY, 0f);
        checkpoint.transform.localScale = new Vector3(0.095f, 0.3875f, 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(checkpoint);
        renderer.sprite = sprite;
        renderer.color = new Color(0.47f, 0.66f, 0.9f, 1f);
        renderer.sortingOrder = 2;
        renderer.drawMode = SpriteDrawMode.Simple;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(checkpoint);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = true;

        Checkpoint2D checkpointComponent = GetOrAddComponent<Checkpoint2D>(checkpoint);
        ConfigureCheckpoint(checkpointComponent, gameManager, collider, renderer);
    }

    private static void CreateOrUpdateExitPortal(Transform parent, Sprite sprite, GravityGardenGameManager gameManager)
    {
        GameObject exitPortal = FindOrCreateChild(parent, "Exit Portal");
        exitPortal.transform.position = new Vector3(24.5f, -2.2625f, 0f);
        exitPortal.transform.localScale = new Vector3(0.3f, 0.6f, 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(exitPortal);
        renderer.sprite = sprite;
        renderer.color = new Color(0.42f, 0.3f, 0.63f, 1f);
        renderer.sortingOrder = 2;
        renderer.drawMode = SpriteDrawMode.Simple;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(exitPortal);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = true;

        ExitPortal portal = GetOrAddComponent<ExitPortal>(exitPortal);
        ConfigurePortal(portal, gameManager, collider, renderer);
    }

    private static void CreateOrUpdateSeeds(Transform parent, Sprite sprite, GravityGardenGameManager gameManager)
    {
        GameObject seedsRoot = FindOrCreateChild(parent, "Seed Group");
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 1", sprite, new Vector2(-10.75f, -2.2f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 2", sprite, new Vector2(-7.5f, -1.32f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 3", sprite, new Vector2(-2.25f, -0.32f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 4", sprite, new Vector2(3.25f, 0.58f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 5", sprite, new Vector2(8.75f, 1.33f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 6", sprite, new Vector2(14.25f, 2.12f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 7", sprite, new Vector2(19.75f, 1.42f), gameManager);
    }

    private static void CreateOrUpdateSeed(Transform parent, string objectName, Sprite sprite, Vector2 position, GravityGardenGameManager gameManager)
    {
        GameObject seed = FindOrCreateChild(parent, objectName);
        seed.transform.position = new Vector3(position.x, position.y, 0f);
        seed.transform.localScale = new Vector3(0.1125f, 0.1125f, 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(seed);
        renderer.sprite = sprite;
        renderer.color = new Color(0.95f, 0.88f, 0.38f, 1f);
        renderer.sortingOrder = 3;
        renderer.drawMode = SpriteDrawMode.Simple;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(seed);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = true;

        EnergySeedCollectible collectible = GetOrAddComponent<EnergySeedCollectible>(seed);
        ConfigureSeed(collectible, gameManager, collider, renderer, 1);
    }

    private static void CreateOrUpdateKillZone(Transform parent, GravityGardenGameManager gameManager)
    {
        GameObject killZone = FindOrCreateChild(parent, "Kill Zone");
        killZone.transform.position = new Vector3(7f, -7f, 0f);
        killZone.transform.localScale = new Vector3(60f, 1f, 1f);

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(killZone);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = true;

        KillZone2D zone = GetOrAddComponent<KillZone2D>(killZone);
        ConfigureKillZone(zone, gameManager, collider);
    }

    private static void CreateOrUpdateEnemies(Transform parent, Sprite sprite, GameObject enemyPrefab)
    {
        GameObject enemiesRoot = FindOrCreateChild(parent, "Enemies");

        CreateOrUpdateEnemyBlocker(enemiesRoot.transform, "Enemy Blocker", sprite, new Vector2(9.18f, 1.36875f), new Vector2(0.18f, 0.55f), new Color(0.24f, 0.19f, 0.18f, 1f));

        GameObject enemyObject = FindOrCreatePrefabChild(enemiesRoot.transform, "Patrolling Enemy", enemyPrefab);
        enemyObject.transform.position = new Vector3(8.55f, 1.26875f, 0f);
        enemyObject.transform.localScale = Vector3.one;
    }

    private static void CreateOrUpdateEnemyBlocker(Transform parent, string objectName, Sprite sprite, Vector2 position, Vector2 size, Color color)
    {
        GameObject blocker = FindOrCreateChild(parent, objectName);
        blocker.transform.position = new Vector3(position.x, position.y, 0f);
        blocker.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(blocker);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 1;
        renderer.drawMode = SpriteDrawMode.Simple;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(blocker);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
    }

    private static Transform EnsureGroundCheck(Transform parent)
    {
        Transform groundCheck = parent.Find("GroundCheck");
        if (groundCheck == null)
        {
            GameObject checkObject = new GameObject("GroundCheck");
            groundCheck = checkObject.transform;
            groundCheck.SetParent(parent, false);
        }

        groundCheck.localPosition = new Vector3(0f, -0.24f, 0f);
        return groundCheck;
    }

    private static void CreateOrUpdateEnemyVisuals(Transform parent, Sprite sprite)
    {
        CreateOrUpdateEnemyVisualChild(parent, "Eye Left", sprite, new Vector3(-0.09f, 0.035f, 0f), new Vector3(0.075f, 0.075f, 1f), new Color(1f, 0.96f, 0.62f, 1f), 6);
        CreateOrUpdateEnemyVisualChild(parent, "Eye Right", sprite, new Vector3(0.09f, 0.035f, 0f), new Vector3(0.075f, 0.075f, 1f), new Color(1f, 0.96f, 0.62f, 1f), 6);
        CreateOrUpdateEnemyVisualChild(parent, "Spike Left", sprite, new Vector3(-0.13f, 0.19f, 0f), new Vector3(0.08f, 0.08f, 1f), new Color(1f, 0.72f, 0.26f, 1f), 5, 45f);
        CreateOrUpdateEnemyVisualChild(parent, "Spike Middle", sprite, new Vector3(0f, 0.205f, 0f), new Vector3(0.08f, 0.08f, 1f), new Color(1f, 0.72f, 0.26f, 1f), 5, 45f);
        CreateOrUpdateEnemyVisualChild(parent, "Spike Right", sprite, new Vector3(0.13f, 0.19f, 0f), new Vector3(0.08f, 0.08f, 1f), new Color(1f, 0.72f, 0.26f, 1f), 5, 45f);
    }

    private static void CreateOrUpdateEnemyVisualChild(Transform parent, string objectName, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder, float zRotation = 0f)
    {
        GameObject child = FindOrCreateChild(parent, objectName);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        child.transform.localScale = localScale;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(child);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Simple;
    }

    private static Transform EnsureChildMarker(Transform parent, string objectName, Vector3 localPosition)
    {
        GameObject child = FindOrCreateChild(parent, objectName);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child.transform;
    }

    private static void ConfigureCamera(Transform target)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            mainCamera.orthographic = true;
        }

        Transform cameraTransform = mainCamera.transform;
        cameraTransform.position = new Vector3(target.position.x, target.position.y + 1.5f, -10f);

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5.5f;

        CameraFollow2D follow = GetOrAddComponent<CameraFollow2D>(mainCamera.gameObject);
        follow.SetTarget(target);
    }

    private static void ConfigurePlayerController(PlayerController2D controller)
    {
        SerializedObject serializedObject = new SerializedObject(controller);
        serializedObject.FindProperty("moveSpeed").floatValue = 5.5f;
        serializedObject.FindProperty("groundAcceleration").floatValue = 45f;
        serializedObject.FindProperty("groundDeceleration").floatValue = 60f;
        serializedObject.FindProperty("airAcceleration").floatValue = 26.25f;
        serializedObject.FindProperty("airDeceleration").floatValue = 30f;
        serializedObject.FindProperty("jumpVelocity").floatValue = 9.2f;
        serializedObject.FindProperty("lowJumpGravityMultiplier").floatValue = 2f;
        serializedObject.FindProperty("maxFallSpeed").floatValue = 20f;
        serializedObject.FindProperty("groundCheckRadius").floatValue = 0.03f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureGameManager(GravityGardenGameManager gameManager, PlayerController2D player, Transform respawnPoint, int minimumSeedsToExit)
    {
        SerializedObject serializedObject = new SerializedObject(gameManager);
        serializedObject.FindProperty("player").objectReferenceValue = player;
        serializedObject.FindProperty("respawnPoint").objectReferenceValue = respawnPoint;
        serializedObject.FindProperty("minimumSeedsToExit").intValue = minimumSeedsToExit;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureHud(GravityGardenHud hud, GravityGardenGameManager gameManager)
    {
        SerializedObject serializedObject = new SerializedObject(hud);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePortal(ExitPortal portal, GravityGardenGameManager gameManager, Collider2D triggerCollider, SpriteRenderer renderer)
    {
        SerializedObject serializedObject = new SerializedObject(portal);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.FindProperty("triggerCollider").objectReferenceValue = triggerCollider;
        serializedObject.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSeed(EnergySeedCollectible collectible, GravityGardenGameManager gameManager, Collider2D triggerCollider, SpriteRenderer renderer, int seedValue)
    {
        SerializedObject serializedObject = new SerializedObject(collectible);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.FindProperty("triggerCollider").objectReferenceValue = triggerCollider;
        serializedObject.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        serializedObject.FindProperty("seedValue").intValue = seedValue;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCheckpoint(Checkpoint2D checkpoint, GravityGardenGameManager gameManager, Collider2D triggerCollider, SpriteRenderer renderer)
    {
        SerializedObject serializedObject = new SerializedObject(checkpoint);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.FindProperty("triggerCollider").objectReferenceValue = triggerCollider;
        serializedObject.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureKillZone(KillZone2D killZone, GravityGardenGameManager gameManager, Collider2D triggerCollider)
    {
        SerializedObject serializedObject = new SerializedObject(killZone);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.FindProperty("triggerCollider").objectReferenceValue = triggerCollider;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEnemy(Enemy2D enemy, Rigidbody2D body, Collider2D bodyCollider, SpriteRenderer renderer)
    {
        SerializedObject serializedObject = new SerializedObject(enemy);
        serializedObject.FindProperty("body").objectReferenceValue = body;
        serializedObject.FindProperty("bodyCollider").objectReferenceValue = bodyCollider;
        serializedObject.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        serializedObject.FindProperty("stompBounceVelocity").floatValue = 8.4f;
        serializedObject.FindProperty("stompMinFallSpeed").floatValue = 0.1f;
        serializedObject.FindProperty("stompContactPadding").floatValue = 0.08f;
        serializedObject.FindProperty("defeatPlayerMessage").stringValue = "The critter bit back. Try again.";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePatrollingEnemy(PatrollingEnemy2D patrol, Enemy2D enemy, Rigidbody2D body, Collider2D bodyCollider, Transform edgeCheck, Transform wallCheck, LayerMask blockerLayers)
    {
        SerializedObject serializedObject = new SerializedObject(patrol);
        serializedObject.FindProperty("enemy").objectReferenceValue = enemy;
        serializedObject.FindProperty("body").objectReferenceValue = body;
        serializedObject.FindProperty("bodyCollider").objectReferenceValue = bodyCollider;
        serializedObject.FindProperty("edgeCheck").objectReferenceValue = edgeCheck;
        serializedObject.FindProperty("wallCheck").objectReferenceValue = wallCheck;
        serializedObject.FindProperty("blockerLayers").intValue = blockerLayers.value;
        serializedObject.FindProperty("moveSpeed").floatValue = 1.6f;
        serializedObject.FindProperty("edgeCheckRadius").floatValue = 0.05f;
        serializedObject.FindProperty("wallCheckRadius").floatValue = 0.05f;
        serializedObject.FindProperty("startFacingRight").boolValue = false;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScenePath, true)
        };
    }

    private static GameObject FindOrCreateRoot(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        return new GameObject(objectName);
    }

    private static GameObject FindOrCreateChild(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject created = new GameObject(objectName);
        created.transform.SetParent(parent, false);
        return created;
    }

    private static GameObject FindOrCreatePrefabChild(Transform parent, string objectName, GameObject prefab)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject created = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject(objectName);

        created.name = objectName;
        created.transform.SetParent(parent, false);
        return created;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T existing = gameObject.GetComponent<T>();
        if (existing != null)
        {
            return existing;
        }

        return gameObject.AddComponent<T>();
    }
}
