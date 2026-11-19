using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WobbleBrawl.Core;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;
using Object = UnityEngine.Object;

/// <summary>Installs the fairground in an additive scene without rewriting the owner's open scene.</summary>
[InitializeOnLoad]
public static class BouncyCastleContent
{
    public const string ScenePath = "Assets/_Project/Scenes/BouncyCastleFair.unity";
    public const string Art = "Assets/_Project/Art/BouncyCastleFair";
    public const string DefinitionPath = "Assets/_Project/Resources/Stages/BouncyCastleFair.asset";
    private const string ModelPath = Art + "/BouncyCastleFair.fbx";
    private const string TemplatePath = "Assets/_Project/Scenes/CombatSandbox.unity";

    static BouncyCastleContent()
    {
        EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += Install;
            }
        };
    }

    internal static void Install()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Install;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
        {
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<StageDef>(DefinitionPath) != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            RegisterScene();
            return;
        }
        Build();
    }

    [MenuItem("Wobble Brawl/Stages/Build Bouncy Castle Fair")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Build stages outside Play Mode.");
        }
        if (AssetDatabase.LoadAssetAtPath<StageDef>(DefinitionPath) != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            RegisterScene();
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            throw new InvalidOperationException("An incomplete fairground scene exists. Preserve it and inspect the import error before retrying.");
        }
        Folder(Art + "/Materials");
        Folder("Assets/_Project/Resources/Stages");
        var tuning = ScriptableObject.CreateInstance<FairgroundDef>();
        var existing = AssetDatabase.LoadAssetAtPath<FairgroundDef>(Art + "/FairgroundTuning.asset");
        if (existing != null)
        {
            Object.DestroyImmediate(tuning);
            tuning = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(tuning, Art + "/FairgroundTuning.asset");
        }
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        modelImporter.importAnimation = false;
        modelImporter.addCollider = false;
        modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
        modelImporter.SaveAndReimport();
        ConfigureTexture(Art + "/BouncyCastle_Atlas.png", 1024);
        ConfigureTexture(Art + "/BouncyCastle_Thumbnail.png", 1024);
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/BouncyCastle_Atlas.png");
        if (atlas == null)
        {
            throw new InvalidOperationException("The fairground atlas must import before building the stage.");
        }
        var body = Material("CastleToon", "Assets/_Project/Art/Items/ToonPickupSurface.shadergraph");
        body.SetColor("_BaseColor", Color.white);
        body.SetTexture("_BaseMap", atlas);
        EditorUtility.SetDirty(body);
        var outline = Material("CastleOutline", "Assets/_Project/Art/Characters/Blobby/Materials/JellyOutline.mat");
        outline.SetColor("_Outline", new Color(.10f, .11f, .19f));
        outline.SetFloat("_OutlineWidth", .025f);
        EditorUtility.SetDirty(outline);
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = default;
        try
        {
            if (!AssetDatabase.CopyAsset(TemplatePath, ScenePath))
            {
                throw new InvalidOperationException("The CombatSandbox template could not be copied.");
            }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var roots = scene.GetRootGameObjects();
            var round = roots.Select(root => root.GetComponent<RoundController>()).Single(value => value != null);
            var floor = roots.Single(root => root.layer == 8 && root.GetComponent<BoxCollider>() != null);
            var camera = roots.Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var training = round.GetComponent<TrainingSandbox>();
            if (training != null) Object.DestroyImmediate(training);
            var properties = new SerializedObject(round);
            properties.FindProperty("trainingMode").boolValue = false;
            properties.ApplyModifiedPropertiesWithoutUndo();
            var floorScale = floor.transform.localScale;
            floorScale.x = 14f;
            floor.transform.localScale = floorScale;
            var old = (HazardController)properties.FindProperty("hazard").objectReferenceValue;
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }
            floor.GetComponent<Renderer>().enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.13f, .16f, .25f);
            camera.transform.position = new Vector3(0f, 3.7f, -10f);
            camera.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
            camera.orthographicSize = 4.8f;
            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
            var model = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), scene);
            model.name = "Bouncy Castle Fair Environment";
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterials = new[] { body, outline };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            var hazard = new GameObject("Fairground Hazard Cycle").AddComponent<HazardController>();
            var fairground = hazard.gameObject.AddComponent<FairgroundHazards>();
            var cycle = ScriptableObject.CreateInstance<HazardDef>();
            cycle.FirstWarningDelay = 15f;
            cycle.WarningSeconds = 2f;
            cycle.ActiveSeconds = 4f;
            cycle.CooldownSeconds = 2f;
            cycle.IdleSeconds = new Vector2(8f, 14f);
            AssetDatabase.CreateAsset(cycle, Art + "/FairgroundCycle.asset");
            Set(hazard, "definition", cycle);
            Set(fairground, "definition", tuning);
            var fighters = new[] { round.Player, round.Opponent };
            Array(fairground, "fighters", fighters);
            var walls = new Rigidbody[2];
            var wallVisuals = new Transform[2];
            var transforms = model.GetComponentsInChildren<Transform>();
            for (int index = 0; index < walls.Length; index++)
            {
                string name = index == 0 ? "SM_LeftWall" : "SM_RightWall";
                var visual = transforms.Single(part => part.name == name);
                var wall = new GameObject(name + " Physics");
                wall.layer = 8;
                wall.transform.SetParent(model.transform, false);
                wall.transform.position = visual.position;
                visual.SetParent(wall.transform, true);
                var collider = wall.AddComponent<BoxCollider>();
                collider.center = Vector3.up * 10f;
                collider.size = new Vector3(1f, 20f, 4.8f);
                walls[index] = wall.AddComponent<Rigidbody>();
                walls[index].isKinematic = true;
                walls[index].useGravity = false;
                walls[index].interpolation = RigidbodyInterpolation.Interpolate;
                wallVisuals[index] = visual;
            }
            Array(fairground, "walls", walls);
            Array(fairground, "wallVisuals", wallVisuals);
            var arrow = ArrowMesh();
            var pads = new Transform[tuning.PadCenters.Length];
            var warnings = new Transform[2];
            for (int index = 0; index < pads.Length; index++)
            {
                pads[index] = Marker(hazard.transform, "BOING pad warning", arrow, body, outline,
                    new Vector3(tuning.PadCenters[index], .35f, -.85f));
                pads[index].localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            for (int index = 0; index < warnings.Length; index++)
            {
                float direction = index == 0 ? -1f : 1f;
                warnings[index] = Marker(walls[index].transform, "Inward wall warning", arrow, body, outline,
                    new Vector3(-direction * 1f, 1.7f, -.9f));
                warnings[index].localRotation = Quaternion.Euler(0f, index == 0 ? 0f : 180f, 0f);
            }
            Array(fairground, "padWarnings", pads);
            Array(fairground, "wallWarnings", warnings);
            var audio = hazard.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.volume = .65f;
            hazard.gameObject.AddComponent<SettingsAudioSource>();
            Set(fairground, "audioSource", audio);
            Set(fairground, "padWarning", Cue("FairPadWarning"));
            Set(fairground, "pumpWarning", Cue("FairPump"));
            Set(fairground, "boing", Cue("FairBoing"));
            var ambience = new GameObject("Fairground ambience");
            var ambientAudio = ambience.AddComponent<AudioSource>();
            ambientAudio.clip = Cue("FairAmbience");
            ambientAudio.playOnAwake = true;
            ambientAudio.loop = true;
            ambientAudio.volume = .15f;
            ambience.AddComponent<SettingsAudioSource>();
            var frame = camera.gameObject.AddComponent<FairgroundCamera>();
            Set(frame, "fightCamera", camera);
            Set(frame, "round", round);
            Set(frame, "hazards", fairground);
            Set(frame, "definition", tuning);
            Set(round, "hazard", hazard);
            var spawnSettings = new SerializedObject((ItemSpawner)new SerializedObject(round).FindProperty("itemSpawner").objectReferenceValue);
            spawnSettings.FindProperty("trainingLayout").objectReferenceValue = null;
            spawnSettings.ApplyModifiedPropertiesWithoutUndo();
            ConfigureItems(properties, tuning);
            EditorSceneManager.SaveScene(scene);
            RegisterScene();
            // Publish the resource last; Boot discovers only a completed, enabled arena.
            var entry = ScriptableObject.CreateInstance<StageDef>();
            entry.Id = "bouncy-castle-fair";
            entry.DisplayName = "Bouncy Castle Fair";
            entry.SceneName = "BouncyCastleFair";
            entry.HazardHint = "Trampoline pads · Wall squeeze";
            entry.Thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/BouncyCastle_Thumbnail.png");
            AssetDatabase.CreateAsset(entry, DefinitionPath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void ConfigureItems(SerializedObject round, FairgroundDef tuning)
    {
        var spawner = (ItemSpawner)round.FindProperty("itemSpawner").objectReferenceValue;
        if (spawner == null)
        {
            throw new InvalidOperationException("The stage template requires the pooled item spawner.");
        }
        var properties = new SerializedObject(spawner);
        properties.FindProperty("spawnOpeningLineup").boolValue = false;
        properties.FindProperty("spawnAtRoundStart").boolValue = false;
        var points = properties.FindProperty("spawnPoints");
        points.arraySize = tuning.ItemPoints.Length;
        for (int index = 0; index < points.arraySize; index++)
        {
            var point = new GameObject("Fair item spawn " + index).transform;
            point.SetParent(spawner.transform, false);
            point.position = tuning.ItemPoints[index];
            points.GetArrayElementAtIndex(index).objectReferenceValue = point;
        }
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AudioClip Cue(string name)
    {
        return Resources.Load<AudioClip>("Audio/" + name) ??
            throw new InvalidOperationException("Missing original fairground audio: " + name);
    }

    private static void RegisterScene()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        var found = scenes.FirstOrDefault(scene => scene.path == ScenePath);
        if (found == null)
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        }
        else
        {
            found.enabled = true;
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Material Material(string name, string source)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result != null)
        {
            return result;
        }
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(source);
        var original = AssetDatabase.LoadAssetAtPath<Material>(source);
        var requiredShader = original != null ? original.shader : shader;
        if (requiredShader == null || ShaderUtil.ShaderHasError(requiredShader))
        {
            throw new InvalidOperationException("Import the shared toon/outline shaders before building the fairground.");
        }
        result = original != null ? new Material(original) : new Material(shader);
        AssetDatabase.CreateAsset(result, path);
        return result;
    }

    private static Transform Marker(Transform parent, string name, Mesh mesh, Material body,
        Material outline, Vector3 position)
    {
        var node = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        node.transform.SetParent(parent, false);
        node.transform.localPosition = position;
        node.GetComponent<MeshFilter>().sharedMesh = mesh;
        node.GetComponent<MeshRenderer>().sharedMaterials = new[] { body, outline };
        node.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        node.SetActive(false);
        return node.transform;
    }

    private static Mesh ArrowMesh()
    {
        string path = Art + "/WarningArrow.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null)
        {
            return mesh;
        }
        mesh = new Mesh { name = "Fairground warning arrow" };
        var face = new[]
        {
            new Vector3(-.6f, -.12f, 0f), new Vector3(.05f, -.12f, 0f),
            new Vector3(.05f, -.4f, 0f), new Vector3(.6f, 0f, 0f),
            new Vector3(.05f, .4f, 0f), new Vector3(.05f, .12f, 0f), new Vector3(-.6f, .12f, 0f)
        };
        var vertices = new Vector3[14];
        for (int index = 0; index < face.Length; index++)
        {
            vertices[index] = face[index] + Vector3.back * .025f;
            vertices[index + face.Length] = face[index] + Vector3.forward * .025f;
        }
        mesh.vertices = vertices;
        var triangles = new System.Collections.Generic.List<int> { 0, 6, 5, 0, 5, 1, 2, 4, 3 };
        int front = triangles.Count;
        for (int index = 0; index < front; index += 3)
        {
            triangles.Add(triangles[index] + 7);
            triangles.Add(triangles[index + 2] + 7);
            triangles.Add(triangles[index + 1] + 7);
        }
        for (int index = 0; index < face.Length; index++)
        {
            int next = (index + 1) % face.Length;
            triangles.AddRange(new[] { index, next, next + 7, index, next + 7, index + 7 });
        }
        mesh.triangles = triangles.ToArray();
        mesh.uv = Enumerable.Repeat(new Vector2(.625f, .125f), vertices.Length).ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void Set(Object target, string name, Object value)
    {
        var properties = new SerializedObject(target);
        properties.FindProperty(name).objectReferenceValue = value;
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Array(Object target, string name, Object[] values)
    {
        var properties = new SerializedObject(target);
        var array = properties.FindProperty(name);
        array.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        int slash = path.LastIndexOf('/');
        Folder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static void ConfigureTexture(string path, int size)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.maxTextureSize = size;
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.format = TextureImporterFormat.ASTC_6x6;
        android.maxTextureSize = size;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
    }
}

internal sealed class FairgroundImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Contains(BouncyCastleContent.Art + "/BouncyCastleFair.fbx"))
        {
            EditorApplication.delayCall += BouncyCastleContent.Install;
        }
    }
}
