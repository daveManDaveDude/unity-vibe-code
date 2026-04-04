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
    private const string MovingPlatformPrefabPath = "Assets/Prefabs/Gameplay/MovingPlatform.prefab";
    private const string CyclingSpikeHazardPrefabPath = "Assets/Prefabs/Gameplay/CyclingSpikeHazard.prefab";

    private static readonly Vector2 PlayerSize = new Vector2(0.225f, 0.45f);
    private static readonly Vector2 StaticPlatformThickness = new Vector2(1f, 0.1875f);
    private static readonly float GroundY = -2.75f;
    private static readonly float PlayerGroundedY = -2.3375f;

    [MenuItem("Vibe/Build Platformer Starter Scene")]
    public static void BuildPlatformerStarterScene()
    {
        EnsureFolders();
        Sprite placeholderSprite = EnsurePlaceholderSprite();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions == null)
        {
            Debug.LogError($"Could not load input actions at '{InputActionsPath}'.");
            return;
        }

        GameObject movingPlatformPrefab = EnsureMovingPlatformPrefab(placeholderSprite);
        GameObject timedHazardPrefab = EnsureCyclingSpikeHazardPrefab(placeholderSprite);

        GameObject player = CreateOrUpdatePlayer(placeholderSprite, inputActions);
        GravityGardenGameManager gameManager = CreateOrUpdateGameplay(player);
        CreateOrUpdateLevel(placeholderSprite);
        CreateOrUpdateSliceObjects(placeholderSprite, gameManager, movingPlatformPrefab, timedHazardPrefab);
        ConfigureCamera(player.transform);
        EnsureBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = player;
        Debug.Log("Gravity Garden slice rebuilt. Collect seeds, ride the moving platform, time the thorn bridge, and reach the portal.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Art/Sprites");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Gameplay");
        EnsureFolder("Assets/Scripts");
        EnsureFolder("Assets/Scripts/Editor");
        EnsureFolder("Assets/Scripts/Runtime");
        EnsureFolder("Assets/Scripts/Runtime/Camera");
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
        ConfigurePlaceholderSpriteImporter();
        Sprite sprite = LoadPlaceholderSpriteSubAsset();
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
        ConfigurePlaceholderSpriteImporter();

        return LoadPlaceholderSpriteSubAsset();
    }

    private static void ConfigurePlaceholderSpriteImporter()
    {
        if (AssetImporter.GetAtPath(PlaceholderSpritePath) is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.spriteImportMode = SpriteImportMode.Single;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static Sprite LoadPlaceholderSpriteSubAsset()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PlaceholderSpritePath);
        for (int index = 0; index < assets.Length; index++)
        {
            if (assets[index] is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }

    private static string ToAbsoluteAssetPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(projectRoot, assetPath);
    }

    private static GameObject CreateOrUpdatePlayer(Sprite sprite, InputActionAsset inputActions)
    {
        GameObject player = FindOrCreateRoot("Player");
        player.transform.position = new Vector3(-12.5f, PlayerGroundedY, 0f);
        player.transform.localScale = Vector3.one;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(player);
        renderer.sprite = sprite;
        renderer.color = new Color(0.93f, 0.53f, 0.38f, 1f);
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

    private static GravityGardenGameManager CreateOrUpdateGameplay(GameObject player)
    {
        GameObject gameplayRoot = FindOrCreateRoot("Gameplay");
        Transform respawnPoint = EnsureRespawnPoint(gameplayRoot.transform);
        PlayerController2D playerController = player.GetComponent<PlayerController2D>();

        GravityGardenGameManager gameManager = GetOrAddComponent<GravityGardenGameManager>(gameplayRoot);
        ConfigureGameManager(gameManager, playerController, respawnPoint, 5);

        GravityGardenHud hud = GetOrAddComponent<GravityGardenHud>(gameplayRoot);
        ConfigureHud(hud, gameManager);

        return gameManager;
    }

    private static void CreateOrUpdateLevel(Sprite sprite)
    {
        GameObject levelRoot = FindOrCreateRoot("Level");
        Transform terrainRoot = FindOrCreateChild(levelRoot.transform, "Terrain").transform;
        Transform sceneryRoot = FindOrCreateChild(levelRoot.transform, "Scenery").transform;

        CreateOrUpdatePlatform(terrainRoot, "Start Ground", sprite, new Vector2(-10.85f, GroundY), new Vector2(7.8f, 0.375f), new Color(0.11f, 0.15f, 0.22f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Intro Platform", sprite, new Vector2(-7.9f, -1.7f), new Vector2(1.8f, StaticPlatformThickness.y), new Color(0.17f, 0.29f, 0.35f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Departure Ledge", sprite, new Vector2(-5.1f, -1.05f), new Vector2(1.6f, StaticPlatformThickness.y), new Color(0.19f, 0.32f, 0.38f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Mid Ground", sprite, new Vector2(4.9f, GroundY), new Vector2(4.8f, 0.375f), new Color(0.11f, 0.15f, 0.22f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Hazard Wait Perch", sprite, new Vector2(8.4f, -1.35f), new Vector2(1.4f, StaticPlatformThickness.y), new Color(0.17f, 0.29f, 0.35f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Hazard Bridge", sprite, new Vector2(10.45f, GroundY), new Vector2(2.4f, 0.375f), new Color(0.11f, 0.15f, 0.22f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Landing Perch", sprite, new Vector2(12.65f, -1.35f), new Vector2(1.6f, StaticPlatformThickness.y), new Color(0.17f, 0.29f, 0.35f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Final Ground", sprite, new Vector2(16.2f, GroundY), new Vector2(6.8f, 0.375f), new Color(0.11f, 0.15f, 0.22f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Final Step", sprite, new Vector2(14.25f, -0.85f), new Vector2(1.7f, StaticPlatformThickness.y), new Color(0.17f, 0.29f, 0.35f, 1f));
        CreateOrUpdatePlatform(terrainRoot, "Portal Dais", sprite, new Vector2(18.65f, -2.45f), new Vector2(1.55f, 0.12f), new Color(0.25f, 0.34f, 0.23f, 1f));

        CreateOrUpdateBackdrop(sceneryRoot, "Garden Backdrop", sprite, new Vector2(2.4f, -0.95f), new Vector2(39.2f, 6.8f), new Color(0.2f, 0.39f, 0.42f, 0.2f));
        CreateOrUpdateBackdrop(sceneryRoot, "Abyss Fog", sprite, new Vector2(2.5f, -5.4f), new Vector2(46f, 3f), new Color(0.06f, 0.11f, 0.14f, 0.5f));
    }

    private static void CreateOrUpdateSliceObjects(Sprite sprite, GravityGardenGameManager gameManager, GameObject movingPlatformPrefab, GameObject timedHazardPrefab)
    {
        GameObject sliceObjectsRoot = FindOrCreateRoot("Slice Objects");
        Transform markersRoot = FindOrCreateChild(sliceObjectsRoot.transform, "Markers").transform;
        Transform collectiblesRoot = FindOrCreateChild(sliceObjectsRoot.transform, "Collectibles").transform;
        Transform traversalRoot = FindOrCreateChild(sliceObjectsRoot.transform, "Traversal").transform;
        Transform hazardsRoot = FindOrCreateChild(sliceObjectsRoot.transform, "Hazards").transform;

        CreateOrUpdateStartMarker(markersRoot, sprite);
        CreateOrUpdateCheckpoint(markersRoot, sprite, gameManager);
        CreateOrUpdateExitPortal(markersRoot, sprite, gameManager);
        CreateOrUpdateSeeds(collectiblesRoot, sprite, gameManager);
        CreateOrUpdateMovingPlatform(traversalRoot, movingPlatformPrefab);
        CreateOrUpdateHazard(hazardsRoot, timedHazardPrefab, gameManager);
        CreateOrUpdateKillZone(sliceObjectsRoot.transform, gameManager);
    }

    private static GameObject EnsureMovingPlatformPrefab(Sprite sprite)
    {
        GameObject platformRoot = new GameObject("Moving Platform");

        try
        {
            SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(platformRoot);
            renderer.sprite = sprite;
            renderer.color = new Color(0.42f, 0.86f, 0.82f, 1f);
            renderer.sortingOrder = 2;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(2.4f, 0.3f);

            Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(platformRoot);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(platformRoot);
            collider.size = new Vector2(2.4f, 0.3f);
            collider.offset = Vector2.zero;
            collider.isTrigger = false;

            Transform pathRoot = FindOrCreateChild(platformRoot.transform, "Path").transform;
            EnsureChildMarker(pathRoot, "Point A", Vector3.zero);
            EnsureChildMarker(pathRoot, "Point B", new Vector3(4.65f, 0f, 0f));

            MovingPlatform2D movingPlatform = GetOrAddComponent<MovingPlatform2D>(platformRoot);
            ConfigureMovingPlatform(movingPlatform, body, collider, renderer, pathRoot, new Vector2(2.4f, 0.3f), 2.45f, 0.3f);

            PrefabUtility.SaveAsPrefabAsset(platformRoot, MovingPlatformPrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            Object.DestroyImmediate(platformRoot);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(MovingPlatformPrefabPath);
    }

    private static GameObject EnsureCyclingSpikeHazardPrefab(Sprite sprite)
    {
        GameObject hazardRoot = new GameObject("Cycling Spike Hazard");

        try
        {
            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(hazardRoot);
            collider.size = new Vector2(2.05f, 0.7f);
            collider.offset = new Vector2(0f, 0.28f);
            collider.isTrigger = true;

            Transform visualsRoot = FindOrCreateChild(hazardRoot.transform, "Visuals").transform;
            visualsRoot.localPosition = new Vector3(0f, 0.12f, 0f);
            visualsRoot.localScale = Vector3.one;

            CreateOrUpdateVisualChild(visualsRoot, "Base", sprite, new Vector3(0f, -0.14f, 0f), new Vector3(2.1f, 0.14f, 1f), new Color(0.35f, 0.71f, 0.52f, 1f), 3);
            CreateOrUpdateVisualChild(visualsRoot, "Spike Left", sprite, new Vector3(-0.62f, 0.02f, 0f), new Vector3(0.34f, 0.34f, 1f), new Color(0.35f, 0.71f, 0.52f, 1f), 4, 45f);
            CreateOrUpdateVisualChild(visualsRoot, "Spike Mid Left", sprite, new Vector3(-0.2f, 0.08f, 0f), new Vector3(0.34f, 0.34f, 1f), new Color(0.35f, 0.71f, 0.52f, 1f), 4, 45f);
            CreateOrUpdateVisualChild(visualsRoot, "Spike Mid Right", sprite, new Vector3(0.2f, 0.08f, 0f), new Vector3(0.34f, 0.34f, 1f), new Color(0.35f, 0.71f, 0.52f, 1f), 4, 45f);
            CreateOrUpdateVisualChild(visualsRoot, "Spike Right", sprite, new Vector3(0.62f, 0.02f, 0f), new Vector3(0.34f, 0.34f, 1f), new Color(0.35f, 0.71f, 0.52f, 1f), 4, 45f);

            GameObject timingLight = FindOrCreateChild(hazardRoot.transform, "Timing Light");
            timingLight.transform.localPosition = new Vector3(0f, 0.82f, 0f);
            timingLight.transform.localRotation = Quaternion.identity;
            timingLight.transform.localScale = new Vector3(0.18f, 0.18f, 1f);

            SpriteRenderer indicatorRenderer = GetOrAddComponent<SpriteRenderer>(timingLight);
            indicatorRenderer.sprite = sprite;
            indicatorRenderer.color = new Color(0.35f, 0.71f, 0.52f, 1f);
            indicatorRenderer.sortingOrder = 5;
            indicatorRenderer.drawMode = SpriteDrawMode.Simple;

            CyclingSpikeHazard2D hazard = GetOrAddComponent<CyclingSpikeHazard2D>(hazardRoot);
            ConfigureHazard(hazard, null, collider, visualsRoot, indicatorRenderer, 1.15f, 0.45f, 0.9f);

            PrefabUtility.SaveAsPrefabAsset(hazardRoot, CyclingSpikeHazardPrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            Object.DestroyImmediate(hazardRoot);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(CyclingSpikeHazardPrefabPath);
    }

    private static void CreateOrUpdateMovingPlatform(Transform parent, GameObject movingPlatformPrefab)
    {
        GameObject platform = InstantiatePrefabChild(parent, "Bridge Platform", movingPlatformPrefab);
        platform.transform.position = new Vector3(-3.35f, -2.02f, 0f);
        platform.transform.rotation = Quaternion.identity;
        platform.transform.localScale = Vector3.one;

        Transform pathRoot = platform.transform.Find("Path");
        if (pathRoot != null)
        {
            EnsureChildMarker(pathRoot, "Point A", Vector3.zero);
            EnsureChildMarker(pathRoot, "Point B", new Vector3(4.65f, 0f, 0f));
        }

        MovingPlatform2D movingPlatform = platform.GetComponent<MovingPlatform2D>();
        Rigidbody2D body = platform.GetComponent<Rigidbody2D>();
        Collider2D collider = platform.GetComponent<Collider2D>();
        SpriteRenderer renderer = platform.GetComponent<SpriteRenderer>();
        ConfigureMovingPlatform(movingPlatform, body, collider, renderer, pathRoot, new Vector2(2.4f, 0.3f), 2.45f, 0.3f);

        CreateOrUpdateStaticMarker(parent, "Bridge Dock Left", movingPlatformPrefab, new Vector3(-4.7f, -2.02f, 0f), new Vector2(0.3f, 0.6f), new Color(0.24f, 0.5f, 0.52f, 1f));
        CreateOrUpdateStaticMarker(parent, "Bridge Dock Right", movingPlatformPrefab, new Vector3(2.65f, -2.02f, 0f), new Vector2(0.3f, 0.6f), new Color(0.24f, 0.5f, 0.52f, 1f));
    }

    private static void CreateOrUpdateHazard(Transform parent, GameObject timedHazardPrefab, GravityGardenGameManager gameManager)
    {
        GameObject hazard = InstantiatePrefabChild(parent, "Thorn Bridge", timedHazardPrefab);
        hazard.transform.position = new Vector3(10.45f, -2.58f, 0f);
        hazard.transform.rotation = Quaternion.identity;
        hazard.transform.localScale = Vector3.one;

        CyclingSpikeHazard2D hazardComponent = hazard.GetComponent<CyclingSpikeHazard2D>();
        Collider2D collider = hazard.GetComponent<Collider2D>();
        Transform visualsRoot = hazard.transform.Find("Visuals");
        Transform indicator = hazard.transform.Find("Timing Light");
        SpriteRenderer indicatorRenderer = indicator != null ? indicator.GetComponent<SpriteRenderer>() : null;
        ConfigureHazard(hazardComponent, gameManager, collider, visualsRoot, indicatorRenderer, 1.15f, 0.45f, 0.9f);
    }

    private static void CreateOrUpdateStartMarker(Transform parent, Sprite sprite)
    {
        GameObject startMarker = FindOrCreateChild(parent, "Start Area");
        startMarker.transform.position = new Vector3(-12.4f, -2.58f, 0f);
        startMarker.transform.localScale = Vector3.one;

        CreateOrUpdateVisualChild(startMarker.transform, "Patch", sprite, Vector3.zero, new Vector3(1.8f, 0.08f, 1f), new Color(0.45f, 0.83f, 0.46f, 1f), 1);
        CreateOrUpdateVisualChild(startMarker.transform, "Stem", sprite, new Vector3(-0.55f, 0.42f, 0f), new Vector3(0.08f, 0.7f, 1f), new Color(0.45f, 0.72f, 0.39f, 1f), 1);
        CreateOrUpdateVisualChild(startMarker.transform, "Plate", sprite, new Vector3(-0.55f, 0.62f, 0f), new Vector3(0.45f, 0.12f, 1f), new Color(0.7f, 0.9f, 0.63f, 1f), 1);
    }

    private static void CreateOrUpdateCheckpoint(Transform parent, Sprite sprite, GravityGardenGameManager gameManager)
    {
        GameObject checkpointRoot = FindOrCreateChild(parent, "Checkpoint");
        checkpointRoot.transform.position = new Vector3(6.15f, -1.7f, 0f);
        checkpointRoot.transform.localScale = Vector3.one;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(checkpointRoot);
        renderer.sprite = sprite;
        renderer.color = new Color(0.47f, 0.66f, 0.9f, 1f);
        renderer.sortingOrder = 2;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(0.28f, 0.28f);

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(checkpointRoot);
        collider.size = new Vector2(1.25f, 1.65f);
        collider.offset = new Vector2(0f, -0.3f);
        collider.isTrigger = true;

        Transform respawnPoint = EnsureChildMarker(checkpointRoot.transform, "Checkpoint Respawn Point", new Vector3(0.8f, PlayerGroundedY - checkpointRoot.transform.position.y, 0f));

        CreateOrUpdateVisualChild(checkpointRoot.transform, "Pole", sprite, new Vector3(0f, -0.46f, 0f), new Vector3(0.09f, 0.82f, 1f), new Color(0.33f, 0.62f, 0.45f, 1f), 1);
        CreateOrUpdateVisualChild(checkpointRoot.transform, "Patch", sprite, new Vector3(0f, -0.84f, 0f), new Vector3(1.45f, 0.08f, 1f), new Color(0.46f, 0.82f, 0.48f, 1f), 1);
        CreateOrUpdateVisualChild(checkpointRoot.transform, "Leaf Left", sprite, new Vector3(-0.18f, -0.15f, 0f), new Vector3(0.22f, 0.1f, 1f), new Color(0.42f, 0.75f, 0.47f, 1f), 1, 28f);
        CreateOrUpdateVisualChild(checkpointRoot.transform, "Leaf Right", sprite, new Vector3(0.18f, -0.15f, 0f), new Vector3(0.22f, 0.1f, 1f), new Color(0.42f, 0.75f, 0.47f, 1f), 1, -28f);

        Checkpoint2D checkpoint = GetOrAddComponent<Checkpoint2D>(checkpointRoot);
        ConfigureCheckpoint(checkpoint, gameManager, collider, renderer, respawnPoint);
    }

    private static void CreateOrUpdateExitPortal(Transform parent, Sprite sprite, GravityGardenGameManager gameManager)
    {
        GameObject portalRoot = FindOrCreateChild(parent, "Exit Portal Set");
        portalRoot.transform.position = Vector3.zero;
        portalRoot.transform.localScale = Vector3.one;

        CreateOrUpdateVisualChild(portalRoot.transform, "Left Post", sprite, new Vector3(18.2f, -2.16f, 0f), new Vector3(0.12f, 1f, 1f), new Color(0.39f, 0.59f, 0.41f, 1f), 1);
        CreateOrUpdateVisualChild(portalRoot.transform, "Right Post", sprite, new Vector3(19.08f, -2.16f, 0f), new Vector3(0.12f, 1f, 1f), new Color(0.39f, 0.59f, 0.41f, 1f), 1);
        CreateOrUpdateVisualChild(portalRoot.transform, "Lintel", sprite, new Vector3(18.64f, -1.68f, 0f), new Vector3(1f, 0.12f, 1f), new Color(0.39f, 0.59f, 0.41f, 1f), 1);

        GameObject exitPortal = FindOrCreateChild(portalRoot.transform, "Portal");
        exitPortal.transform.position = new Vector3(18.64f, -2.22f, 0f);
        exitPortal.transform.localScale = new Vector3(0.3f, 0.64f, 1f);

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
        CreateOrUpdateSeed(parent, "Seed 1", sprite, new Vector2(-12.4f, -2.15f), gameManager);
        CreateOrUpdateSeed(parent, "Seed 2", sprite, new Vector2(-7.9f, -1.25f), gameManager);
        CreateOrUpdateSeed(parent, "Seed 3", sprite, new Vector2(-0.8f, -1.3f), gameManager);
        CreateOrUpdateSeed(parent, "Seed 4", sprite, new Vector2(5.1f, -2.15f), gameManager);
        CreateOrUpdateSeed(parent, "Seed 5", sprite, new Vector2(8.4f, -0.95f), gameManager);
        CreateOrUpdateSeed(parent, "Seed 6", sprite, new Vector2(14.25f, -0.45f), gameManager);
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
        killZone.transform.position = new Vector3(2.5f, -6.7f, 0f);
        killZone.transform.localScale = new Vector3(46f, 1.5f, 1f);

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(killZone);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = true;

        KillZone2D zone = GetOrAddComponent<KillZone2D>(killZone);
        ConfigureKillZone(zone, gameManager, collider);
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
        renderer.sortingOrder = 0;
        renderer.size = Vector2.one;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(platform);
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = false;
    }

    private static void CreateOrUpdateBackdrop(Transform parent, string objectName, Sprite sprite, Vector2 position, Vector2 size, Color color)
    {
        GameObject backdrop = FindOrCreateChild(parent, objectName);
        backdrop.transform.position = new Vector3(position.x, position.y, 5f);
        backdrop.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(backdrop);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = -10;
        renderer.size = Vector2.one;
    }

    private static void CreateOrUpdateStaticMarker(Transform parent, string objectName, GameObject referencePrefab, Vector3 position, Vector2 size, Color color)
    {
        GameObject marker = FindOrCreateChild(parent, objectName);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(marker);
        renderer.sprite = referencePrefab != null ? referencePrefab.GetComponent<SpriteRenderer>().sprite : null;
        renderer.color = color;
        renderer.sortingOrder = 1;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
    }

    private static void CreateOrUpdateVisualChild(Transform parent, string objectName, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder, float zRotation = 0f)
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

    private static Transform EnsureRespawnPoint(Transform parent)
    {
        GameObject respawnPoint = FindOrCreateChild(parent, "Respawn Point");
        respawnPoint.transform.position = new Vector3(-12.5f, PlayerGroundedY, 0f);
        respawnPoint.transform.localScale = Vector3.one;
        return respawnPoint.transform;
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
        mainCamera.backgroundColor = new Color(0.08f, 0.13f, 0.18f, 1f);

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
        serializedObject.FindProperty("maxJumpCount").intValue = 2;
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

    private static void ConfigureKillZone(KillZone2D killZone, GravityGardenGameManager gameManager, Collider2D triggerCollider)
    {
        SerializedObject serializedObject = new SerializedObject(killZone);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.FindProperty("triggerCollider").objectReferenceValue = triggerCollider;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCheckpoint(
        Checkpoint2D checkpoint,
        GravityGardenGameManager gameManager,
        Collider2D triggerCollider,
        SpriteRenderer renderer,
        Transform respawnPoint)
    {
        SerializedObject serializedObject = new SerializedObject(checkpoint);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.FindProperty("triggerCollider").objectReferenceValue = triggerCollider;
        serializedObject.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        serializedObject.FindProperty("respawnPoint").objectReferenceValue = respawnPoint;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureMovingPlatform(
        MovingPlatform2D movingPlatform,
        Rigidbody2D body,
        Collider2D platformCollider,
        SpriteRenderer spriteRenderer,
        Transform pathRoot,
        Vector2 size,
        float moveSpeed,
        float waitDuration)
    {
        SerializedObject serializedObject = new SerializedObject(movingPlatform);
        serializedObject.FindProperty("body").objectReferenceValue = body;
        serializedObject.FindProperty("platformCollider").objectReferenceValue = platformCollider;
        serializedObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
        serializedObject.FindProperty("pathRoot").objectReferenceValue = pathRoot;
        serializedObject.FindProperty("platformSize").vector2Value = size;
        serializedObject.FindProperty("moveSpeed").floatValue = moveSpeed;
        serializedObject.FindProperty("waitDurationAtPoint").floatValue = waitDuration;
        serializedObject.FindProperty("pointReachDistance").floatValue = 0.02f;
        serializedObject.FindProperty("pingPong").boolValue = true;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureHazard(
        CyclingSpikeHazard2D hazard,
        GravityGardenGameManager gameManager,
        Collider2D triggerCollider,
        Transform hazardVisualRoot,
        SpriteRenderer indicatorRenderer,
        float safeDuration,
        float warningDuration,
        float dangerDuration)
    {
        SerializedObject serializedObject = new SerializedObject(hazard);
        serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedObject.FindProperty("triggerCollider").objectReferenceValue = triggerCollider;
        serializedObject.FindProperty("hazardVisualRoot").objectReferenceValue = hazardVisualRoot;
        serializedObject.FindProperty("indicatorRenderer").objectReferenceValue = indicatorRenderer;
        serializedObject.FindProperty("safeDuration").floatValue = safeDuration;
        serializedObject.FindProperty("warningDuration").floatValue = warningDuration;
        serializedObject.FindProperty("dangerDuration").floatValue = dangerDuration;
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

    private static GameObject InstantiatePrefabChild(Transform parent, string objectName, GameObject prefab)
    {
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
