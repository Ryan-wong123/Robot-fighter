using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;
using Object = UnityEngine.Object;

public static partial class CombatSandboxBuilder
{
    private const string BathtubPath = "Assets/_Project/Scenes/BathtubBrawl.unity";
    private const string BathtubArt = "Assets/_Project/Art/BathtubBrawl";

    [MenuItem("Wobble Brawl/Create Bathtub Brawl")]
    public static void BuildBathtub()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BathtubPath) != null)
            return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RoundController round = null;
        Transform floor = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.TryGetComponent<RoundController>(out var controller))
                round = controller;
            if (root.layer == 8 && root.GetComponent<BoxCollider>() != null)
                floor = root.transform;
            if (root.TryGetComponent<Camera>(out var camera) && !root.TryGetComponent<AudioListener>(out _))
                camera.gameObject.AddComponent<AudioListener>();
        }
        if (round == null || floor == null)
            throw new InvalidOperationException("Bathtub Brawl requires the CombatSandbox round and arena floor.");

        var roundData = new SerializedObject(round);
        var prior = (HazardController)roundData.FindProperty("hazard").objectReferenceValue;
        if (prior != null)
            Object.DestroyImmediate(prior.gameObject);

        var stage = new GameObject("Bathtub Brawl Environment").transform;
        var porcelain = StageMaterial("Porcelain", new Color(.94f, .99f, 1f));
        var trim = StageMaterial("Tub Trim", new Color(.34f, .78f, .85f));
        var grout = StageMaterial("Grout", new Color(.56f, .82f, .86f));
        var lightTile = StageMaterial("Light Tile", new Color(.82f, .96f, .98f));
        var blueTile = StageMaterial("Blue Tile", new Color(.66f, .88f, .95f));
        var chrome = StageMaterial("Faucet Chrome", new Color(.47f, .69f, .79f), .45f);
        var soapPink = StageMaterial("Rose Soap", new Color(.98f, .46f, .65f));
        var soapBlue = StageMaterial("Blue Soap", new Color(.26f, .77f, .93f));
        var duckYellow = StageMaterial("Duck Yellow", new Color(1f, .76f, .13f));
        var duckOrange = StageMaterial("Duck Orange", new Color(.98f, .39f, .08f));
        var dark = StageMaterial("Duck Eye", new Color(.14f, .12f, .21f));
        var water = StageMaterial("Water", new Color(.09f, .69f, .94f, .38f));
        var foam = StageMaterial("Sea Foam", new Color(.65f, .96f, 1f));

        floor.GetComponent<Renderer>().sharedMaterial = porcelain;
        StagePart(stage, "Tile Backing", PrimitiveType.Cube, new Vector3(0, 2.5f, 4.3f),
            new Vector3(17f, 6f, .24f), grout);
        for (int x = -7; x <= 7; x++)
            for (int y = 0; y < 4; y++)
                StagePart(stage, "Glazed Tile", PrimitiveType.Cube,
                    new Vector3(x * 1.1f, .5f + y * 1.2f, 4.15f), new Vector3(1.06f, 1.15f, .1f),
                    (x + y) % 2 == 0 ? lightTile : blueTile);
        StagePart(stage, "Back Tub Wall", PrimitiveType.Cube, new Vector3(0, .5f, 2.18f),
            new Vector3(14f, 1.1f, .42f), porcelain);
        StagePart(stage, "Back Tub Rim", PrimitiveType.Cube, new Vector3(0, 1.12f, 2.16f),
            new Vector3(14.35f, .22f, .56f), trim);
        StagePart(stage, "Front Rim", PrimitiveType.Cube, new Vector3(0, -.27f, -2.3f),
            new Vector3(14.3f, .25f, .38f), trim);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            StagePart(stage, "Tub End", PrimitiveType.Cube, new Vector3(sign * 7.12f, .38f, 0),
                new Vector3(.28f, .96f, 4.5f), porcelain, true);
            StagePart(stage, "Tub End Rim", PrimitiveType.Cube, new Vector3(sign * 7.12f, .9f, 0),
                new Vector3(.4f, .2f, 4.65f), trim);
        }
        Soap(stage, -5.2f, soapPink);
        Soap(stage, 5.4f, soapBlue);
        var spout = new Vector3(-4.95f, 3.30f, 2.1f);
        Pipe(stage, "Faucet Riser", new Vector3(-6.05f, 1.45f, 2.1f),
            new Vector3(-6.05f, 3.95f, 2.1f), .18f, chrome);
        Pipe(stage, "Faucet Arm", new Vector3(-6.05f, 3.95f, 2.1f),
            new Vector3(-5.28f, 3.95f, 2.1f), .18f, chrome);
        Pipe(stage, "Curved Spout", new Vector3(-5.28f, 3.95f, 2.1f), spout, .18f, chrome);
        StagePart(stage, "Faucet Cap", PrimitiveType.Sphere, new Vector3(-6.05f, 3.95f, 2.1f),
            Vector3.one * .36f, chrome);
        StagePart(stage, "Faucet Valve", PrimitiveType.Cylinder, new Vector3(-6.5f, 1.75f, 2.03f),
            new Vector3(.42f, .13f, .42f), chrome);

        for (int index = 0; index < 9; index++)
        {
            float x = -6.1f + index * 1.46f;
            float size = .12f + index % 3 * .085f;
            StagePart(stage, "Bubble", PrimitiveType.Sphere,
                new Vector3(x, 1.65f + index % 4 * .38f, 2.87f), Vector3.one * size, foam);
        }

        var duck = Group(stage, "Rubber Duck", new Vector3(.5f, 1.48f, 1.68f));
        StagePart(duck, "Duck Body", PrimitiveType.Sphere, new Vector3(0, .18f, 0),
            new Vector3(1.65f, .79f, .86f), duckYellow);
        StagePart(duck, "Duck Head", PrimitiveType.Sphere, new Vector3(-.48f, .79f, 0),
            Vector3.one * .72f, duckYellow);
        StagePart(duck, "Duck Tail", PrimitiveType.Sphere, new Vector3(.62f, .42f, 0),
            new Vector3(.48f, .5f, .53f), duckYellow).transform.Rotate(0, 0, -30f);
        StagePart(duck, "Duck Beak", PrimitiveType.Sphere, new Vector3(-.87f, .67f, -.03f),
            new Vector3(.48f, .23f, .43f), duckOrange);
        StagePart(duck, "Duck Wing", PrimitiveType.Sphere, new Vector3(.17f, .21f, -.35f),
            new Vector3(.68f, .37f, .24f), duckOrange);
        StagePart(duck, "Duck Eye", PrimitiveType.Sphere, new Vector3(-.56f, .86f, -.328f),
            new Vector3(.091f, .12f, .04f), dark);
        StagePart(duck, "Eye Shine", PrimitiveType.Sphere, new Vector3(-.578f, .885f, -.351f),
            Vector3.one * .026f, porcelain);

        var hazard = new GameObject("Bathtub Hazard Cycle").AddComponent<HazardController>();
        var bath = hazard.gameObject.AddComponent<BathtubHazards>();
        var data = GetData<HazardDef>("BathtubHazardCycle");
        data.WarningSeconds = 2f;
        data.ActiveSeconds = 4f;
        Set(hazard, "definition", data);
        EditorUtility.SetDirty(data);
        var hit = GetData<MoveDef>("BathtubDuckWaveHit");
        hit.LegDamage = hit.BodyDamage = hit.GuardCost = hit.GaugeGain = 0;
        hit.StunFrames = 16;
        hit.HitStopFrames = 0;
        hit.Pushback = 5;
        EditorUtility.SetDirty(hit);

        var level = Group(hazard.transform, "Rising Water", Vector3.zero);
        StagePart(level, "Water Surface", PrimitiveType.Cube, new Vector3(0, -.03f, 0),
            new Vector3(13.95f, .05f, 4.2f), water);
        StagePart(level, "Waterline", PrimitiveType.Cube, new Vector3(0, -.02f, -1.56f),
            new Vector3(13.95f, .035f, .08f), foam);
        var waterWarning = Group(hazard.transform, "Drips and Water Warning", Vector3.zero);
        StagePart(waterWarning, "Projected Waterline", PrimitiveType.Cube, new Vector3(0, 1.2f, 1.93f),
            new Vector3(13.85f, .045f, .06f), foam);
        for (int index = 0; index < 4; index++)
            StagePart(waterWarning, "Faucet Drip", PrimitiveType.Sphere,
                spout + Vector3.down * (.30f + index * .34f), new Vector3(.06f, .13f, .06f), foam);
        var duckWarning = Group(hazard.transform, "Incoming Duck Wave Warning", new Vector3(5.7f, .03f, -.25f));
        var warningInk = StageMaterial("Incoming Wave", new Color(.10f, .33f, .63f));
        StagePart(duckWarning, "Duck Shadow", PrimitiveType.Sphere, new Vector3(.48f, 0, .25f),
            new Vector3(1.5f, .045f, 1f), warningInk);
        for (int sign = -1; sign <= 1; sign += 2)
            StagePart(duckWarning, "Left Chevron", PrimitiveType.Cube, new Vector3(-.38f, .04f, sign * .22f),
                new Vector3(.7f, .055f, .13f), foam).transform.Rotate(0, sign * 35f, 0);
        var wave = Group(hazard.transform, "Duck Wave", Vector3.zero);
        StagePart(wave, "Wave Crest", PrimitiveType.Sphere, new Vector3(0, .14f, 0),
            new Vector3(1.44f, 1.02f, 3.72f), water);
        for (int index = 0; index < 7; index++)
            StagePart(wave, "Foam Bump", PrimitiveType.Sphere, new Vector3(-.18f, .48f, -1.56f + index * .52f),
                new Vector3(.38f, .24f, .62f), foam);
        foreach (var visual in new[] { level, waterWarning, duckWarning, wave })
            visual.gameObject.SetActive(false);

        var serialized = new SerializedObject(bath);
        var fighters = serialized.FindProperty("fighters");
        fighters.arraySize = 2;
        fighters.GetArrayElementAtIndex(0).objectReferenceValue = roundData.FindProperty("player").objectReferenceValue;
        fighters.GetArrayElementAtIndex(1).objectReferenceValue = roundData.FindProperty("opponent").objectReferenceValue;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Set(bath, "waterSurface", level);
        Set(bath, "waterWarning", waterWarning.gameObject);
        Set(bath, "duckWarning", duckWarning.gameObject);
        Set(bath, "duck", duck);
        Set(bath, "wave", wave);
        Set(bath, "waveHit", hit);
        var sound = hazard.gameObject.AddComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.volume = .65f;
        Set(bath, "audioSource", sound);
        Set(bath, "drip", AssetDatabase.LoadAssetAtPath<AudioClip>(BathtubArt + "/FaucetDrip.wav"));
        Set(bath, "quack", AssetDatabase.LoadAssetAtPath<AudioClip>(BathtubArt + "/DuckQuack.wav"));
        Set(round, "hazard", hazard);
        EditorSceneManager.SaveScene(scene, BathtubPath);
        AssetDatabase.SaveAssets();
    }

    private static Transform Group(Transform parent, string label, Vector3 position)
    {
        var group = new GameObject(label).transform;
        group.SetParent(parent, false);
        group.localPosition = position;
        return group;
    }

    private static void Soap(Transform stage, float x, Material material)
    {
        var soap = StagePart(stage, "Soap Platform", PrimitiveType.Cube, new Vector3(x, .225f, 0),
            new Vector3(1.4f, .45f, 1.28f), material, true);
        soap.layer = 8;
        StagePart(stage, "Soap Inset", PrimitiveType.Cube, new Vector3(x, .451f, 0),
            new Vector3(.93f, .025f, .76f), material);
    }

    private static void Pipe(Transform stage, string label, Vector3 start, Vector3 end, float radius, Material material)
    {
        var pipe = StagePart(stage, label, PrimitiveType.Cylinder, (start + end) * .5f,
            new Vector3(radius * 2f, Vector3.Distance(start, end) * .5f, radius * 2f), material);
        pipe.transform.rotation = Quaternion.FromToRotation(Vector3.up, end - start);
    }

    private static GameObject StagePart(Transform parent, string label, PrimitiveType primitive,
        Vector3 position, Vector3 scale, Material material, bool keepCollider = false)
    {
        var part = GameObject.CreatePrimitive(primitive);
        part.name = label;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        var renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        if (!keepCollider)
            Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    private static Material StageMaterial(string label, Color color, float metallic = 0f)
    {
        string path = BathtubArt + "/" + label.Replace(" ", "") + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;
        bool useToon = color.a >= 1f && metallic == 0f;
        material = new Material(useToon
            ? AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Art/Items/ToonPickupSurface.shadergraph")
            : Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        if (useToon)
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        else
        {
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", .35f);
        }
        if (color.a < 1f)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
