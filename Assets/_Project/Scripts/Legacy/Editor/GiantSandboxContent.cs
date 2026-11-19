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

/// <summary>Installs the sandbox in an additive scene without rewriting the owner's open scene.</summary>
[InitializeOnLoad]
public static class GiantSandboxContent
{
    public const string ScenePath = "Assets/_Project/Scenes/GiantSandbox.unity";
    public const string Art = "Assets/_Project/Art/GiantSandbox";
    public const string DefinitionPath = "Assets/_Project/Resources/Stages/GiantSandbox.asset";
    private const string ModelPath = Art + "/GiantSandbox.fbx";
    private const string TemplatePath = "Assets/_Project/Scenes/CombatSandbox.unity";

    static GiantSandboxContent()
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

    [MenuItem("Wobble Brawl/Stages/Build Giant Sandbox")]
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
            throw new InvalidOperationException("An incomplete sandbox scene exists. Preserve it and inspect the import error before retrying.");
        }
        Folder(Art + "/Materials");
        Folder("Assets/_Project/Resources/Stages");
        var tuning = ScriptableObject.CreateInstance<GiantSandboxDef>();
        var existing = AssetDatabase.LoadAssetAtPath<GiantSandboxDef>(Art + "/SandboxTuning.asset");
        if (existing != null)
        {
            Object.DestroyImmediate(tuning);
            tuning = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(tuning, Art + "/SandboxTuning.asset");
        }
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        modelImporter.importAnimation = false;
        modelImporter.addCollider = false;
        modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
        modelImporter.SaveAndReimport();
        ConfigureTexture(Art + "/GiantSandbox_Atlas.png", 1024);
        ConfigureTexture(Art + "/GiantSandbox_Thumbnail.png", 1024);
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/GiantSandbox_Atlas.png");
        if (atlas == null)
        {
            throw new InvalidOperationException("The sandbox atlas must import before building the stage.");
        }
        var body = Material("SandboxToon", "Assets/_Project/Art/Items/ToonPickupSurface.shadergraph");
        body.SetColor("_BaseColor", Color.white);
        body.SetTexture("_BaseMap", atlas);
        EditorUtility.SetDirty(body);
        var outline = Material("SandboxOutline", "Assets/_Project/Art/Characters/Blobby/Materials/JellyOutline.mat");
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
            model.name = "Giant Sandbox Environment";
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterials = new[] { body, outline };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            var hazard = new GameObject("Giant Sandbox Hazard Cycle").AddComponent<HazardController>();
            var sandbox = hazard.gameObject.AddComponent<GiantSandboxHazards>();
            var cycle = ScriptableObject.CreateInstance<HazardDef>();
            cycle.FirstWarningDelay = 15f;
            cycle.WarningSeconds = 2f;
            cycle.ActiveSeconds = 4f;
            cycle.CooldownSeconds = 2f;
            cycle.IdleSeconds = new Vector2(8f, 14f);
            AssetDatabase.CreateAsset(cycle, Art + "/SandboxCycle.asset");
            Set(hazard, "definition", cycle);
            Set(sandbox, "definition", tuning);
            Array(sandbox, "fighters", new[] { round.Player, round.Opponent });
            var transforms = model.GetComponentsInChildren<Transform>();
            foreach (string name in new[] { "Paw", "Shadow", "Whiskers", "Spiral" })
            {
                var visual = transforms.Single(part => part.name == "SM_" + name);
                Set(sandbox, name.ToLowerInvariant(), visual);
                visual.gameObject.SetActive(false);
            }
            var hit = ScriptableObject.CreateInstance<MoveDef>();
            hit.BodyDamage = hit.LegDamage = hit.GuardCost = hit.GaugeGain = hit.HitStopFrames = 0;
            hit.StunFrames = 30;
            hit.Pushback = 3.5f;
            hit.HeavyReaction = true;
            hit.KnocksDown = true;
            AssetDatabase.CreateAsset(hit, Art + "/PawSweepHit.asset");
            Set(sandbox, "pawHit", hit);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                var wall = new GameObject("Sandbox edge collider");
                wall.layer = 8;
                wall.transform.SetParent(model.transform, false);
                wall.transform.position = new Vector3(sign * (tuning.HalfWidth + .25f), 8f, 0f);
                wall.AddComponent<BoxCollider>().size = new Vector3(.5f, 16f, 4.8f);
            }
            var audio = hazard.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.volume = .65f;
            hazard.gameObject.AddComponent<SettingsAudioSource>();
            Set(sandbox, "audioSource", audio);
            Set(sandbox, "purr", Cue("SandPurr"));
            Set(sandbox, "swirl", Cue("SandSwirl"));
            Set(sandbox, "thump", Cue("SandThump"));
            var ambience = new GameObject("Backyard ambience");
            var ambientAudio = ambience.AddComponent<AudioSource>();
            ambientAudio.clip = Cue("SandAmbience");
            ambientAudio.playOnAwake = true;
            ambientAudio.loop = true;
            ambientAudio.volume = .15f;
            ambience.AddComponent<SettingsAudioSource>();
            ConfigureDust(model.transform, body);
            Set(round, "hazard", hazard);
            var spawnSettings = new SerializedObject((ItemSpawner)new SerializedObject(round).FindProperty("itemSpawner").objectReferenceValue);
            spawnSettings.FindProperty("trainingLayout").objectReferenceValue = null;
            spawnSettings.ApplyModifiedPropertiesWithoutUndo();
            ConfigureItems(properties, tuning);
            EditorSceneManager.SaveScene(scene);
            RegisterScene();
            // Publish the resource last; Boot discovers only a completed, enabled arena.
            var entry = ScriptableObject.CreateInstance<StageDef>();
            entry.Id = "giant-sandbox";
            entry.DisplayName = "Giant Sandbox";
            entry.SceneName = "GiantSandbox";
            entry.HazardHint = "Cat paw sweep · Sinking sand";
            entry.Thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/GiantSandbox_Thumbnail.png");
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

    private static void ConfigureDust(Transform parent, Material material)
    {
        string path = Art + "/SandGrain.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            var temporary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh = Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
            mesh.name = "Sand grain";
            mesh.uv = Enumerable.Repeat(new Vector2(.625f, .125f), mesh.vertexCount).ToArray();
            AssetDatabase.CreateAsset(mesh, path);
            Object.DestroyImmediate(temporary);
        }
        var node = new GameObject("Background sand motes");
        node.transform.SetParent(parent, false);
        node.transform.localPosition = new Vector3(0f, .8f, 3.8f);
        var particles = node.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = 8f;
        main.startSize = new ParticleSystem.MinMaxCurve(.018f, .04f);
        main.startSpeed = .1f;
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission;
        emission.rateOverTime = 1f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(14f, .8f, .5f);
        var renderer = node.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void ConfigureItems(SerializedObject round, GiantSandboxDef tuning)
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
            var point = new GameObject("Sandbox item spawn " + index).transform;
            point.SetParent(spawner.transform, false);
            point.position = tuning.ItemPoints[index];
            points.GetArrayElementAtIndex(index).objectReferenceValue = point;
        }
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AudioClip Cue(string name)
    {
        return Resources.Load<AudioClip>("Audio/" + name) ??
            throw new InvalidOperationException("Missing original sandbox audio: " + name);
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
            throw new InvalidOperationException("Import the shared toon/outline shaders before building the sandbox.");
        }
        result = original != null ? new Material(original) : new Material(shader);
        AssetDatabase.CreateAsset(result, path);
        return result;
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

internal sealed class SandboxImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Contains(GiantSandboxContent.Art + "/GiantSandbox.fbx"))
        {
            EditorApplication.delayCall += GiantSandboxContent.Install;
        }
    }
}
