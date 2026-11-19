using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;

/// <summary>Imports the Blender export and authors Unity assets without scene rewiring.</summary>
[InitializeOnLoad]
public static class MrJackContent
{
    public const string ModelPath = "Assets/_Project/Art/Characters/MrJack/MrJack.fbx";
    public const string DefinitionPath = "Assets/_Project/Resources/Characters/MrJack.asset";
    private static bool installing;
    private const int Revision = 4;
    private const string Art = "Assets/_Project/Art/Characters/MrJack";
    private const string Data = "Assets/_Project/Data/Combat/MrJack";
    private const string VisualPath = "Assets/_Project/Prefabs/MrJackVisual.prefab";
    private const string PrefabPath = "Assets/_Project/Prefabs/MrJackFighter.prefab";

    static MrJackContent()
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
        if (installing) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += InstallIfMissing;
            return;
        }
        var installed = AssetDatabase.LoadAssetAtPath<CharacterDef>(DefinitionPath);
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            return;
        if (installed != null && installed.Prefab != null && installed.VisualPrefab != null && installed.Boss != null && installed.Boss.InstalledRevision == Revision)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/JackToon.mat");
            if (material != null && material.GetTexture("_BaseMap") == null)
                RepairColors();
            return;
        }
        installing = true;
        try { Build(); }
        finally { installing = false; }
    }

    [MenuItem("Wobble Brawl/Characters/Repair Mr. Jack Colors")]
    public static void RepairColors()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        ConfigureTexture();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/JackToon.mat");
        BindAtlas(material);
        AssetDatabase.SaveAssets();
    }

    private static void BindAtlas(Material material)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/MrJack_Body_Albedo.png");
        if (texture == null || material == null)
            throw new InvalidOperationException("Mr. Jack requires its imported 2D atlas and toon material.");
        material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        EditorUtility.SetDirty(material);
    }

    [MenuItem("Wobble Brawl/Characters/Build Mr. Jack")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build Mr. Jack outside Play Mode.");
        EnsureFolder(Data);
        EnsureFolder("Assets/_Project/Resources/Characters");
        EnsureFolder(Art + "/Materials");
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("Mr. Jack Blender FBX is missing.");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importBlendShapes = false;
        importer.isReadable = true;
        importer.importCameras = importer.importLights = false;
        importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        var settings = importer.defaultClipAnimations;
        foreach (var clip in settings)
            clip.loopTime = new[] { "Idle", "Walk", "Block", "JumpFall", "OutIdle", "OutWalk", "OutBlock", "OutJumpFall" }.Any(name => clip.name.EndsWith("_" + name) || clip.name.EndsWith("_Out" + name));
        importer.clipAnimations = settings;
        importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__") && clip.name.Contains("MrJack_"))
            .Select(CopyClip).OrderBy(clip => clip.name).ToArray();
        if (clips.Length != 45)
            throw new InvalidOperationException("Expected 45 Mr. Jack clips, got " + clips.Length);
        var controller = CreateController(clips);
        ConfigureTexture();
        var body = MakeMaterial("JackToon", "BlobbyJelly.mat");
        BindAtlas(body);
        var outline = MakeMaterial("JackOutline", "JellyOutline.mat");
        outline.SetColor("_Outline", new Color(.09f, .02f, .14f));
        outline.SetFloat("_OutlineWidth", .012f);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(outline);
        BuildVisual(controller, body, outline);

        // Create the resource definition last, so a failed import retries next domain reload.
        var definition = AssetDatabase.LoadAssetAtPath<CharacterDef>(DefinitionPath);
        bool create = definition == null;
        if (create)
            definition = ScriptableObject.CreateInstance<CharacterDef>();
        definition.DisplayName = "Mr. Jack";
        definition.AnimationPrefix = "MrJack";
        definition.VisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
        definition.VisualClips = clips;
        definition.Height = 2.3f;
        definition.BodyWidth = 1.05f;
        definition.MaxHealth = 320;
        definition.MaxGuard = 120;
        definition.MoveSpeed = 2.8f;
        definition.JumpSpeed = 12f;
        definition.RagdollReactionScale = .85f;
        definition.PartialRagdollSeconds = .4f;
        definition.Punch = Chain("Punch", new[] { 21, 24, 27, 30 }, new[] { 26, 27, 28, 29 }, 11, 6, clips);
        definition.Kick = Chain("Kick", new[] { 27, 30 }, new[] { 20, 21 }, 13, 7, clips);
        definition.Special = Special("Special", clips);
        definition.Boss = GetOrCreate<BossCombatDef>(Data + "/BossCombat.asset");
        definition.Boss.SecondPunch = Chain("OutPunch", new[] { 14, 17, 20, 23 }, new[] { 23, 24, 25, 26 }, 11, 6, clips);
        definition.Boss.SecondKick = Chain("OutKick", new[] { 20, 23 }, new[] { 17, 18 }, 13, 7, clips);
        definition.Boss.SecondSpecial = Special("OutSpecial", clips);
        definition.Boss.SecondMoveSpeed = 4.2f;
        definition.Boss.SpecialCooldownFrames = 250;
        definition.Boss.Profile = GetOrCreate<AIProfile>(Data + "/BossAI.asset");
        var ai = definition.Boss.Profile;
        ai.ReactionFrames = 3; ai.AttackIntervalFrames = 12; ai.Aggression = .95f;
        ai.BlockChance = .75f; ai.MistakeChance = .08f; ai.ItemUseChance = .9f;
        ai.HazardAvoidance = .92f; ai.PickupRange = 3.8f;
        EditorUtility.SetDirty(ai);
        EditorUtility.SetDirty(definition.Boss);
        if (create)
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        BuildFighter(definition);
        definition.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        definition.Boss.InstalledRevision = Revision;
        EditorUtility.SetDirty(definition.Boss);
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip CopyClip(AnimationClip imported)
    {
        string name = imported.name.Substring(imported.name.LastIndexOf("MrJack_", StringComparison.Ordinal));
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
        string path = Data + "/MrJack.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null)
            return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var graph = controller.layers[0].stateMachine;
        var idle = graph.AddState("Idle", new Vector3(320f, 0f));
        idle.motion = clips.Single(clip => clip.name == "MrJack_Idle");
        graph.defaultState = idle;
        int index = 0;
        foreach (var clip in clips.Where(clip => clip.name != "MrJack_Idle"))
        {
            string name = clip.name.Substring("MrJack_".Length);
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
            visual.name = "MrJackVisual";
            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterials = new[] { body, outline };
                renderer.updateWhenOffscreen = true;
            }
            visual.transform.localScale *= 2.3f / bounds.size.y;
            var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            HandGripContent.BindVisual(visual);
            ConfigureBoss(visual);
            PrefabUtility.SaveAsPrefabAsset(visual, VisualPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(visual); }
    }

    private static void ConfigureBoss(GameObject visual)
    {
        var feedback = visual.AddComponent<BossPresentation>();
        var feedbackData = new SerializedObject(feedback);
        var springs = feedbackData.FindProperty("springJoints");
        springs.arraySize = 4;
        for (int index = 0; index < 4; index++)
            springs.GetArrayElementAtIndex(index).objectReferenceValue = visual.GetComponentsInChildren<Transform>()
                .Single(bone => bone.name == "Spring" + (index + 1));
        feedbackData.ApplyModifiedPropertiesWithoutUndo();
        var ringObject = new GameObject("Surprise warning ring");
        ringObject.transform.SetParent(visual.transform, false);
        var ring = ringObject.AddComponent<LineRenderer>();
        ring.positionCount = 32;
        ring.loop = ring.useWorldSpace = true;
        ring.startWidth = ring.endWidth = .055f;
        var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/Warning.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(1f, .45f, .06f));
            AssetDatabase.CreateAsset(material, Art + "/Materials/Warning.mat");
        }
        ring.sharedMaterial = material;
        ring.enabled = false;
        Set(feedback, "warningRing", ring);
        var waveObject = new GameObject("Lid slam shockwave");
        waveObject.transform.SetParent(visual.transform, false);
        var wave = waveObject.AddComponent<LineRenderer>();
        wave.positionCount = 32;
        wave.loop = wave.useWorldSpace = true;
        wave.startWidth = wave.endWidth = .07f;
        wave.sharedMaterial = material;
        wave.enabled = false;
        Set(feedback, "shockwaveRing", wave);
        Set(feedback, "warningAudio", MakeAudio(visual, "MrJackMusicBox"));
        Set(feedback, "voice", MakeAudio(visual, "MrJackLaugh"));
    }

    private static AudioSource MakeAudio(GameObject root, string clip)
    {
        var node = new GameObject(clip);
        node.transform.SetParent(root.transform, false);
        var audio = node.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = Resources.Load<AudioClip>("Audio/" + clip);
        if (audio.clip == null) throw new InvalidOperationException("Import " + clip + " before building Mr. Jack.");
        node.AddComponent<WobbleBrawl.Core.SettingsAudioSource>();
        return audio;
    }

    private static MoveDef Special(string name, AnimationClip[] clips)
    {
        var move = GetOrCreate<MoveDef>(Data + "/" + name + ".asset");
        move.AnimationTrigger = name;
        move.Clip = clips.Single(clip => clip.name == "MrJack_" + name);
        move.Startup = 75; move.Active = 5; move.Recovery = 20;
        move.BodyDamage = 22; move.LegDamage = 11; move.GuardCost = 25;
        move.StunFrames = 20; move.HitStopFrames = 6; move.Pushback = 6;
        move.LaunchSpeed = 5; move.KnocksDown = true;
        move.HitboxOffset = new Vector3(0f, .9f, 0f);
        move.HitboxSize = new Vector3(2f, 1.8f, 1f);
        EditorUtility.SetDirty(move);
        return move;
    }

    private static void BuildFighter(CharacterDef definition)
    {
        var root = new GameObject("MrJackFighter");
        try
        {
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2.3f;
            capsule.radius = .525f;
            capsule.center = Vector3.up * 1.15f;
            var fighter = root.AddComponent<Fighter>();
            Set(fighter, "character", definition);
            var ai = root.AddComponent<AIInput>();
            Set(ai, "profile", definition.Boss.Profile);
            Set(fighter, "inputSource", ai);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(definition.VisualPrefab, root.transform);
            Set(fighter, "animator", visual.GetComponent<Animator>());
            var props = new SerializedObject(fighter);
            props.FindProperty("team").intValue = 1;
            var clips = props.FindProperty("visualClips");
            clips.arraySize = definition.VisualClips.Length;
            for (int i = 0; i < clips.arraySize; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue = definition.VisualClips[i];
            props.ApplyModifiedPropertiesWithoutUndo();
            var hurt = new GameObject("Body Hurtbox");
            hurt.layer = 9;
            hurt.transform.SetParent(root.transform, false);
            hurt.transform.localPosition = Vector3.up * 1.15f;
            var shape = hurt.AddComponent<BoxCollider>();
            shape.size = new Vector3(1.05f, 2.1f, .7f);
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
            move.Clip = clips.Single(clip => clip.name == "MrJack_" + name);
            move.Startup = startup[i];
            move.Active = 5;
            move.Recovery = recovery[i];
            move.BodyDamage = damage;
            move.LegDamage = legs;
            move.HeavyReaction = kind.Contains("Kick") || i >= 2;
            move.HitboxOffset = new Vector3(.84f, kind.Contains("Kick") ? .65f : 1.1f, 0);
            move.HitboxSize = new Vector3(1.8f, 1f, 1f);
            move.GroundShockwave = kind.Contains("Punch") && i == 3;
            if (move.GroundShockwave)
            {
                move.HitboxOffset = new Vector3(0f, .25f, 0f);
                move.HitboxSize = new Vector3(3.2f, .5f, 1f);
                move.Pushback = 4f;
            }
            move.ForwardSpeed = kind.Contains("Kick") && i == 0 ? (kind.StartsWith("Out", StringComparison.Ordinal) ? 3.5f : 2f) : 0f;
            move.HopSpeed = kind.Contains("Kick") && i == 0 ? 2.5f : 0f;
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
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "/MrJack_Body_Albedo.png");
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

internal sealed class MrJackImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
        string[] moved, string[] movedFrom)
    {
        if (imported.Contains(MrJackContent.ModelPath))
            EditorApplication.delayCall += MrJackContent.InstallIfMissing;
    }
}
