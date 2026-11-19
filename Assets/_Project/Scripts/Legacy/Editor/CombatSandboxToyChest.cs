using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public static partial class CombatSandboxBuilder
{
    private const string ToyChestPath = "Assets/_Project/Scenes/ToyChestShowdown.unity";
    private const string ToyChestArt = "Assets/_Project/Art/ToyChestShowdown";

    [MenuItem("Wobble Brawl/Create Toy Chest Showdown")]
    public static void BuildToyChest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build Toy Chest Showdown outside Play Mode.");
        var boss = AssetDatabase.LoadAssetAtPath<CharacterDef>(MrJackContent.DefinitionPath);
        if (boss == null || boss.VisualPrefab == null)
            throw new InvalidOperationException("Run Wobble Brawl > Characters > Build Mr. Jack first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ToyChestPath) != null)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var existing = EditorSceneManager.OpenScene(ToyChestPath);
            var controller = existing.GetRootGameObjects().Select(root => root.GetComponent<MatchController>())
                .Single(component => component != null);
            Set(controller, "stageOpponent", boss);
            EditorSceneManager.MarkSceneDirty(existing);
            EditorSceneManager.SaveScene(existing);
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder(ToyChestArt))
            AssetDatabase.CreateFolder("Assets/_Project/Art", "ToyChestShowdown");

        var (scene, round, floor) = OpenStageTemplate();
        var roundData = new SerializedObject(round);
        ((Transform)roundData.FindProperty("playerSpawn").objectReferenceValue).position = new Vector3(-2, .02f, 0);
        ((Transform)roundData.FindProperty("opponentSpawn").objectReferenceValue).position = new Vector3(2, .02f, 0);
        var stage = new GameObject("Toy Chest Environment").transform;
        var plum = StageMaterial("Chest Plum", new Color(.25f, .1f, .38f), ToyChestArt);
        var wood = StageMaterial("Honey Wood", new Color(.66f, .36f, .18f), ToyChestArt);
        var trim = StageMaterial("Chest Brass", new Color(.98f, .68f, .25f), ToyChestArt);
        var block = StageMaterial("Block Coral", new Color(.84f, .23f, .29f), ToyChestArt);
        var dark = StageMaterial("Chest Ink", new Color(.12f, .055f, .18f), ToyChestArt);
        var deck = Group(stage, "Tilting deck", Vector3.zero);
        floor.GetComponent<Renderer>().enabled = false;
        for (int i = 0; i < 14; i++)
            StagePart(deck, "Wood plank", PrimitiveType.Cube, new Vector3(i - 6.5f, -.17f, 0),
                new Vector3(.98f, .34f, 4.8f), wood);
        StagePart(stage, "Chest back", PrimitiveType.Cube, new Vector3(0, 1.25f, 2.6f),
            new Vector3(14.6f, 3f, .55f), plum);
        var lid = StagePart(stage, "Propped lid", PrimitiveType.Cube, new Vector3(0, 4.8f, 3.3f),
            new Vector3(14.6f, 3.5f, .45f), plum);
        lid.transform.localRotation = Quaternion.Euler(-26f, 0, 0);
        for (int side = -1; side <= 1; side += 2)
        {
            var wall = StagePart(stage, "Chest end", PrimitiveType.Cube, new Vector3(side * 7.23f, .35f, 0),
                new Vector3(.46f, 1.4f, 4.8f), plum, true);
            wall.layer = 8;
            StagePart(stage, "Brass hinge", PrimitiveType.Cube, new Vector3(side * 5.2f, 2.72f, 2.24f),
                new Vector3(.65f, .35f, .15f), trim);
        }
        StagePart(stage, "Front rim", PrimitiveType.Cube, new Vector3(0, -.35f, -2.4f),
            new Vector3(14.7f, .38f, .35f), plum);
        StagePart(stage, "Music box", PrimitiveType.Cube, new Vector3(-5.35f, .63f, 1.8f),
            new Vector3(1.55f, 1.25f, 1.1f), block);
        for (int i = 0; i < 7; i++)
            StagePart(stage, "Music box tine", PrimitiveType.Cube, new Vector3(-5.95f + .2f * i, 1.27f, 1.75f),
                new Vector3(.12f, .1f, .7f - .07f * i), trim);
        StagePart(stage, "Teddy head", PrimitiveType.Sphere, new Vector3(3.8f, 3.05f, 2.36f),
            new Vector3(1.6f, 1.25f, .9f), wood);
        for (int side = -1; side <= 1; side += 2)
        {
            StagePart(stage, "Teddy ear", PrimitiveType.Sphere, new Vector3(3.8f + .65f * side, 3.55f, 2.38f),
                Vector3.one * .49f, wood);
            StagePart(stage, "Teddy eye", PrimitiveType.Sphere, new Vector3(3.8f + .27f * side, 3.13f, 1.93f),
                Vector3.one * .14f, dark);
        }
        StagePart(stage, "Teddy muzzle", PrimitiveType.Sphere, new Vector3(3.8f, 2.89f, 1.92f),
            new Vector3(.55f, .35f, .28f), trim);

        var node = new GameObject("Toy Chest Hazard Cycle");
        var cycle = node.AddComponent<HazardController>();
        var chest = node.AddComponent<ToyChestHazards>();
        var timing = GetData<HazardDef>("ToyChestCycle");
        timing.WarningSeconds = 2f;
        timing.ActiveSeconds = 4f;
        timing.Hit = null;
        EditorUtility.SetDirty(timing);
        var hit = GetData<MoveDef>("ToyChestBlockHit");
        hit.BodyDamage = hit.LegDamage = 10;
        hit.Pushback = 3;
        hit.LaunchSpeed = 4;
        hit.GaugeGain = 0;
        EditorUtility.SetDirty(hit);
        Set(cycle, "definition", timing);
        Set(round, "hazard", cycle);
        Set(chest, "blockHit", hit);
        Set(chest, "player", round.Player);
        Set(chest, "opponent", round.Opponent);
        Set(chest, "deck", deck);
        Set(chest, "deckFloor", floor.GetComponent<BoxCollider>());
        var arrow = Group(node.transform, "Tilt direction", new Vector3(0, .055f, -1.05f));
        StagePart(arrow, "Arrow shaft", PrimitiveType.Cube, Vector3.zero, new Vector3(1.35f, .025f, .14f), trim);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            var tip = StagePart(arrow, "Arrow head", PrimitiveType.Cube, new Vector3(.62f, 0, sign * .19f),
                new Vector3(.6f, .025f, .13f), trim);
            tip.transform.localRotation = Quaternion.Euler(0, sign * 43f, 0);
        }
        arrow.gameObject.SetActive(false);
        Set(chest, "tiltWarning", arrow);

        var blocks = new Transform[3];
        var shadows = new Transform[3];
        for (int i = 0; i < blocks.Length; i++)
        {
            var position = new Vector3((i - 1) * 4f, .035f, 0);
            shadows[i] = StagePart(node.transform, "Growing shadow", PrimitiveType.Cylinder, position,
                new Vector3(1.5f, .02f, 1.5f), trim).transform;
            blocks[i] = StagePart(node.transform, "Alphabet block", PrimitiveType.Cube, position,
                Vector3.one * .8f, block, true).transform;
            blocks[i].gameObject.layer = 8;
            var letter = Group(blocks[i], "Letter", new Vector3(0, 0, -.515f)).gameObject.AddComponent<TextMesh>();
            letter.text = ((char)('A' + i)).ToString();
            letter.anchor = TextAnchor.MiddleCenter;
            letter.fontSize = 64;
            letter.characterSize = .12f;
            letter.color = Color.white;
            blocks[i].gameObject.SetActive(false);
            shadows[i].gameObject.SetActive(false);
        }
        ConfigureKitchenArray(chest, "blocks", blocks);
        ConfigureKitchenArray(chest, "shadows", shadows);
        var warning = node.AddComponent<AudioSource>();
        warning.playOnAwake = false;
        node.AddComponent<SettingsAudioSource>();
        var music = new GameObject("Toy Chest Music");
        var source = music.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>("Audio/ToyChestMusic");
        source.loop = true;
        source.volume = .35f;
        Set(chest, "music", source);
        Set(chest, "warning", warning);
        Set(chest, "rattle", Resources.Load<AudioClip>("Audio/ToyChestRattle"));
        Set(chest, "creak", Resources.Load<AudioClip>("Audio/ToyChestCreak"));

        var match = round.GetComponent<MatchController>();
        var matchData = new SerializedObject(match);
        matchData.FindProperty("isBossStage").boolValue = true;
        matchData.ApplyModifiedPropertiesWithoutUndo();
        Set(match, "stageOpponent", boss);

        if (!EditorSceneManager.SaveScene(scene, ToyChestPath))
            throw new InvalidOperationException("Could not save Toy Chest Showdown.");
        EditorBuildSettings.scenes = EditorBuildSettings.scenes
            .Where(entry => entry.path != ToyChestPath)
            .Append(new EditorBuildSettingsScene(ToyChestPath, true)).ToArray();
        AssetDatabase.SaveAssets();
    }
}
