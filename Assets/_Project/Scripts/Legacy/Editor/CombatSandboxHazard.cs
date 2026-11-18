using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public static partial class CombatSandboxBuilder
{
    [MenuItem("Wobble Brawl/Configure Combat Sandbox Hazard")]
    public static void AddHazard()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!root.TryGetComponent<RoundController>(out var round))
                continue;
            ConfigureHazard(round);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return;
        }
        throw new System.InvalidOperationException("CombatSandbox requires a round controller.");
    }

    private static void ConfigureHazard(RoundController round)
    {
        if (new SerializedObject(round).FindProperty("hazard").objectReferenceValue != null)
            return;

        var data = GetData<HazardDef>("SandboxHazard");
        data.WarningSeconds = 2f;
        data.Hit = GetData<MoveDef>("SandboxHazardHit");
        data.Hit.BodyDamage = data.Hit.LegDamage = 12;
        data.Hit.StunFrames = 18;
        data.Hit.Pushback = 5f;
        data.Hit.GaugeGain = 0;
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(data.Hit);

        var hazard = new GameObject("Sandbox Stage Hazard").AddComponent<HazardController>();
        hazard.transform.position = Vector3.up * data.Size.y * .5f;
        var warning = CreateHazardMarker(hazard.transform, data.Size, "Hazard Warning",
            new Color(1f, .76f, .04f), .06f);
        var active = CreateHazardMarker(hazard.transform, data.Size, "Hazard Active",
            new Color(1f, .22f, .06f), .22f);
        Set(hazard, "definition", data);
        Set(hazard, "warningMarker", warning);
        Set(hazard, "activeMarker", active);
        Set(round, "hazard", hazard);
    }

    private static GameObject CreateHazardMarker(Transform parent, Vector3 size,
        string label, Color color, float height)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.name = label;
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = Vector3.down * ((size.y - height) * .5f - .03f);
        marker.transform.localScale = new Vector3(size.x, height, size.z);

        string path = DataFolder + "/" + label.Replace(" ", "") + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
        }
        marker.GetComponent<Renderer>().sharedMaterial = material;
        marker.SetActive(false);
        return marker;
    }
}
