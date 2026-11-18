using System;
using UnityEditor;
using UnityEngine;
using WobbleBrawl.Gameplay;

public static class CombatPickupArt
{
    private const string ArtFolder = "Assets/_Project/Art/Items";
    private const string PrefabFolder = "Assets/_Project/Prefabs";
    private const string VisualName = "Blender Visual";
    private const string ToonGraph =
        ArtFolder + "/ToonPickupSurface.shadergraph";

    [MenuItem("Wobble Brawl/Apply Combat Pickup Art")]
    public static void Apply()
    {
        if (!AssetDatabase.IsValidFolder(ArtFolder + "/Materials"))
            AssetDatabase.CreateFolder(ArtFolder, "Materials");
        foreach (string key in new[] { "BananaPeel", "FoamGlove", "RubberChicken",
                     "RocketPlunger", "WhoopeeCushion", "Anvil" })
            ApplyToPrefab(key);
        AssetDatabase.SaveAssets();
    }

    private static void ApplyToPrefab(string key)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtFolder}/{key}.fbx");
        if (model == null)
            throw new InvalidOperationException($"Missing {key} FBX.");

        string path = $"{PrefabFolder}/{key}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (!root.TryGetComponent<PooledItem>(out _) ||
                !root.TryGetComponent<Collider>(out var trigger) || !trigger.isTrigger)
                throw new InvalidOperationException($"{key} needs its existing pooled trigger.");

            var previous = root.transform.Find(VisualName);
            if (previous != null)
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (root.TryGetComponent<MeshRenderer>(out var placeholder))
                UnityEngine.Object.DestroyImmediate(placeholder);
            if (root.TryGetComponent<MeshFilter>(out var filter))
                UnityEngine.Object.DestroyImmediate(filter);

            var visual = new GameObject(VisualName);
            visual.transform.SetParent(root.transform, false);
            Vector3 scale = root.transform.localScale;
            // Preserve the existing root scale, which determines the trigger size.
            visual.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            PrefabUtility.InstantiatePrefab(model, visual.transform);
            var renderers = visual.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length != 1)
                throw new InvalidOperationException($"{key} must import as a single mesh.");
            var renderer = renderers[0];
            var materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
                materials[index] = ImportPaletteMaterial(materials[index].name);
            renderer.sharedMaterials = materials;

            Vector3 size = renderer.bounds.size;
            float extent = Mathf.Max(size.x, size.y, size.z);
            if (extent < .45f || extent > .75f)
                throw new InvalidOperationException($"{key} imported at the wrong scale: {size}.");
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"Pickup art verified: {key}, bounds {size}, {materials.Length} materials.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Material ImportPaletteMaterial(string name)
    {
        int separator = name.LastIndexOf('_');
        if (separator < 0 || !ColorUtility.TryParseHtmlString("#" + name.Substring(separator + 1), out var color))
            throw new InvalidOperationException($"Invalid pickup palette material: {name}.");

        string path = $"{ArtFolder}/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ToonGraph);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("The pickup toon Shader Graph failed to import.");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
            material.shader = shader;
        material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    [MenuItem("Wobble Brawl/Refresh Combat Pickup Toon Materials")]
    public static void RefreshToonMaterials()
    {
        string folder = ArtFolder + "/Materials";
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var material = ImportPaletteMaterial(System.IO.Path.GetFileNameWithoutExtension(path));
            Debug.Log($"Pickup toon verified: {material.name} uses {material.shader.name}.");
        }
        AssetDatabase.SaveAssets();
    }
}
