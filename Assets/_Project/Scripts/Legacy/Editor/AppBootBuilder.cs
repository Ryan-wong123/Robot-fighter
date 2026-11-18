using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath) == null)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("App", typeof(AppStateMachine), typeof(AppFlowView));
            EditorSceneManager.SaveScene(scene, BootPath);
        }

        string[] appScenes = { BootPath, Scenes + "CombatSandbox.unity" };
        EditorBuildSettings.scenes = appScenes.Select(path => new EditorBuildSettingsScene(path, true))
            .Concat(EditorBuildSettings.scenes.Where(scene => !appScenes.Contains(scene.path))).ToArray();
    }
}
