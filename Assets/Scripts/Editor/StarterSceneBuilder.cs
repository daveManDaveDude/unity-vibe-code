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

        GameObject player = CreateOrUpdatePlayer(placeholderSprite, inputActions);
        CreateOrUpdateLevel(placeholderSprite);
        CreateOrUpdateGameplay(player, placeholderSprite);
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
        EnsureFolder("Assets/Scripts");
        EnsureFolder("Assets/Scripts/Editor");
        EnsureFolder("Assets/Scripts/Runtime");
        EnsureFolder("Assets/Scripts/Runtime/Camera");
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
        CreateOrUpdatePlatform(levelRoot.transform, "Ground", sprite, new Vector2(0f, -2.75f), new Vector2(18f, 1.5f), new Color(0.16f, 0.22f, 0.33f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform A", sprite, new Vector2(-2.5f, -0.25f), new Vector2(4f, 0.75f), new Color(0.27f, 0.45f, 0.65f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform B", sprite, new Vector2(3.5f, 1.2f), new Vector2(4f, 0.75f), new Color(0.27f, 0.45f, 0.65f));
        CreateOrUpdatePlatform(levelRoot.transform, "Platform C", sprite, new Vector2(7.5f, 2.5f), new Vector2(3f, 0.75f), new Color(0.27f, 0.45f, 0.65f));
    }

    private static void CreateOrUpdateGameplay(GameObject player, Sprite sprite)
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
        CreateOrUpdateExitPortal(sliceObjectsRoot.transform, sprite, gameManager);
        CreateOrUpdateSeeds(sliceObjectsRoot.transform, sprite, gameManager);
        CreateOrUpdateKillZone(sliceObjectsRoot.transform, gameManager);
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
        player.transform.position = new Vector3(-6.5f, -1.1f, 0f);
        player.transform.localScale = Vector3.one;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(player);
        renderer.sprite = sprite;
        renderer.color = new Color(0.93f, 0.53f, 0.38f);
        renderer.sortingOrder = 5;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(0.9f, 1.8f);

        Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(player);
        body.gravityScale = 4f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D bodyCollider = GetOrAddComponent<CapsuleCollider2D>(player);
        bodyCollider.direction = CapsuleDirection2D.Vertical;
        bodyCollider.size = new Vector2(0.9f, 1.8f);
        bodyCollider.offset = Vector2.zero;

        Transform groundCheck = EnsureGroundCheck(player.transform);
        PlayerController2D controller = GetOrAddComponent<PlayerController2D>(player);
        controller.Configure(body, bodyCollider, groundCheck, inputActions, 1 << 0);

        return player;
    }

    private static Transform EnsureRespawnPoint(Transform parent)
    {
        GameObject respawnPoint = FindOrCreateChild(parent, "Respawn Point");
        respawnPoint.transform.position = new Vector3(-6.5f, -1.1f, 0f);
        respawnPoint.transform.localScale = Vector3.one;
        return respawnPoint.transform;
    }

    private static void CreateOrUpdateStartMarker(Transform parent, Sprite sprite)
    {
        GameObject startMarker = FindOrCreateChild(parent, "Start Area");
        startMarker.transform.position = new Vector3(-6.5f, -1.84f, 0f);
        startMarker.transform.localScale = new Vector3(2.8f, 0.28f, 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(startMarker);
        renderer.sprite = sprite;
        renderer.color = new Color(0.42f, 0.78f, 0.47f, 1f);
        renderer.sortingOrder = 1;
        renderer.drawMode = SpriteDrawMode.Simple;
    }

    private static void CreateOrUpdateExitPortal(Transform parent, Sprite sprite, GravityGardenGameManager gameManager)
    {
        GameObject exitPortal = FindOrCreateChild(parent, "Exit Portal");
        exitPortal.transform.position = new Vector3(8.2f, -0.8f, 0f);
        exitPortal.transform.localScale = new Vector3(1.2f, 2.4f, 1f);

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
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 1", sprite, new Vector2(-5.2f, -1.15f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 2", sprite, new Vector2(-2.5f, 0.9f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 3", sprite, new Vector2(3.5f, 2.35f), gameManager);
        CreateOrUpdateSeed(seedsRoot.transform, "Seed 4", sprite, new Vector2(7.5f, 3.55f), gameManager);
    }

    private static void CreateOrUpdateSeed(Transform parent, string objectName, Sprite sprite, Vector2 position, GravityGardenGameManager gameManager)
    {
        GameObject seed = FindOrCreateChild(parent, objectName);
        seed.transform.position = new Vector3(position.x, position.y, 0f);
        seed.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

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
        killZone.transform.position = new Vector3(0f, -8.5f, 0f);
        killZone.transform.localScale = new Vector3(40f, 2f, 1f);

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(killZone);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = true;

        KillZone2D zone = GetOrAddComponent<KillZone2D>(killZone);
        ConfigureKillZone(zone, gameManager, collider);
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

        groundCheck.localPosition = new Vector3(0f, -0.96f, 0f);
        return groundCheck;
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
        cameraTransform.position = new Vector3(0f, 1.5f, -10f);

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5.5f;

        CameraFollow2D follow = GetOrAddComponent<CameraFollow2D>(mainCamera.gameObject);
        follow.SetTarget(target);
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

    private static void ConfigureKillZone(KillZone2D killZone, GravityGardenGameManager gameManager, Collider2D triggerCollider)
    {
        SerializedObject serializedObject = new SerializedObject(killZone);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.FindProperty("triggerCollider").objectReferenceValue = triggerCollider;
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
