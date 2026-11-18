using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;

/// <summary>Imports the Blender export and authors Unity assets without scene rewiring.</summary>
[InitializeOnLoad]
public static class SirNoodleContent
{
    public const string ModelPath = "Assets/_Project/Art/Characters/SirNoodle/SirNoodle.fbx";
    public const string DefinitionPath = "Assets/_Project/Resources/Characters/SirNoodle.asset";
    private const string Art = "Assets/_Project/Art/Characters/SirNoodle";
    private const string Data = "Assets/_Project/Data/Combat/SirNoodle";
    private const string VisualPath = "Assets/_Project/Prefabs/SirNoodleVisual.prefab";
    private const string PrefabPath = "Assets/_Project/Prefabs/SirNoodleFighter.prefab";

    static SirNoodleContent()
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
            (installed != null && installed.Prefab != null && installed.VisualPrefab != null) ||
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            return;
        Build();
    }

    [MenuItem("Wobble Brawl/Characters/Build Sir Noodle")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build Sir Noodle outside Play Mode.");
        EnsureFolder(Data);
        EnsureFolder("Assets/_Project/Resources/Characters");
        EnsureFolder(Art + "/Materials");
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("Sir Noodle Blender FBX is missing.");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = importer.importLights = false;
        importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        var settings = importer.defaultClipAnimations;
        foreach (var clip in settings)
            clip.loopTime = new[] { "Idle", "Walk", "Block", "JumpFall" }.Any(name => clip.name.EndsWith("_" + name));
        importer.clipAnimations = settings;
        importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__") && clip.name.Contains("SirNoodle_"))
            .Select(CopyClip).OrderBy(clip => clip.name).ToArray();
        if (clips.Length != 22)
            throw new InvalidOperationException("Expected 22 Sir Noodle clips, got " + clips.Length);
        var controller = CreateController(clips);
        var body = MakeMaterial("NoodleToon", "BlobbyJelly.mat");
        body.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/SirNoodle_Body_Albedo.png"));
        var outline = MakeMaterial("NoodleOutline", "JellyOutline.mat");
        outline.SetColor("_Outline", new Color(.10f, .07f, .16f));
        outline.SetFloat("_OutlineWidth", .009f);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(outline);
        ConfigureTexture();
        BuildVisual(controller, body, outline);

        // Create the resource definition last, so a failed import retries next domain reload.
        var definition = AssetDatabase.LoadAssetAtPath<CharacterDef>(DefinitionPath);
        bool create = definition == null;
        if (create)
            definition = ScriptableObject.CreateInstance<CharacterDef>();
        definition.DisplayName = "Sir Noodle";
        definition.AnimationPrefix = "SirNoodle";
        definition.VisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
        definition.VisualClips = clips;
        definition.Height = 2f;
        definition.BodyWidth = .48f;
        definition.MaxHealth = 170;
        definition.MaxGuard = 60;
        definition.MoveSpeed = 3.2f;
        definition.JumpSpeed = 10f;
        definition.RagdollReactionScale = 1.25f;
        definition.PartialRagdollSeconds = .6f;
        definition.Punch = Chain("Punch", new[] { 7, 9, 10, 13 }, new[] { 15, 15, 17, 21 }, 9, 5, clips);
        definition.Kick = Chain("Kick", new[] { 10, 13 }, new[] { 18, 21 }, 11, 5, clips);
        definition.Special = GetOrCreate<MoveDef>(Data + "/Special.asset");
        var special = definition.Special;
        special.AnimationTrigger = "Special";
        special.Clip = clips.Single(clip => clip.name == "SirNoodle_Special");
        special.Startup = 12;
        special.Active = 24;
        special.Recovery = 30;
        special.BodyDamage = 7;
        special.LegDamage = 5;
        special.HitIntervalFrames = 6;
        special.AlternateSides = true;
        special.HeavyReaction = true;
        special.StunFrames = 7;
        special.HitStopFrames = 3;
        special.Pushback = .25f;
        special.HitboxOffset = new Vector3(.98f, 1f, 0f);
        special.HitboxSize = new Vector3(2.1f, 1.3f, 1f);
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
        string name = imported.name.Substring(imported.name.LastIndexOf("SirNoodle_", StringComparison.Ordinal));
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
        EditorUtility.SetDirty(copy);
        return copy;
    }

    private static AnimatorController CreateController(AnimationClip[] clips)
    {
        string path = Data + "/SirNoodle.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null)
            return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var graph = controller.layers[0].stateMachine;
        var idle = graph.AddState("Idle", new Vector3(320f, 0f));
        idle.motion = clips.Single(clip => clip.name == "SirNoodle_Idle");
        graph.defaultState = idle;
        int index = 0;
        foreach (var clip in clips.Where(clip => clip.name != "SirNoodle_Idle"))
        {
            string name = clip.name.Substring("SirNoodle_".Length);
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
            visual.name = "SirNoodleVisual";
            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterials = new[] { body, outline };
                renderer.updateWhenOffscreen = true;
            }
            visual.transform.localScale *= 2f / bounds.size.y;
            var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var hand = visual.GetComponentsInChildren<Transform>().Single(bone => bone.name == "Hand.L");
            var socket = new GameObject("Item Socket").transform;
            socket.SetParent(hand, false);
            visual.AddComponent<FighterVisual>().ItemSocket = socket;
            PrefabUtility.SaveAsPrefabAsset(visual, VisualPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(visual); }
    }

    private static void BuildFighter(CharacterDef definition)
    {
        var root = new GameObject("SirNoodleFighter");
        try
        {
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = .24f;
            capsule.center = Vector3.up;
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
            hurt.transform.localPosition = Vector3.up;
            var shape = hurt.AddComponent<BoxCollider>();
            shape.size = new Vector3(.48f, 1.8f, .6f);
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
            move.Clip = clips.Single(clip => clip.name == "SirNoodle_" + name);
            move.Startup = startup[i];
            move.Active = 4;
            move.Recovery = recovery[i];
            move.BodyDamage = damage;
            move.LegDamage = legs;
            move.HeavyReaction = kind == "Kick" || i >= 2;
            move.HitboxOffset = new Vector3(.98f, kind == "Kick" ? .65f : 1.1f, 0);
            move.HitboxSize = new Vector3(2.1f, 1f, 1f);
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
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "/SirNoodle_Body_Albedo.png");
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

internal sealed class SirNoodleImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
        string[] moved, string[] movedFrom)
    {
        if (imported.Contains(SirNoodleContent.ModelPath))
            EditorApplication.delayCall += SirNoodleContent.InstallIfMissing;
    }
}
