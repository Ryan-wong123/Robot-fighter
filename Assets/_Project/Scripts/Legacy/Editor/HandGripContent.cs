using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WobbleBrawl.Gameplay;

/// <summary>Repairs original grip references using bind-mesh hand weights, without runtime bone-name searches.</summary>
[InitializeOnLoad]
public static class HandGripContent
{
    private static bool building;
    static HandGripContent()
    {
        EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
        };
    }

    public static void Install()
    {
        if (building || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Install;
            return;
        }
        for (int index = 0; index < SceneManager.sceneCount; index++)
            if (SceneManager.GetSceneAt(index).isDirty) return;
        Repair();
    }

    [MenuItem("Wobble Brawl/Items/Repair Fighter Hand Grips")]
    public static void Repair()
    {
        if (building || EditorApplication.isPlayingOrWillChangePlaymode) return;
        for (int index = 0; index < SceneManager.sceneCount; index++)
            if (SceneManager.GetSceneAt(index).isDirty)
                throw new InvalidOperationException("Save scene edits before repairing hand grips.");
        building = true;
        Scene previous = SceneManager.GetActiveScene();
        try
        {
            foreach (string name in new[] { "BlobbyFighter", "MadamePlushVisual", "MadamePlushFighter" })
            {
                string path = "Assets/_Project/Prefabs/" + name + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = RepairRoot(root);
                    if (name == "MadamePlushVisual") changed |= BindVisual(root);
                    if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string name in new[] { "CombatSandbox", "BathtubBrawl", "KitchenCounter", "BouncyCastleFair", "GiantSandbox" })
            {
                string path = "Assets/_Project/Scenes/" + name + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                var scene = SceneManager.GetSceneByPath(path);
                bool loaded = scene.isLoaded;
                if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    bool changed = false;
                    foreach (var root in scene.GetRootGameObjects()) changed |= RepairRoot(root);
                    if (changed)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                }
                finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssets();
        }
        finally
        {
            building = false;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static bool RepairRoot(GameObject root)
    {
        bool changed = false;
        foreach (var fighter in root.GetComponentsInChildren<Fighter>(true))
        {
            var fighterData = new SerializedObject(fighter);
            var definition = fighterData.FindProperty("character").objectReferenceValue;
            if (definition == null) continue;
            string path = AssetDatabase.GetAssetPath(definition);
            if (!path.EndsWith("/SandboxFighter.asset") && !path.EndsWith("/MadamePlush.asset")) continue;
            var animator = (Animator)fighterData.FindProperty("animator").objectReferenceValue;
            if (animator == null) continue;
            changed |= BindVisual(animator.gameObject);
            var holder = fighter.GetComponent<ItemHolder>();
            if (holder == null) continue;
            var data = new SerializedObject(holder);
            var socket = animator.GetComponent<FighterVisual>().ItemSocket;
            if (data.FindProperty("handSocket").objectReferenceValue == socket) continue;
            data.FindProperty("handSocket").objectReferenceValue = socket;
            data.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }
        return changed;
    }

    public static bool BindVisual(GameObject visual)
    {
        var hand = visual.GetComponentsInChildren<Transform>(true).Single(bone => bone.name == "Hand.L");
        Vector3 center = Vector3.zero;
        float total = 0f;
        foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            int bone = Array.IndexOf(renderer.bones, hand);
            if (bone < 0) continue;
            var mesh = renderer.sharedMesh;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            // Weight the bind-space vertices assigned to the hand, including the glove/palm.
            for (int index = 0; index < weights.Length; index++)
            {
                var weight = weights[index];
                float value = (weight.boneIndex0 == bone ? weight.weight0 : 0f) +
                    (weight.boneIndex1 == bone ? weight.weight1 : 0f) +
                    (weight.boneIndex2 == bone ? weight.weight2 : 0f) +
                    (weight.boneIndex3 == bone ? weight.weight3 : 0f);
                if (value < .5f) continue;
                center += hand.TransformPoint(mesh.bindposes[bone].MultiplyPoint3x4(vertices[index])) * value;
                total += value;
            }
        }
        if (total <= 0f) throw new InvalidOperationException("No hand-weighted vertices for item grip.");
        Vector3 localCenter = hand.InverseTransformPoint(center / total);
        var references = visual.GetComponent<FighterVisual>();
        bool changed = references == null;
        if (references == null) references = visual.AddComponent<FighterVisual>();
        var socket = references.ItemSocket;
        if (socket == null)
        {
            socket = new GameObject("Item Socket").transform;
            references.ItemSocket = socket;
            changed = true;
        }
        if (socket.parent != hand || (socket.localPosition - localCenter).sqrMagnitude > .00000001f ||
            Quaternion.Angle(socket.localRotation, Quaternion.identity) > .001f)
        {
            socket.SetParent(hand, false);
            socket.SetLocalPositionAndRotation(localCenter, Quaternion.identity);
            changed = true;
        }
        if (changed) EditorUtility.SetDirty(references);
        return changed;
    }
}
