using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;
using WobbleBrawl.UI;

public static class CombatSandboxBuilder
{
    // This FBX needs a centimetre-to-metre root scale for its baked skin.
    private const float BlobbyImportScale = 0.01f;
    private const string DataFolder = "Assets/_Project/Data/Combat";
    private const string ScenePath = "Assets/_Project/Scenes/CombatSandbox.unity";

    [MenuItem("Wobble Brawl/Create Combat Sandbox")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            return;
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
            AssetDatabase.CreateFolder("Assets/_Project", "Data");
        if (!AssetDatabase.IsValidFolder(DataFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Combat");

        var character = GetData<CharacterDef>("SandboxFighter");
        character.Punch = Chain("Punch", 4, 10, 5);
        character.Kick = Chain("Kick", 2, 12, 6);
        character.Special = Chain("Special", 1, 15, 10);
        EditorUtility.SetDirty(character);

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Arena Floor";
        floor.transform.position = Vector3.down * 0.25f;
        floor.transform.localScale = new Vector3(14f, 0.5f, 5f);
        floor.layer = 8;

        var camera = new GameObject("Fight Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 3f, -10f);
        camera.transform.rotation = Quaternion.Euler(7f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = 4.4f;
        var light = new GameObject("Arena Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        var canvas = new GameObject("HUD", typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var player = CreateFighter("Player", -2, 0, character, camera, canvas);
        var opponent = CreateFighter("Opponent", 2, 1, character, camera, canvas);
        Set(player, "inputSource", player.gameObject.AddComponent<SandboxInput>());
        var ai = opponent.gameObject.AddComponent<AIInput>();
        Set(ai, "opponent", player);
        Set(opponent, "inputSource", ai);

        var flow = new GameObject("Match");
        var round = flow.AddComponent<RoundController>();
        Set(round, "player", player);
        Set(round, "opponent", opponent);
        Set(round, "playerSpawn", Spawn("Player Spawn", -2));
        Set(round, "opponentSpawn", Spawn("Opponent Spawn", 2));
        Set(flow.AddComponent<MatchController>(), "round", round);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    private static Transform Spawn(string name, float x)
    {
        var point = new GameObject(name).transform;
        point.position = new Vector3(x, 0.02f, 0f);
        return point;
    }

    private static Fighter CreateFighter(string name, float x, int team, CharacterDef data, Camera camera, Canvas canvas)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(GetFighterPrefab(data));
        root.name = name;
        root.transform.position = new Vector3(x, 0.02f, 0f);
        var fighter = root.GetComponent<Fighter>();
        Number(fighter, "team", team);
        var knockout = root.AddComponent<KnockoutPresentation>();
        Set(knockout, "fighter", fighter);
        Set(knockout, "fightCamera", camera);
        var label = new GameObject(name + " Impact", typeof(RectTransform)).AddComponent<Text>();
        label.transform.SetParent(canvas.transform, false);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        var popup = label.gameObject.AddComponent<ComicImpactPopup>();
        Set(popup, "fighter", fighter);
        Set(popup, "fightCamera", camera);
        Set(popup, "label", label);
        return fighter;
    }

    private static GameObject GetFighterPrefab(CharacterDef data)
    {
        const string prefabPath = "Assets/_Project/Prefabs/BlobbyFighter.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
            return prefab;

        var root = new GameObject("BlobbyFighter");
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = Vector3.up * 0.9f;
        collider.height = 1.8f;
        collider.radius = 0.35f;
        var fighter = root.AddComponent<Fighter>();
        Set(fighter, "character", data);
        Mask(fighter, "groundLayers", 1 << 8);
        Mask(root.GetComponent<Hitbox>(), "hurtLayers", 1 << 9);

        var hurt = new GameObject("Body Hurtbox");
        hurt.layer = 9;
        hurt.transform.SetParent(root.transform, false);
        hurt.transform.localPosition = Vector3.up * 0.9f;
        var shape = hurt.AddComponent<BoxCollider>();
        shape.size = new Vector3(0.7f, 1.6f, 0.6f);
        shape.isTrigger = true;
        Set(hurt.AddComponent<Hurtbox>(), "owner", fighter);
        Number(hurt.GetComponent<Hurtbox>(), "zone", (int)HurtZone.Body);

        var visual = CreateBlobbyVisual(root.transform);
        var squash = root.AddComponent<SquashStretch>();
        Set(squash, "fighter", fighter);
        Set(squash, "visual", visual.transform);
        try
        {
            return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateBlobbyVisual(Transform parent)
    {
        const string path = "Assets/_Project/Art/Characters/Blobby/BlobbyCharacter.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        if (importer != null && (importer.animationType != ModelImporterAnimationType.Generic || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel))
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
                clip.loopTime = clip.name.EndsWith("Walk") || clip.name.EndsWith("Idle") || clip.name.EndsWith("Block");
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null)
            throw new System.InvalidOperationException("Import BlobbyCharacter.fbx before building the sandbox.");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        visual.transform.localScale = Vector3.one * BlobbyImportScale;
        const string atlasPath = "Assets/_Project/Art/Characters/Blobby/Blobby_Body_Albedo.png";
        const string materialPath = "Assets/_Project/Art/Characters/Blobby/Materials/BlobbyJelly.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(materialPath.Replace("BlobbyJelly.mat", "ToonJellySurface.shadergraph")));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        if (AssetImporter.GetAtPath(atlasPath) is TextureImporter textureImporter &&
            textureImporter.textureShape != TextureImporterShape.Texture2D)
        {
            textureImporter.textureShape = TextureImporterShape.Texture2D;
            textureImporter.SaveAndReimport();
        }
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        if (atlas == null)
            throw new System.InvalidOperationException("Blobby requires the 2D face atlas.");
        material.SetTexture("_BaseMap", atlas);
        EditorUtility.SetDirty(material);
        var outline = AssetDatabase.LoadAssetAtPath<Material>(materialPath.Replace("BlobbyJelly.mat", "JellyOutline.mat"));
        foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            skin.sharedMaterials = new[] { material, outline };
        var animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Avatar avatar && avatar.isValid)
                animator.avatar = avatar;
        if (animator.avatar == null)
            throw new System.InvalidOperationException("Blobby requires a valid Generic avatar.");
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = CreateAnimationController(path);
        var fighter = parent.GetComponent<Fighter>();
        Set(fighter, "animator", animator);
        var serialized = new SerializedObject(fighter);
        var clipsProperty = serialized.FindProperty("visualClips");
        clipsProperty.arraySize = 0;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (!(asset is AnimationClip clip) || clip.legacy || clip.name.StartsWith("__preview__"))
                continue;
            string clipPath = DataFolder + "/" + clip.name.Substring(clip.name.LastIndexOf('|') + 1) + ".anim";
            var standalone = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (standalone == null)
            {
                standalone = Object.Instantiate(clip);
                AssetDatabase.CreateAsset(standalone, clipPath);
            }
            clipsProperty.GetArrayElementAtIndex(clipsProperty.arraySize++).objectReferenceValue = standalone;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return visual;
    }

    private static AnimatorController CreateAnimationController(string modelPath)
    {
        const string path = DataFolder + "/Blobby.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            if (!(asset is AnimationClip clip) || clip.legacy || clip.name.StartsWith("__preview__"))
                continue;
            int prefix = clip.name.LastIndexOf("Blobby_", System.StringComparison.Ordinal);
            if (prefix < 0)
                continue;
            string name = clip.name.Substring(prefix + "Blobby_".Length);
            AnimatorState state = null;
            foreach (var child in machine.states)
                if (child.state.name == name)
                    state = child.state;
            if (state == null)
                state = machine.AddState(name);
            state.motion = clip;
            EditorUtility.SetDirty(state);
            if (name == "Idle")
                machine.defaultState = state;
        }

        if (machine.defaultState == null || machine.defaultState.motion == null)
            throw new System.InvalidOperationException("Blobby idle clip is missing.");
        EditorUtility.SetDirty(machine);
        return controller;
    }

    public static void UpdateSharedBlobbyVisual()
    {
        const string path = "Assets/_Project/Prefabs/BlobbyFighter.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var squash = root.GetComponent<SquashStretch>();
            var previous = (Transform)new SerializedObject(squash).FindProperty("visual").objectReferenceValue;
            var visual = CreateBlobbyVisual(root.transform);
            Set(squash, "visual", visual.transform);
            Object.DestroyImmediate(previous.gameObject);
            Mask(root.GetComponent<Fighter>(), "groundLayers", 1 << 8);
            Mask(root.GetComponent<Hitbox>(), "hurtLayers", 1 << 9);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static void InstallBlobbyVisuals()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var camera = System.Array.Find(scene.GetRootGameObjects(), root => root.GetComponent<Camera>() != null).GetComponent<Camera>();
        foreach (var root in scene.GetRootGameObjects())
        {
            var squash = root.GetComponent<SquashStretch>();
            if (squash == null)
                continue;
            var ko = root.GetComponent<KnockoutPresentation>();
            if (ko == null)
                ko = root.AddComponent<KnockoutPresentation>();
            Set(ko, "fighter", root.GetComponent<Fighter>());
            Set(ko, "fightCamera", camera);
            var visualProperty = new SerializedObject(squash).FindProperty("visual");
            var previous = (Transform)visualProperty.objectReferenceValue;

            var visual = CreateBlobbyVisual(root.transform);
            Set(squash, "visual", visual.transform);
            if (previous != null)
                Object.DestroyImmediate(previous.gameObject);
        }
        Chain("Punch", 4, 10, 5);
        Chain("Kick", 2, 12, 6);
        Chain("Special", 1, 15, 10);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
    }

    private static MoveDef Chain(string name, int length, int body, int legs)
    {
        MoveDef next = null;
        for (int index = length; index >= 1; index--)
        {
            var move = GetData<MoveDef>(name + index);
            move.AnimationTrigger = name == "Special" ? name : name + index;
            move.BodyDamage = body;
            move.LegDamage = legs;
            move.NextInCombo = next;
            move.HitboxSize = new Vector3(1.5f, 1f, 1f);
            EditorUtility.SetDirty(move);
            next = move;
        }
        return next;
    }

    private static T GetData<T>(string name) where T : ScriptableObject
    {
        string path = DataFolder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
            return existing;
        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Mask(Object target, string field, int value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        var bits = property.FindPropertyRelative("m_Bits") ?? property;
        bits.intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Number(Object target, string field, int value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
