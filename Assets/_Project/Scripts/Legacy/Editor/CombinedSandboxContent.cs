using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WobbleBrawl.Core;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

/// <summary>Consolidates the two authored test scenes through Unity's scene/asset APIs.</summary>
[InitializeOnLoad]
public static class CombinedSandboxContent
{
    public const string ScenePath = "Assets/_Project/Scenes/CombatSandbox.unity";
    public const string RetiredPath = "Assets/_Project/Scenes/NewItemSandbox.unity";
    public const string LayoutPath = "Assets/_Project/Data/Combat/TrainingSandbox.asset";
    private const int Revision = 2;
    private static bool building;

    static CombinedSandboxContent()
    {
        EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
        };
    }

    public static void Install()
    {
        if (building || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isUpdating || EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += Install;
            return;
        }
        var catalog = Resources.Load<LaunchItemCatalog>("Items/LaunchItems");
        if (catalog == null || catalog.Items == null || catalog.Items.Length != 6) return;
        var layout = AssetDatabase.LoadAssetAtPath<TrainingSandboxLayout>(LayoutPath);
        if (layout != null && layout.InstalledRevision == Revision &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(RetiredPath) == null) return;
        var current = SceneManager.GetSceneByPath(ScenePath);
        var retired = SceneManager.GetSceneByPath(RetiredPath);
        if ((current.isLoaded && current.isDirty) || (retired.isLoaded && retired.isDirty)) return;
        Build();
    }

    [MenuItem("Wobble Brawl/Items/Configure Combined Sandbox")]
    public static void Build()
    {
        if (building) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Configure outside Play Mode.");
        var catalog = Resources.Load<LaunchItemCatalog>("Items/LaunchItems");
        if (catalog == null || catalog.Items == null || catalog.Items.Length != 6)
            throw new InvalidOperationException("Build Additional Six Items before configuring the combined sandbox.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var retired = SceneManager.GetSceneByPath(RetiredPath);
        if ((scene.isLoaded && scene.isDirty) || (retired.isLoaded && retired.isDirty))
            throw new InvalidOperationException("Save your modified sandbox scene before configuring the combined sandbox.");
        bool wasLoaded = scene.isLoaded;
        Scene previous = SceneManager.GetActiveScene();
        building = true;
        try
        {
            Backup(ScenePath);
            Backup(RetiredPath);
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var roots = scene.GetRootGameObjects();
            var round = roots.Select(root => root.GetComponent<RoundController>()).Single(component => component != null);
            var floor = roots.Single(root => root.layer == 8 && root.GetComponent<BoxCollider>() != null);
            var camera = roots.Select(root => root.GetComponent<Camera>()).Single(component => component != null);
            var layout = AssetDatabase.LoadAssetAtPath<TrainingSandboxLayout>(LayoutPath);
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<TrainingSandboxLayout>();
                AssetDatabase.CreateAsset(layout, LayoutPath);
            }
            var scale = floor.transform.localScale;
            scale.x = layout.HalfWidth * 2f;
            floor.transform.localScale = scale;
            camera.orthographicSize = layout.CameraSize;
            camera.transform.position = new Vector3(0f, 5f, -12f);
            var properties = new SerializedObject(round);
            properties.FindProperty("trainingMode").boolValue = true;
            var left = (Transform)properties.FindProperty("playerSpawn").objectReferenceValue;
            var right = (Transform)properties.FindProperty("opponentSpawn").objectReferenceValue;
            left.position = new Vector3(-layout.FighterSpawnX, .02f, 0f);
            right.position = new Vector3(layout.FighterSpawnX, .02f, 0f);
            var spawner = (ItemSpawner)properties.FindProperty("itemSpawner").objectReferenceValue;
            properties.ApplyModifiedPropertiesWithoutUndo();
            var bounds = round.GetComponent<TrainingSandbox>() ?? round.gameObject.AddComponent<TrainingSandbox>();
            Set(bounds, "layout", layout);
            Set(bounds, "fightCamera", camera);
            var boundProperties = new SerializedObject(bounds);
            var fighters = boundProperties.FindProperty("fighters");
            fighters.arraySize = 2;
            fighters.GetArrayElementAtIndex(0).objectReferenceValue = round.Player;
            fighters.GetArrayElementAtIndex(1).objectReferenceValue = round.Opponent;
            boundProperties.ApplyModifiedPropertiesWithoutUndo();
            var originals = new[] { "BananaPeel", "FoamGlove", "RubberChicken", "RocketPlunger", "WhoopeeCushion", "Anvil" }
                .Select(key => AssetDatabase.LoadAssetAtPath<ItemDef>("Assets/_Project/Data/Combat/" + key + ".asset")).ToArray();
            if (originals.Any(item => item == null)) throw new InvalidOperationException("Original six item definitions are missing.");
            var definitions = originals.Concat(catalog.Items).ToArray();
            var spawn = new SerializedObject(spawner);
            spawn.FindProperty("trainingLayout").objectReferenceValue = layout;
            spawn.FindProperty("spawnOpeningLineup").boolValue = true;
            var table = spawn.FindProperty("table");
            var points = spawn.FindProperty("spawnPoints");
            table.arraySize = points.arraySize = definitions.Length;
            var usedPoints = new System.Collections.Generic.HashSet<Transform>();
            for (int index = 0; index < definitions.Length; index++)
            {
                var entry = table.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("Item").objectReferenceValue = definitions[index];
                entry.FindPropertyRelative("Weight").intValue = 1;
                entry.FindPropertyRelative("Capacity").intValue = 1;
                var point = (Transform)points.GetArrayElementAtIndex(index).objectReferenceValue;
                if (point == null || !usedPoints.Add(point))
                {
                    point = new GameObject("Training rack slot " + index).transform;
                    SceneManager.MoveGameObjectToScene(point.gameObject, scene);
                    point.SetParent(spawner.transform, false);
                    usedPoints.Add(point);
                }
                point.position = new Vector3((index - (definitions.Length - 1) * .5f) * layout.ItemSpacing, .65f, 0f);
                points.GetArrayElementAtIndex(index).objectReferenceValue = point;
            }
            spawn.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var scenes = EditorBuildSettings.scenes.Where(entry => entry.path != RetiredPath).ToList();
            if (!scenes.Any(entry => entry.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            if (retired.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
                EditorSceneManager.CloseScene(retired, true);
                wasLoaded = true;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RetiredPath) != null && !AssetDatabase.DeleteAsset(RetiredPath))
                throw new InvalidOperationException("Could not retire NewItemSandbox after backing it up.");
            layout.InstalledRevision = Revision;
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            building = false;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (!wasLoaded && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void Backup(string path)
    {
        if (!File.Exists(path)) return;
        string directory = Path.Combine("Logs", "SandboxBackups");
        Directory.CreateDirectory(directory);
        foreach (string suffix in new[] { "", ".meta" })
        {
            string source = path + suffix;
            string destination = Path.Combine(directory, Path.GetFileName(source));
            if (File.Exists(source) && !File.Exists(destination)) File.Copy(source, destination);
        }
    }

    private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var properties = new SerializedObject(target);
        properties.FindProperty(name).objectReferenceValue = value;
        properties.ApplyModifiedPropertiesWithoutUndo();
    }
}
