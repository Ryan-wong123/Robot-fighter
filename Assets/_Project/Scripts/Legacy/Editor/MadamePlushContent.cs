using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;

/// <summary>Imports the Blender export and authors Unity assets without scene rewiring.</summary>
[InitializeOnLoad]
public static class MadamePlushContent
{
    public const string ModelPath = "Assets/_Project/Art/Characters/MadamePlush/MadamePlush.fbx";
    public const string DefinitionPath = "Assets/_Project/Resources/Characters/MadamePlush.asset";
    private const string Art = "Assets/_Project/Art/Characters/MadamePlush";
    private const string Data = "Assets/_Project/Data/Combat/MadamePlush";
    private const string VisualPath = "Assets/_Project/Prefabs/MadamePlushVisual.prefab";
    private const string PrefabPath = "Assets/_Project/Prefabs/MadamePlushFighter.prefab";

    static MadamePlushContent()
    {
        EditorApplication.delayCall += InstallIfMissing;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += InstallIfMissing;
        };
    }

    internal static void InstallIfMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += InstallIfMissing;
            return;
        }
        var installed = AssetDatabase.LoadAssetAtPath<CharacterDef>(DefinitionPath);
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            return;
        if (installed != null && installed.Prefab != null && installed.VisualPrefab != null)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/PlushToon.mat");
            if (material != null && material.GetTexture("_BaseMap") == null)
                RepairColors();
            return;
        }
        Build();
    }

    [MenuItem("Wobble Brawl/Characters/Repair Madame Plush Colors")]
    public static void RepairColors()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        ConfigureTexture();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/PlushToon.mat");
        BindAtlas(material);
        AssetDatabase.SaveAssets();
    }

    private static void BindAtlas(Material material)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/MadamePlush_Body_Albedo.png");
        if (texture == null || material == null)
            throw new InvalidOperationException("Madame Plush requires its imported 2D atlas and toon material.");
        material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        EditorUtility.SetDirty(material);
    }

    [MenuItem("Wobble Brawl/Characters/Build Madame Plush")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build Madame Plush outside Play Mode.");
        EnsureFolder(Data);
        EnsureFolder("Assets/_Project/Resources/Characters");
        EnsureFolder(Art + "/Materials");
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("Madame Plush Blender FBX is missing.");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importBlendShapes = true;
        importer.importBlendShapes = true;
        importer.importCameras = importer.importLights = false;
        importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        var settings = importer.defaultClipAnimations;
        foreach (var clip in settings)
            clip.loopTime = new[] { "Idle", "Walk", "Block", "JumpFall" }.Any(name => clip.name.EndsWith("_" + name));
        importer.clipAnimations = settings;
        importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__") && clip.name.Contains("MadamePlush_"))
            .Select(CopyClip).OrderBy(clip => clip.name).ToArray();
        if (clips.Length != 22)
            throw new InvalidOperationException("Expected 22 Madame Plush clips, got " + clips.Length);
        var controller = CreateController(clips);
        ConfigureTexture();
        var body = MakeMaterial("PlushToon", "BlobbyJelly.mat");
        BindAtlas(body);
        var outline = MakeMaterial("PlushOutline", "JellyOutline.mat");
        outline.SetColor("_Outline", new Color(.10f, .035f, .02f));
        outline.SetFloat("_OutlineWidth", .012f);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(outline);
        BuildVisual(controller, body, outline);

        // Create the resource definition last, so a failed import retries next domain reload.
        var definition = AssetDatabase.LoadAssetAtPath<CharacterDef>(DefinitionPath);
        bool create = definition == null;
        if (create)
            definition = ScriptableObject.CreateInstance<CharacterDef>();
        definition.DisplayName = "Madame Plush";
        definition.AnimationPrefix = "MadamePlush";
        definition.VisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
        definition.VisualClips = clips;
        definition.Height = 1.8f;
        definition.BodyWidth = .86f;
        definition.MaxHealth = 240;
        definition.MaxGuard = 100;
        definition.MoveSpeed = 2.4f;
        definition.JumpSpeed = 9f;
        definition.RagdollReactionScale = .85f;
        definition.PartialRagdollSeconds = .4f;
        definition.HasStuffingLoss = true;
        definition.LowHealthDamageBonus = 10;
        definition.Punch = Chain("Punch", new[] { 9, 12, 14, 18 }, new[] { 19, 22, 24, 28 }, 12, 6, clips);
        definition.Kick = Chain("Kick", new[] { 14, 18 }, new[] { 24, 28 }, 14, 7, clips);
        definition.Special = GetOrCreate<MoveDef>(Data + "/Special.asset");
        var special = definition.Special;
        special.AnimationTrigger = "Special";
        special.Clip = clips.Single(clip => clip.name == "MadamePlush_Special");
        special.Startup = 24;
        special.Active = 5;
        special.Recovery = 35;
        special.BodyDamage = 24;
        special.LegDamage = 12;
        special.HitIntervalFrames = 0;
        special.GuardCost = 25;
        special.LaunchSpeed = 7f;
        special.LaunchBounds = new Vector2(-6.2f, 6.2f);
        special.LaunchContainmentFrames = 90;
        special.AlternateSides = false;
        special.HeavyReaction = true;
        special.StunFrames = 18;
        special.HitStopFrames = 8;
        special.Pushback = 8f;
        special.HitboxOffset = new Vector3(.63f, 1.15f, 0f);
        special.HitboxSize = new Vector3(1.35f, 1.5f, 1f);
        EditorUtility.SetDirty(special);
        if (create)
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        BuildFighter(definition);
        definition.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip CopyClip(AnimationClip imported)
    {
        string name = imported.name.Substring(imported.name.LastIndexOf("MadamePlush_", StringComparison.Ordinal));
        string path = Data + "/" + name + ".anim";
        var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (copy == null)
        {
            copy = UnityEngine.Object.Instantiate(imported);
            AssetDatabase.CreateAsset(copy, path);
        }
        else
            EditorUtility.CopySerialized(imported, copy);
        copy.name = name;
        // Stuffing is driven by health, so exported rest curves must not close damaged seams.
        foreach (var binding in AnimationUtility.GetCurveBindings(copy))
            if (binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal))
                AnimationUtility.SetEditorCurve(copy, binding, null);
        EditorUtility.SetDirty(copy);
        return copy;
    }

    private static AnimatorController CreateController(AnimationClip[] clips)
    {
        string path = Data + "/MadamePlush.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null)
            return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var graph = controller.layers[0].stateMachine;
        var idle = graph.AddState("Idle", new Vector3(320f, 0f));
        idle.motion = clips.Single(clip => clip.name == "MadamePlush_Idle");
        graph.defaultState = idle;
        int index = 0;
        foreach (var clip in clips.Where(clip => clip.name != "MadamePlush_Idle"))
        {
            string name = clip.name.Substring("MadamePlush_".Length);
            var state = graph.AddState(name, new Vector3(320f + index % 4 * 220f, 120f + index / 4 * 100f));
            index++;
            state.motion = clip;
            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
            var enter = graph.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0, name);
            enter.hasExitTime = false;
            enter.duration = .05f;
            enter.canTransitionToSelf = false;
            var exit = state.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 1f;
            exit.duration = .08f;
        }
        return controller;
    }

    private static void BuildVisual(AnimatorController controller, Material body, Material outline)
    {
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        try
        {
            visual.name = "MadamePlushVisual";
            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterials = new[] { body, outline };
                renderer.updateWhenOffscreen = true;
            }
            visual.transform.localScale *= 1.8f / bounds.size.y;
            var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var hand = visual.GetComponentsInChildren<Transform>().Single(bone => bone.name == "Hand.L");
            var socket = new GameObject("Item Socket").transform;
            socket.SetParent(hand, false);
            socket.position = visual.transform.position + new Vector3(.70f, .65f, .21f);
            visual.AddComponent<FighterVisual>().ItemSocket = socket;
            ConfigureStuffing(visual, renderers[0]);
            PrefabUtility.SaveAsPrefabAsset(visual, VisualPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(visual); }
    }

    private static void ConfigureStuffing(GameObject visual, SkinnedMeshRenderer skin)
    {
        if (skin.sharedMesh.blendShapeCount < 3)
            throw new InvalidOperationException("Madame Plush needs three stuffing morphs.");
        var feedback = visual.AddComponent<StuffingVisual>();
        Set(feedback, "skin", skin);
        var bones = visual.GetComponentsInChildren<Transform>();
        var positions = new[] { new Vector3(-.19f, .96f, .36f), new Vector3(.39f, 1.04f, .28f), new Vector3(-.18f, .44f, .28f) };
        var names = new[] { "Spine", "Upper_Arm.L", "Pelvis" };
        var properties = new SerializedObject(feedback);
        var anchors = properties.FindProperty("anchors");
        anchors.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            var anchor = new GameObject("Stuffing Anchor " + i).transform;
            anchor.SetParent(bones.Single(bone => bone.name == names[i]), false);
            anchor.position = visual.transform.position + positions[i];
            anchors.GetArrayElementAtIndex(i).objectReferenceValue = anchor;
        }
        properties.ApplyModifiedPropertiesWithoutUndo();
        var fx = new GameObject("Cotton Puff");
        fx.transform.SetParent(visual.transform, false);
        var particles = fx.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = .6f;
        main.startLifetime = .6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(.8f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(.08f, .15f);
        main.startColor = new Color(1f, .96f, .86f);
        main.gravityModifier = .3f;
        main.maxParticles = 16;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .04f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        string meshPath = Art + "/CottonPuff.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = "Cotton Puff" };
            mesh.vertices = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            mesh.triangles = new[] { 0,4,3,0,3,5,0,5,2,0,2,4,1,3,4,1,5,3,1,2,5,1,4,2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }
        renderer.mesh = mesh;
        string materialPath = Art + "/Materials/CottonPuff.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        renderer.sharedMaterial = material;
        Set(feedback, "cotton", particles);
        Set(feedback, "clothAudio", MakeAudio(fx, "PlushCreak"));
        Set(feedback, "winAudio", MakeAudio(fx, "PlushWinBell"));
    }

    private static AudioSource MakeAudio(GameObject root, string clip)
    {
        var node = new GameObject(clip);
        node.transform.SetParent(root.transform, false);
        var audio = node.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = Resources.Load<AudioClip>("Audio/" + clip);
        if (audio.clip == null)
            throw new InvalidOperationException("Import the original " + clip + " audio cue before building Madame Plush.");
        node.AddComponent<WobbleBrawl.Core.SettingsAudioSource>();
        return audio;
    }

    private static void BuildFighter(CharacterDef definition)
    {
        var root = new GameObject("MadamePlushFighter");
        try
        {
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = .43f;
            capsule.center = Vector3.up * .9f;
            var fighter = root.AddComponent<Fighter>();
            Set(fighter, "character", definition);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(definition.VisualPrefab, root.transform);
            Set(fighter, "animator", visual.GetComponent<Animator>());
            var props = new SerializedObject(fighter);
            var clips = props.FindProperty("visualClips");
            clips.arraySize = definition.VisualClips.Length;
            for (int i = 0; i < clips.arraySize; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue = definition.VisualClips[i];
            props.ApplyModifiedPropertiesWithoutUndo();
            var hurt = new GameObject("Body Hurtbox");
            hurt.layer = 9;
            hurt.transform.SetParent(root.transform, false);
            hurt.transform.localPosition = Vector3.up * .9f;
            var shape = hurt.AddComponent<BoxCollider>();
            shape.size = new Vector3(.86f, 1.6f, .6f);
            shape.isTrigger = true;
            Set(hurt.AddComponent<Hurtbox>(), "owner", fighter);
            var squash = root.AddComponent<SquashStretch>();
            Set(squash, "fighter", fighter);
            Set(squash, "visual", visual.transform);
            Set(root.AddComponent<ItemHolder>(), "handSocket", visual.GetComponent<FighterVisual>().ItemSocket);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static MoveDef Chain(string kind, int[] startup, int[] recovery, int damage, int legs, AnimationClip[] clips)
    {
        MoveDef next = null;
        for (int i = startup.Length - 1; i >= 0; i--)
        {
            string name = kind + (i + 1);
            var move = GetOrCreate<MoveDef>(Data + "/" + name + ".asset");
            move.AnimationTrigger = name;
            move.Clip = clips.Single(clip => clip.name == "MadamePlush_" + name);
            move.Startup = startup[i];
            move.Active = 4;
            move.Recovery = recovery[i];
            move.BodyDamage = damage;
            move.LegDamage = legs;
            move.HeavyReaction = kind == "Kick" || i >= 2;
            move.HitboxOffset = new Vector3(.63f, kind == "Kick" ? .4f : 1f, 0);
            move.HitboxSize = new Vector3(1.35f, 1f, 1f);
            move.NextInCombo = next;
            EditorUtility.SetDirty(move);
            next = move;
        }
        return next;
    }

    private static Material MakeMaterial(string name, string template)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/_Project/Art/Characters/Blobby/Materials/" + template));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void ConfigureTexture()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "/MadamePlush_Body_Albedo.png");
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.maxTextureSize = 1024;
        importer.mipmapEnabled = true;
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.format = TextureImporterFormat.ASTC_6x6;
        android.maxTextureSize = 1024;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
    }

    private static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
        var result = AssetDatabase.LoadAssetAtPath<T>(path);
        if (result != null) return result;
        result = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(result, path);
        return result;
    }

    private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var properties = new SerializedObject(target);
        properties.FindProperty(name).objectReferenceValue = value;
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}

internal sealed class MadamePlushImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
        string[] moved, string[] movedFrom)
    {
        if (imported.Contains(MadamePlushContent.ModelPath))
            EditorApplication.delayCall += MadamePlushContent.InstallIfMissing;
    }
}
