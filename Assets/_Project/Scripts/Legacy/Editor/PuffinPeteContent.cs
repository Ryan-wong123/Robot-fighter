using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;

/// <summary>Imports the Blender export and authors Unity assets without scene rewiring.</summary>
[InitializeOnLoad]
public static class PuffinPeteContent
{
    public const string ModelPath = "Assets/_Project/Art/Characters/PuffinPete/PuffinPete.fbx";
    public const string DefinitionPath = "Assets/_Project/Resources/Characters/PuffinPete.asset";
    private const string Art = "Assets/_Project/Art/Characters/PuffinPete";
    private const string Data = "Assets/_Project/Data/Combat/PuffinPete";
    private const string VisualPath = "Assets/_Project/Prefabs/PuffinPeteVisual.prefab";
    private const string PrefabPath = "Assets/_Project/Prefabs/PuffinPeteFighter.prefab";

    static PuffinPeteContent()
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
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/PeteToon.mat");
            if (material != null && material.GetTexture("_BaseMap") == null)
                RepairColors();
            return;
        }
        Build();
    }

    [MenuItem("Wobble Brawl/Characters/Repair Puffin Pete Colors")]
    public static void RepairColors()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        ConfigureTexture();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/PeteToon.mat");
        BindAtlas(material);
        AssetDatabase.SaveAssets();
    }

    private static void BindAtlas(Material material)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/PuffinPete_Body_Albedo.png");
        if (texture == null || material == null)
            throw new InvalidOperationException("Puffin Pete requires its imported 2D atlas and toon material.");
        material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        EditorUtility.SetDirty(material);
    }

    [MenuItem("Wobble Brawl/Characters/Build Puffin Pete")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build Puffin Pete outside Play Mode.");
        EnsureFolder(Data);
        EnsureFolder("Assets/_Project/Resources/Characters");
        EnsureFolder(Art + "/Materials");
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("Puffin Pete Blender FBX is missing.");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importBlendShapes = true;
        importer.importCameras = importer.importLights = false;
        importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        var settings = importer.defaultClipAnimations;
        foreach (var clip in settings)
            clip.loopTime = new[] { "Idle", "Walk", "Block", "JumpFall", "Deflate" }.Any(name => clip.name.EndsWith("_" + name));
        importer.clipAnimations = settings;
        importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__") && clip.name.Contains("PuffinPete_"))
            .Select(CopyClip).OrderBy(clip => clip.name).ToArray();
        if (clips.Length != 24)
            throw new InvalidOperationException("Expected 24 Puffin Pete clips, got " + clips.Length);
        var controller = CreateController(clips);
        ConfigureTexture();
        var body = MakeMaterial("PeteToon", "BlobbyJelly.mat");
        BindAtlas(body);
        var outline = MakeMaterial("PeteOutline", "JellyOutline.mat");
        outline.SetColor("_Outline", new Color(.015f, .05f, .18f));
        outline.SetFloat("_OutlineWidth", .012f);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(outline);
        BuildVisual(controller, body, outline);

        // Create the resource definition last, so a failed import retries next domain reload.
        var definition = AssetDatabase.LoadAssetAtPath<CharacterDef>(DefinitionPath);
        bool create = definition == null;
        if (create)
            definition = ScriptableObject.CreateInstance<CharacterDef>();
        definition.DisplayName = "Puffin Pete";
        definition.AnimationPrefix = "PuffinPete";
        definition.VisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
        definition.VisualClips = clips;
        definition.Height = 1.55f;
        definition.BodyWidth = 1.1f;
        definition.MaxHealth = 220;
        definition.MaxGuard = 100;
        definition.MoveSpeed = 2.6f;
        definition.JumpSpeed = 8f;
        definition.RagdollReactionScale = .65f;
        definition.PartialRagdollSeconds = .28f;
        definition.HasInflation = true;
        definition.PopCapacity = 100;
        definition.PopGainPerHit = 25;
        definition.DeflateFrames = 75;
        definition.InflationDamageReduction = 30;
        definition.DeflateSpeed = 5f;
        definition.DeflateTurnFrames = 12;
        definition.DeflateBounds = new Vector2(-6.2f, 6.2f);
        definition.Punch = Chain("Punch", new[] { 7, 9, 10, 13 }, new[] { 15, 15, 17, 21 }, 10, 5, clips);
        definition.Kick = Chain("Kick", new[] { 10, 13 }, new[] { 18, 21 }, 12, 6, clips);
        definition.Special = GetOrCreate<MoveDef>(Data + "/Special.asset");
        var special = definition.Special;
        special.AnimationTrigger = "Special";
        special.Clip = clips.Single(clip => clip.name == "PuffinPete_Special");
        special.Startup = 18;
        special.Active = 24;
        special.Recovery = 30;
        special.BodyDamage = 18;
        special.LegDamage = 10;
        special.HitIntervalFrames = 0;
        special.AlternateSides = false;
        special.ForwardSpeed = 2.2f;
        special.HopSpeed = 2.5f;
        special.HeavyReaction = true;
        special.StunFrames = 14;
        special.HitStopFrames = 6;
        special.Pushback = 4f;
        special.HitboxOffset = new Vector3(.6f, .75f, 0f);
        special.HitboxSize = new Vector3(2.1f, 1.4f, 1f);
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
        string name = imported.name.Substring(imported.name.LastIndexOf("PuffinPete_", StringComparison.Ordinal));
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
        string path = Data + "/PuffinPete.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null)
            return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var graph = controller.layers[0].stateMachine;
        var idle = graph.AddState("Idle", new Vector3(320f, 0f));
        idle.motion = clips.Single(clip => clip.name == "PuffinPete_Idle");
        graph.defaultState = idle;
        int index = 0;
        foreach (var clip in clips.Where(clip => clip.name != "PuffinPete_Idle"))
        {
            string name = clip.name.Substring("PuffinPete_".Length);
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
            visual.name = "PuffinPeteVisual";
            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterials = new[] { body, outline };
                renderer.updateWhenOffscreen = true;
            }
            visual.transform.localScale *= 1.55f / bounds.size.y;
            var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var hand = visual.GetComponentsInChildren<Transform>().Single(bone => bone.name == "Hand.L");
            var socket = new GameObject("Item Socket").transform;
            socket.SetParent(hand, false);
            visual.AddComponent<FighterVisual>().ItemSocket = socket;
            ConfigureInflationVisual(visual, renderers[0]);
            PrefabUtility.SaveAsPrefabAsset(visual, VisualPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(visual); }
    }

    private static void ConfigureInflationVisual(GameObject visual, SkinnedMeshRenderer skin)
    {
        if (skin.sharedMesh.blendShapeCount < 2)
            throw new InvalidOperationException("Puffin Pete requires Inflate and Deflate morphs.");
        var feedback = visual.AddComponent<InflationVisual>();
        Set(feedback, "skin", skin);
        ConfigureMorphSocket(feedback, skin, visual.GetComponent<FighterVisual>().ItemSocket);
        var burstObject = new GameObject("Air Burst");
        burstObject.transform.SetParent(visual.transform, false);
        burstObject.transform.position = visual.transform.position + Vector3.up * .8f;
        var particles = burstObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = .8f;
        main.startLifetime = .8f;
        main.startSpeed = 2.5f;
        main.startSize = .05f;
        main.startColor = new Color(.75f, .94f, 1f);
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .12f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2f;
        renderer.velocityScale = .1f;
        string path = Art + "/Materials/AirBurst.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        renderer.sharedMaterial = material;
        Set(feedback, "airBurst", particles);
        var audio = burstObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = Resources.Load<AudioClip>("Audio/PetePop");
        burstObject.AddComponent<WobbleBrawl.Core.SettingsAudioSource>();
        Set(feedback, "popAudio", audio);
    }

    private static void ConfigureMorphSocket(InflationVisual feedback, SkinnedMeshRenderer skin, Transform socket)
    {
        var mesh = skin.sharedMesh;
        var vertices = mesh.vertices;
        var point = skin.transform.InverseTransformPoint(socket.parent.position);
        int closest = 0;
        for (int index = 1; index < vertices.Length; index++)
            if ((vertices[index] - point).sqrMagnitude < (vertices[closest] - point).sqrMagnitude)
                closest = index;
        var delta = new Vector3[vertices.Length];
        var properties = new SerializedObject(feedback);
        for (int index = 0; index < mesh.blendShapeCount; index++)
        {
            string name = mesh.GetBlendShapeName(index);
            string field = name.EndsWith("Inflate", StringComparison.Ordinal) ? "inflateSocketOffset" :
                name.EndsWith("Deflate", StringComparison.Ordinal) ? "deflateSocketOffset" : null;
            if (field == null) continue;
            mesh.GetBlendShapeFrameVertices(index, 0, delta, null, null);
            properties.FindProperty(field).vector3Value = socket.parent.InverseTransformVector(
                skin.transform.TransformVector(delta[closest]));
        }
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildFighter(CharacterDef definition)
    {
        var root = new GameObject("PuffinPeteFighter");
        try
        {
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.55f;
            capsule.radius = .55f;
            capsule.center = Vector3.up * .775f;
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
            hurt.transform.localPosition = Vector3.up * .775f;
            var shape = hurt.AddComponent<BoxCollider>();
            shape.size = new Vector3(1.1f, 1.35f, .8f);
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
            move.Clip = clips.Single(clip => clip.name == "PuffinPete_" + name);
            move.Startup = startup[i];
            move.Active = 4;
            move.Recovery = recovery[i];
            move.BodyDamage = damage;
            move.LegDamage = legs;
            move.HeavyReaction = kind == "Kick" || i >= 2;
            move.HitboxOffset = new Vector3(.63f, kind == "Kick" ? .35f : .9f, 0);
            move.HitboxSize = new Vector3(1.35f, .9f, 1f);
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
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "/PuffinPete_Body_Albedo.png");
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

internal sealed class PuffinPeteImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
        string[] moved, string[] movedFrom)
    {
        if (imported.Contains(PuffinPeteContent.ModelPath))
            EditorApplication.delayCall += PuffinPeteContent.InstallIfMissing;
    }
}
