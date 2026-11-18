using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WobbleBrawl.Flow;
using WobbleBrawl.UI;

public static partial class CombatSandboxBuilder
{
    [MenuItem("Wobble Brawl/Wire Match HUD Scenes")]
    public static void AddMatchHud()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        foreach (string path in new[] { ScenePath, BathtubPath, KitchenPath })
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!root.TryGetComponent<RoundController>(out var round))
                    continue;
                ConfigureMatchHud(root, round);
                EditorSceneManager.SaveScene(scene);
                break;
            }
        }
    }

    private static void ConfigureMatchHud(GameObject root, RoundController round)
    {
        if (!root.TryGetComponent<MatchHud>(out var hud))
            hud = root.AddComponent<MatchHud>();
        Set(hud, "match", root.GetComponent<MatchController>());
        Set(hud, "round", round);
    }
}
