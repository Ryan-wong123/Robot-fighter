using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.UI;

public static class AppBootBuilder
{
    private const string Scenes = "Assets/_Project/Scenes/";
    private const string BootPath = Scenes + "Boot.unity";

    [MenuItem("Wobble Brawl/Create App Boot Scene")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        UnityEngine.SceneManagement.Scene scene;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath) == null)
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("App", typeof(AppStateMachine), typeof(AppFlowView));
        }
        else
            scene = EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);

        var app = scene.GetRootGameObjects().Select(root => root.GetComponent<AppStateMachine>())
            .Single(controller => controller != null);
        var configuration = new SerializedObject(app);
        configuration.FindProperty("blobby").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<CharacterDef>("Assets/_Project/Data/Combat/SandboxFighter.asset");
        var stages = configuration.FindProperty("stages");
        var entries = new[]
        {
            new StageChoice { Name = "Bathtub Brawl", Scene = "BathtubBrawl", Hazards = "Rising water · Duck wave" },
            new StageChoice { Name = "Kitchen Counter", Scene = "KitchenCounter", Hazards = "Flying toast · Mixer belt" }
        };
        stages.arraySize = entries.Length;
        for (int index = 0; index < entries.Length; index++)
        {
            var entry = stages.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Name").stringValue = entries[index].Name;
            entry.FindPropertyRelative("Scene").stringValue = entries[index].Scene;
            entry.FindPropertyRelative("Hazards").stringValue = entries[index].Hazards;
            entry.FindPropertyRelative("Thumbnail").objectReferenceValue =
                Resources.Load<Texture2D>("StagePreviews/" + entries[index].Scene);
            entry.FindPropertyRelative("Thumbnail").objectReferenceValue =
                Resources.Load<Texture2D>("StagePreviews/" + entries[index].Scene);
        }
        configuration.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, BootPath);

        string[] appScenes = new[] { BootPath }.Concat(entries.Select(entry => Scenes + entry.Scene + ".unity")).ToArray();
        EditorBuildSettings.scenes = appScenes.Select(path => new EditorBuildSettingsScene(path, true))
            .Concat(EditorBuildSettings.scenes.Where(scene => !appScenes.Contains(scene.path))).ToArray();
    }
}
