using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;

public static partial class CombatSandboxBuilder
{
    private const string KitchenPath = "Assets/_Project/Scenes/KitchenCounter.unity";
    private const string KitchenArt = "Assets/_Project/Art/KitchenCounter";

    [MenuItem("Wobble Brawl/Create Kitchen Counter")]
    public static void BuildKitchen()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(KitchenPath) != null)
            return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var (scene, round, floor) = OpenStageTemplate();
        var stage = new GameObject("Kitchen Counter Environment").transform;
        var maple = KitchenMaterial("Maple Counter", "E9AE62");
        var grain = KitchenMaterial("Counter Grain", "BE783F");
        var board = KitchenMaterial("Cutting Board", "BA743C");
        var boardEdge = KitchenMaterial("Board Groove", "9C5230");
        var tile = KitchenMaterial("Cream Tile", "F6DDA0");
        var tileAlt = KitchenMaterial("Butter Tile", "F2C674");
        var cupboard = KitchenMaterial("Cupboard", "97532F");
        var cupboardInset = KitchenMaterial("Cupboard Inset", "C98D54");
        var silver = KitchenMaterial("Brushed Silver", "B5C4D4", .55f);
        var ink = KitchenMaterial("Appliance Dark", "2F293B");
        var red = KitchenMaterial("Mixer Red", "D9454B");
        var ember = KitchenMaterial("Hot Coil", "FF5233");
        var ivory = KitchenMaterial("Arrow Cream", "FFF3D2");
        var crust = KitchenMaterial("Toast Crust", "AF5F29");
        var bread = KitchenMaterial("Toast Center", "F6BF6A");
        var mint = KitchenMaterial("Bowl Mint", "68B5A4");
        var leaf = KitchenMaterial("Fruit Leaf", "7CAF56");

        floor.GetComponent<Renderer>().sharedMaterial = maple;
        Part(stage, "Counter Fascia", new Vector3(0, -.43f, -2.34f), new Vector3(14.3f, .35f, .4f), grain);
        for (int index = 0; index < 5; index++)
            Part(stage, "Counter Seam", new Vector3(0, .002f, -1.8f + index * .91f),
                new Vector3(14f, .008f, .032f), grain);
        for (int column = -7; column <= 7; column++)
            for (int row = 0; row < 3; row++)
                Part(stage, "Backsplash Tile", new Vector3(column * 1.04f, .45f + row * .82f, 3.86f),
                    new Vector3(1.01f, .79f, .16f), (column + row) % 2 == 0 ? tile : tileAlt);
        Part(stage, "Backsplash Grout", new Vector3(0, 1.26f, 4f), new Vector3(17f, 2.55f, .17f), grain);
        for (int column = -3; column <= 3; column++)
        {
            float x = column * 2.4f;
            Part(stage, "Cupboard Door", new Vector3(x, 3.93f, 3.73f), new Vector3(2.32f, 2.75f, .28f), cupboard);
            Part(stage, "Door Inset", new Vector3(x, 3.96f, 3.56f), new Vector3(1.95f, 2.31f, .12f), cupboardInset);
            StagePart(stage, "Door Pull", PrimitiveType.Sphere, new Vector3(x + .66f, 3.18f, 3.44f),
                new Vector3(.14f, .25f, .14f), silver);
        }
        Part(stage, "Cutting Board", new Vector3(0, .12f, 1.7f), new Vector3(2.25f, .24f, 1.22f), board);
        Part(stage, "Board Grip", new Vector3(.93f, .247f, 1.7f), new Vector3(.17f, .02f, .35f), boardEdge);
        for (int side = -1; side <= 1; side += 2)
            Part(stage, "Counter End Stop", new Vector3(side * 7.12f, .36f, 0),
                new Vector3(.28f, .9f, 4.5f), maple, true);

        var toaster = Group(stage, "Two Slot Toaster", new Vector3(-5.7f, 0, 1.52f));
        Part(toaster, "Toaster Shell", new Vector3(0, .57f, 0), new Vector3(1.45f, 1.03f, .94f), silver);
        Part(toaster, "Toaster Base", new Vector3(0, .13f, 0), new Vector3(1.51f, .21f, .99f), ink);
        Part(toaster, "Lever Track", new Vector3(.49f, .62f, -.488f), new Vector3(.09f, .45f, .04f), ink);
        Part(toaster, "Lever Grip", new Vector3(.49f, .69f, -.55f), new Vector3(.23f, .10f, .12f), silver);
        var coils = Group(toaster, "Glowing Toaster Coils", Vector3.zero);
        var launchPoints = new Transform[2];
        for (int index = 0; index < launchPoints.Length; index++)
        {
            float x = -.33f + index * .53f;
            Part(toaster, "Toast Slot", new Vector3(x, 1.094f, 0), new Vector3(.26f, .028f, .67f), ink);
            Part(coils, "Glowing Element", new Vector3(x, 1.12f, 0), new Vector3(.15f, .027f, .58f), ember);
            launchPoints[index] = Group(toaster, "Toast Launch", new Vector3(x, 1.32f, 0));
        }

        var mixer = Group(stage, "Stand Mixer", new Vector3(5.58f, 0, 1.51f));
        Part(mixer, "Mixer Base", new Vector3(0, .12f, 0), new Vector3(1.49f, .21f, .98f), red);
        Part(mixer, "Mixer Stand", new Vector3(.47f, .89f, 0), new Vector3(.48f, 1.43f, .63f), red);
        StagePart(mixer, "Mixer Head", PrimitiveType.Sphere, new Vector3(-.02f, 1.64f, 0),
            new Vector3(1.59f, .57f, .76f), red);
        Part(mixer, "Mixer Chrome Band", new Vector3(-.03f, 1.57f, -.353f),
            new Vector3(1.12f, .09f, .065f), silver);
        Pipe(mixer, "Mixing Shaft", new Vector3(-.43f, 1.52f, 0), new Vector3(-.43f, .72f, 0), .055f, silver);
        StagePart(mixer, "Mixing Bowl", PrimitiveType.Sphere, new Vector3(-.37f, .46f, 0),
            new Vector3(.88f, .52f, .77f), silver);
        StagePart(stage, "Fruit Bowl", PrimitiveType.Sphere, new Vector3(2.12f, .36f, 2.55f),
            new Vector3(1.49f, .65f, 1.01f), mint);
        for (int index = 0; index < 4; index++)
        {
            Vector3 fruit = new Vector3(1.61f + index * .34f, .68f + index % 2 * .12f, 2.52f);
            StagePart(stage, "Fruit", PrimitiveType.Sphere, fruit, Vector3.one * .47f, index % 2 == 0 ? red : bread);
            StagePart(stage, "Fruit Leaf", PrimitiveType.Sphere, fruit + new Vector3(.085f, .25f, 0),
                new Vector3(.20f, .09f, .11f), leaf).transform.Rotate(0, 0, 25f);
        }
        for (int index = 0; index < 8; index++)
        {
            float x = -2.7f + index * .34f;
            float z = 1.7f + index % 3 * .29f;
            StagePart(stage, "Spilled Cereal", PrimitiveType.Cylinder, new Vector3(x, .055f, z),
                new Vector3(.19f, .04f, .19f), index % 2 == 0 ? bread : mint);
        }

        var hazard = new GameObject("Kitchen Hazard Cycle").AddComponent<HazardController>();
        var kitchen = hazard.gameObject.AddComponent<KitchenHazards>();
        var data = GetData<HazardDef>("KitchenHazardCycle");
        data.WarningSeconds = 2f;
        data.ActiveSeconds = 4f;
        Set(hazard, "definition", data);
        EditorUtility.SetDirty(data);
        var hit = GetData<MoveDef>("KitchenToastHit");
        hit.BodyDamage = hit.LegDamage = hit.GuardCost = 10;
        hit.StunFrames = 14;
        hit.HitStopFrames = hit.GaugeGain = 0;
        hit.Pushback = 2.6f;
        EditorUtility.SetDirty(hit);

        var toastSlices = new Transform[2];
        var landingMarkers = new Transform[2];
        for (int index = 0; index < toastSlices.Length; index++)
        {
            var slice = Group(hazard.transform, "Flying Toast " + (index + 1), Vector3.zero);
            Part(slice, "Toast Crust", Vector3.zero, new Vector3(.39f, .49f, .19f), crust);
            StagePart(slice, "Rounded Crown", PrimitiveType.Sphere, Vector3.up * .19f,
                new Vector3(.48f, .23f, .2f), crust);
            Part(slice, "Bread Face", new Vector3(0, .02f, -.104f), new Vector3(.29f, .39f, .025f), bread);
            slice.gameObject.SetActive(false);
            toastSlices[index] = slice;

            var marker = Group(hazard.transform, "Toast Landing " + (index + 1),
                new Vector3(-1.6f + index * 2.75f, .028f, 0));
            for (int sign = -1; sign <= 1; sign += 2)
                Part(marker, "Landing Cross", Vector3.zero, new Vector3(.83f, .035f, .13f), ember)
                    .transform.Rotate(0, sign * 45f, 0);
            marker.gameObject.SetActive(false);
            landingMarkers[index] = marker;
        }

        var belt = Group(hazard.transform, "Mixer Belt", new Vector3(3.2f, .015f, 0));
        Part(belt, "Rubber Belt", Vector3.zero, new Vector3(2.4f, .05f, 1.36f), ink);
        for (int index = -4; index <= 4; index++)
            Part(belt, "Belt Tread", new Vector3(index * .25f, .025f, 0), new Vector3(.035f, .02f, 1.25f), silver);
        var arrows = Group(belt, "Belt Direction", new Vector3(0, .052f, 0));
        for (int index = -1; index <= 1; index++)
            for (int sign = -1; sign <= 1; sign += 2)
                Part(arrows, "Chevron", new Vector3(index * .67f, 0, sign * .16f),
                    new Vector3(.48f, .023f, .09f), ivory).transform.Rotate(0, sign * 42f, 0);

        coils.gameObject.SetActive(false);
        arrows.gameObject.SetActive(false);
        var roundData = new SerializedObject(round);
        ConfigureKitchenArray(kitchen, "fighters", new[]
        {
            roundData.FindProperty("player").objectReferenceValue,
            roundData.FindProperty("opponent").objectReferenceValue
        });
        ConfigureKitchenArray(kitchen, "toastSlices", toastSlices);
        ConfigureKitchenArray(kitchen, "landingMarkers", landingMarkers);
        ConfigureKitchenArray(kitchen, "launchPoints", launchPoints);
        Set(kitchen, "toastHit", hit);
        Set(kitchen, "hotCoils", coils.gameObject);
        Set(kitchen, "belt", belt);
        Set(kitchen, "beltArrows", arrows);
        var sound = hazard.gameObject.AddComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.volume = .65f;
        Set(kitchen, "warningAudio", sound);
        Set(kitchen, "ticking", AssetDatabase.LoadAssetAtPath<AudioClip>(KitchenArt + "/ToasterTick.wav"));
        Set(kitchen, "mixerWhir", AssetDatabase.LoadAssetAtPath<AudioClip>(KitchenArt + "/MixerWhir.wav"));
        Set(round, "hazard", hazard);
        EditorSceneManager.SaveScene(scene, KitchenPath);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureKitchenArray(KitchenHazards kitchen, string field, Object[] values)
    {
        var data = new SerializedObject(kitchen);
        var array = data.FindProperty(field);
        array.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
            array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Part(Transform parent, string label, Vector3 position, Vector3 scale,
        Material material, bool keepCollider = false) =>
        StagePart(parent, label, PrimitiveType.Cube, position, scale, material, keepCollider);

    private static Material KitchenMaterial(string label, string hex, float metallic = 0f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        return StageMaterial(label, color, metallic, KitchenArt);
    }
}
