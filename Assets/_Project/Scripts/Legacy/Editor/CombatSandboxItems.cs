using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public static partial class CombatSandboxBuilder
{
    [MenuItem("Wobble Brawl/Configure Combat Sandbox Items")]
    public static void AddCombatItems()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!root.TryGetComponent<RoundController>(out var round))
                continue;
            ConfigureTestPickup(round);
            ConfigureCombatItems(round);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return;
        }
        throw new System.InvalidOperationException("CombatSandbox requires a round controller.");
    }

    private static void ConfigureCombatItems(RoundController round)
    {
        var roundProperties = new SerializedObject(round);
        var spawner = (ItemSpawner)roundProperties.FindProperty("itemSpawner").objectReferenceValue;
        var banana = MakeItem("Banana Peel", ItemUseMode.Placed, PrimitiveType.Capsule,
            new Color(1f, .83f, .08f), new Vector3(.33f, .18f, .38f), CombatItemAction.Strike);
        SetItemHit(banana, 0, 18, 4f);
        var glove = MakeItem("Foam Glove", ItemUseMode.OnPickup, PrimitiveType.Sphere,
            new Color(.95f, .16f, .30f), new Vector3(.56f, .48f, .45f), CombatItemAction.BoostHits);
        CombatEffect(glove).ExtraPushback = 5f;
        var chicken = MakeItem("Rubber Chicken", ItemUseMode.Thrown, PrimitiveType.Capsule,
            new Color(1f, .61f, .13f), new Vector3(.27f, .43f, .28f), CombatItemAction.Strike);
        chicken.ThrowSpeed = 5f;
        chicken.ThrowLift = 1.5f;
        chicken.GroundBounces = 2;
        chicken.BounceSpeed = 2.3f;
        SetItemHit(chicken, 10, 14, 3f);
        var rocket = MakeItem("Rocket Plunger", ItemUseMode.Held, PrimitiveType.Cylinder,
            new Color(.13f, .72f, .95f), new Vector3(.37f, .30f, .37f), CombatItemAction.Launch);
        CombatEffect(rocket).LaunchVelocity = new Vector2(9f, 3.5f);
        var cushion = MakeItem("Whoopee Cushion", ItemUseMode.Held, PrimitiveType.Sphere,
            new Color(.93f, .35f, .76f), new Vector3(.60f, .24f, .52f), CombatItemAction.AreaStrike);
        CombatEffect(cushion).Radius = 2.4f;
        SetItemHit(cushion, 0, 24, 5f);
        var anvil = MakeItem("Anvil", ItemUseMode.Placed, PrimitiveType.Cube,
            new Color(.29f, .25f, .46f), new Vector3(.62f, .44f, .45f), CombatItemAction.Strike);
        anvil.PlacementOffset = 1.3f;
        anvil.DropHeight = 3.5f;
        anvil.TelegraphFrames = 90;
        SetItemHit(anvil, 18, 24, 5f);

        ItemDef[] items = { banana, glove, chicken, rocket, cushion, anvil };
        float[] positions = { -5.2f, -3.5f, -0.85f, 0.85f, 3.5f, 5.2f };
        var properties = new SerializedObject(spawner);
        properties.FindProperty("spawnOpeningLineup").boolValue = true;
        var points = properties.FindProperty("spawnPoints");
        var table = properties.FindProperty("table");
        points.arraySize = table.arraySize = items.Length;
        for (int index = 0; index < items.Length; index++)
        {
            var item = items[index];
            var point = spawner.transform.Find(item.Id + " Spawn");
            if (point == null)
            {
                point = new GameObject(item.Id + " Spawn").transform;
                point.SetParent(spawner.transform, false);
            }
            point.position = new Vector3(positions[index], .55f, 0f);
            points.GetArrayElementAtIndex(index).objectReferenceValue = point;
            var entry = table.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Item").objectReferenceValue = item;
            entry.FindPropertyRelative("Weight").intValue = 1;
            entry.FindPropertyRelative("Capacity").intValue = 1;
            EditorUtility.SetDirty(item);
            EditorUtility.SetDirty(CombatEffect(item));
        }
        properties.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ItemDef MakeItem(string label, ItemUseMode mode, PrimitiveType primitive,
        Color color, Vector3 scale, CombatItemAction action)
    {
        string key = label.Replace(" ", "");
        var item = GetData<ItemDef>(key);
        var effect = GetData<CombatItemEffect>(key + "Effect");
        effect.Action = action;
        item.Id = label;
        item.Mode = mode;
        item.Effects = new ItemEffect[] { effect };
        string path = "Assets/_Project/Prefabs/" + key + ".prefab";
        item.Prefab = AssetDatabase.LoadAssetAtPath<PooledItem>(path);
        if (item.Prefab != null)
            return item;

        var material = AssetDatabase.LoadAssetAtPath<Material>(DataFolder + "/" + key + ".mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            AssetDatabase.CreateAsset(material, DataFolder + "/" + key + ".mat");
        }
        var visual = GameObject.CreatePrimitive(primitive);
        visual.name = label;
        visual.transform.localScale = scale;
        visual.GetComponent<Renderer>().sharedMaterial = material;
        var pooledItem = visual.AddComponent<PooledItem>();
        visual.GetComponent<Collider>().isTrigger = true;
        visual.GetComponent<Rigidbody>().useGravity = false;
        item.Prefab = PrefabUtility.SaveAsPrefabAsset(pooledItem.gameObject, path).GetComponent<PooledItem>();
        Object.DestroyImmediate(visual);
        return item;
    }

    private static CombatItemEffect CombatEffect(ItemDef item) => (CombatItemEffect)item.Effects[0];

    private static void SetItemHit(ItemDef item, int damage, int stunFrames, float pushback)
    {
        var move = GetData<MoveDef>(item.Id.Replace(" ", "") + "Hit");
        move.BodyDamage = move.LegDamage = damage;
        move.StunFrames = stunFrames;
        move.Pushback = pushback;
        move.HitStopFrames = 0;
        move.GaugeGain = 0;
        move.GuardCost = damage;
        CombatEffect(item).Hit = move;
        EditorUtility.SetDirty(move);
    }
}
