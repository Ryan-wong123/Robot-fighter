using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;
using Object = UnityEngine.Object;

/// <summary>Authors the six approved pickups and a separate test scene without rewriting existing scenes.</summary>
[InitializeOnLoad]
public static class NoveltyItemsContent
{
    public const string Art = "Assets/_Project/Art/Items/Novelty";
    public const string Data = "Assets/_Project/Data/Items/Novelty";
    public const string CatalogPath = "Assets/_Project/Resources/Items/LaunchItems.asset";
    public const string TestScene = "Assets/_Project/Scenes/NewItemSandbox.unity";
    private static readonly string[] Keys = { "CreamPie", "Boomerang", "PogoStick", "BubbleWand", "WindUpCar", "BoxingJack" };
    private static readonly string[] Labels = { "Cream Pie", "Boomerang", "Pogo Stick", "Bubble Wand", "Wind-up Car", "Spring Boxing Jack" };
    private static readonly string[] Ids = { "cream-pie", "boomerang", "pogo-stick", "bubble-wand", "wind-up-car", "boxing-jack" };
    internal static bool IsBuilding { get; private set; }
    private static bool automaticBuildFailed;

    static NoveltyItemsContent()
    {
        EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
        };
    }

    internal static void Install()
    {
        if (IsBuilding || automaticBuildFailed) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Install;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode || Keys.Any(key =>
            AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/" + key + ".fbx") == null)) return;
        if (AssetDatabase.LoadAssetAtPath<LaunchItemCatalog>(CatalogPath) == null ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene) == null)
        {
            try { Build(); }
            catch (Exception error)
            {
                automaticBuildFailed = true;
                Debug.LogException(error);
            }
        }
    }

    [MenuItem("Wobble Brawl/Items/Build Additional Six Items")]
    public static void Build()
    {
        if (IsBuilding) return;
        IsBuilding = true;
        automaticBuildFailed = false;
        try { BuildContent(); }
        catch { automaticBuildFailed = true; throw; }
        finally { IsBuilding = false; }
    }

    private static void BuildContent()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build outside Play Mode.");
        var installed = AssetDatabase.LoadAssetAtPath<LaunchItemCatalog>(CatalogPath);
        if (installed != null)
        {
            BuildTestScene(installed.Items);
            return;
        }
        Folder(Data);
        Folder(Art + "/Materials");
        Folder("Assets/_Project/Resources/Items");
        ConfigureTexture();
        var body = MakeMaterial("NoveltyToon", "Assets/_Project/Art/Items/ToonPickupSurface.shadergraph");
        body.SetColor("_BaseColor", Color.white);
        body.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/NoveltyItems_Atlas.png"));
        var outline = MakeMaterial("NoveltyOutline", "Assets/_Project/Art/Characters/Blobby/Materials/JellyOutline.mat");
        outline.SetFloat("_OutlineWidth", .012f);
        var bubble = MakeMaterial("BubbleSurface", "Assets/_Project/Art/BathtubBrawl/ToonStageWater.shader");
        bubble.SetColor("_BaseColor", new Color(.4f, .85f, 1f, .22f));
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(outline);
        EditorUtility.SetDirty(bubble);
        var definitions = new ItemDef[Keys.Length];
        for (int index = 0; index < Keys.Length; index++)
        {
            string key = Keys[index];
            var importer = (ModelImporter)AssetImporter.GetAtPath(Art + "/" + key + ".fbx");
            importer.importAnimation = false;
            importer.preserveHierarchy = true;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var definition = Get<ItemDef>(Data + "/" + key + ".asset");
            definitions[index] = definition;
            definition.Id = Ids[index];
            definition.NoveltyKind = (NoveltyItemKind)index;
            definition.Mode = index == 0 || index == 1 || index == 3 ? ItemUseMode.Thrown : ItemUseMode.Placed;
            definition.Effects = Array.Empty<ItemEffect>();
            definition.LifetimeFrames = 300;
            definition.ThrowSpeed = index == 3 ? 2.5f : index == 4 ? 4f : 7f;
            definition.PlacementOffset = index == 2 ? 0f : .9f;
            definition.GroundClearance = index == 5 ? .27f : .18f;
            definition.StatusFrames = index == 3 ? 120 : 90;
            definition.SlowPercent = 65;
            definition.OutboundFrames = 35;
            definition.EffectRadius = index == 5 ? 1.5f : 1.8f;
            definition.UseVelocity = new Vector2(4f, 10f);
            definition.ActionFrames = 12;
            definition.TelegraphFrames = index == 5 ? 90 : 0;
            definition.AIUseRange = index == 2 ? new Vector2(1.5f, 4.5f) :
                new Vector2(.2f, index == 5 ? 2.4f : 4.5f);
            if (index != 3)
            {
                var hit = Get<MoveDef>(Data + "/" + key + "Hit.asset");
                int[] damage = { 6, 8, 0, 0, 10, 12 };
                int[] stun = { 6, 8, 12, 0, 24, 20 };
                float[] push = { 1.2f, 1.8f, 5.5f, 0f, 3f, 3f };
                hit.BodyDamage = hit.LegDamage = hit.GuardCost = damage[index];
                hit.StunFrames = stun[index];
                hit.Pushback = push[index];
                hit.HitStopFrames = hit.GaugeGain = 0;
                hit.KnocksDown = index == 4;
                hit.HeavyReaction = index >= 4;
                hit.LaunchSpeed = index == 5 ? 6f : 0f;
                definition.ContactHit = hit;
                EditorUtility.SetDirty(hit);
            }
            BuildPrefab(index, definition, body, outline, bubble);
            EditorUtility.SetDirty(definition);
        }
        BuildTestScene(definitions);
        var catalog = Get<LaunchItemCatalog>(CatalogPath);
        catalog.Items = definitions;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static void BuildPrefab(int index, ItemDef definition, Material body, Material outline, Material bubble)
    {
        var root = new GameObject(Labels[index]);
        try
        {
            var physics = root.AddComponent<Rigidbody>();
            physics.isKinematic = true;
            physics.useGravity = false;
            root.AddComponent<SphereCollider>().radius = index == 4 ? .32f : .25f;
            var pooled = root.AddComponent<PooledItem>();
            var novelty = root.AddComponent<NoveltyItem>();
            var model = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/" + Keys[index] + ".fbx"), root.transform);
            var main = FindVisual(model, "_Body", true);
            var action = FindVisual(model, "_Action", index == 3 || index == 5);
            var warning = FindVisual(model, "_Warning", index == 5);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                bool isBubble = index == 3 && action != null && renderer.transform == action;
                renderer.sharedMaterials = new[] { isBubble ? bubble : body, outline };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            Set(novelty, "baseVisual", main);
            Set(novelty, "actionVisual", action);
            Set(novelty, "warningVisual", warning);
            if (index == 3) action.gameObject.SetActive(false);
            if (warning != null) warning.gameObject.SetActive(false);
            string path = Data + "/" + Keys[index] + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            definition.Prefab = AssetDatabase.LoadAssetAtPath<PooledItem>(path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void BuildTestScene(ItemDef[] definitions)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene) != null) return;
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = default;
        try
        {
            if (!AssetDatabase.CopyAsset("Assets/_Project/Scenes/CombatSandbox.unity", TestScene))
                throw new InvalidOperationException("Could not copy the combat test scene.");
            scene = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Additive);
            var round = scene.GetRootGameObjects().Select(root => root.GetComponent<RoundController>())
                .Single(component => component != null);
            var properties = new SerializedObject(round);
            var hazard = (HazardController)properties.FindProperty("hazard").objectReferenceValue;
            if (hazard != null) Object.DestroyImmediate(hazard.gameObject);
            properties.FindProperty("hazard").objectReferenceValue = null;
            properties.ApplyModifiedPropertiesWithoutUndo();
            var spawner = (ItemSpawner)properties.FindProperty("itemSpawner").objectReferenceValue;
            var spawn = new SerializedObject(spawner);
            var table = spawn.FindProperty("table");
            table.arraySize = definitions.Length;
            for (int index = 0; index < definitions.Length; index++)
            {
                var entry = table.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("Item").objectReferenceValue = definitions[index];
                entry.FindPropertyRelative("Weight").intValue = 1;
                entry.FindPropertyRelative("Capacity").intValue = 1;
            }
            spawn.FindProperty("spawnOpeningLineup").boolValue = true;
            spawn.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(entry => entry.path == TestScene)) scenes.Add(new EditorBuildSettingsScene(TestScene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    // Unity may collapse a one-mesh FBX into its renamed root. Mesh names survive that layout.
    public static Transform FindVisual(GameObject model, string suffix, bool required)
    {
        var filters = model.GetComponentsInChildren<MeshFilter>(true);
        var matches = filters.Where(filter => filter.transform.name.EndsWith(suffix, StringComparison.Ordinal) ||
            (filter.sharedMesh != null && filter.sharedMesh.name.EndsWith(suffix, StringComparison.Ordinal))).ToArray();
        if (matches.Length == 1) return matches[0].transform;
        if (matches.Length == 0 && suffix == "_Body" && filters.Length == 1) return filters[0].transform;
        if (!required && matches.Length == 0) return null;
        throw new InvalidOperationException("Expected one " + suffix + " mesh in " + model.name +
            "; imported meshes: " + string.Join(", ", filters.Select(filter => filter.name)));
    }

    private static Material MakeMaterial(string name, string source)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var original = AssetDatabase.LoadAssetAtPath<Material>(source);
        var shader = original != null ? original.shader : AssetDatabase.LoadAssetAtPath<Shader>(source);
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Import shared toon shaders first.");
        var material = original != null ? new Material(original) : new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static T Get<T>(string path) where T : ScriptableObject
    {
        var value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value != null) return value;
        value = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(value, path);
        return value;
    }

    private static void Set(Object target, string name, Object value)
    {
        var properties = new SerializedObject(target);
        properties.FindProperty(name).objectReferenceValue = value;
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        Folder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static void ConfigureTexture()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "/NoveltyItems_Atlas.png");
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.maxTextureSize = 1024;
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.format = TextureImporterFormat.ASTC_6x6;
        android.maxTextureSize = 1024;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
    }
}

internal sealed class NoveltyItemsImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (!NoveltyItemsContent.IsBuilding && imported.Any(path =>
            path.StartsWith(NoveltyItemsContent.Art + "/", StringComparison.Ordinal) &&
            (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith("NoveltyItems_Atlas.png", StringComparison.OrdinalIgnoreCase))))
            EditorApplication.delayCall += NoveltyItemsContent.Install;
    }
}
