using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Core;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;
using Object = UnityEngine.Object;

public sealed class NoveltyItemTests
{
    [Test]
    public void VisualLookup_HandlesCollapsedRootMeshNamesAndInactiveAction()
    {
        var root = new GameObject("CreamPie");
        var body = new Mesh { name = "SM_CreamPie_Body" };
        var actionMesh = new Mesh { name = "SM_BubbleWand_Action" };
        try
        {
            root.AddComponent<MeshFilter>().sharedMesh = body;
            var lookup = Type.GetType("NoveltyItemsContent, Assembly-CSharp-Editor", true).GetMethod("FindVisual");
            Assert.That(lookup.Invoke(null, new object[] { root, "_Body", true }), Is.EqualTo(root.transform));
            body.name = "ImportedMesh";
            Assert.That(lookup.Invoke(null, new object[] { root, "_Body", true }), Is.EqualTo(root.transform));
            body.name = "SM_CreamPie_Body";
            var action = new GameObject("Renamed child", typeof(MeshFilter));
            action.transform.SetParent(root.transform, false);
            action.GetComponent<MeshFilter>().sharedMesh = actionMesh;
            action.SetActive(false);
            Assert.That(lookup.Invoke(null, new object[] { root, "_Action", true }), Is.EqualTo(action.transform));
            Assert.That(lookup.Invoke(null, new object[] { root, "_Warning", false }), Is.Null);
            var error = Assert.Throws<TargetInvocationException>(() =>
                lookup.Invoke(null, new object[] { root, "_Warning", true }));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(body);
            Object.DestroyImmediate(actionMesh);
        }
    }

    [Test]
    public void StatusTimers_RefreshExpireBreakAndResetWithoutMultiplyingSlow()
    {
        var status = new ItemStatusState();
        status.Slow(90, 65);
        status.Slow(30, 80);
        Assert.That(status.SlowFrames, Is.EqualTo(90));
        Assert.That(status.SlowPercent, Is.EqualTo(65));
        for (int frame = 0; frame < 89; frame++) status.Advance();
        Assert.That(status.SlowPercent, Is.EqualTo(65));
        status.Advance();
        Assert.That(status.SlowPercent, Is.EqualTo(100));
        status.Slow(0, 50);
        Assert.That(status.SlowPercent, Is.EqualTo(100));
        status.Trap(120);
        status.Advance();
        Assert.That(status.TrapFrames, Is.EqualTo(119));
        status.BreakTrap();
        Assert.That(status.TrapFrames, Is.Zero);
        status.Trap(10);
        status.Slow(10, 30);
        status.Reset();
        Assert.That(status.TrapFrames + status.SlowFrames, Is.Zero);
        Assert.That(status.SlowPercent, Is.EqualTo(100));
    }

    [Test]
    public void Catalog_HasSixUniqueOriginalModelsAndConfiguredGameplay()
    {
        EnsureContent();
        var catalog = Resources.Load<LaunchItemCatalog>("Items/LaunchItems");
        Assert.That(catalog.Items.Length, Is.EqualTo(6));
        Assert.That(catalog.Items.Select(item => item.Id).Distinct().Count(), Is.EqualTo(6));
        foreach (var item in catalog.Items)
        {
            Assert.That(item.Prefab.GetComponent<NoveltyItem>(), Is.Not.Null);
            Assert.That(item.Prefab.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(30));
            Assert.That(item.AIUseRange.y, Is.GreaterThan(0f));
        }
        var jack = catalog.Items.Single(item => item.NoveltyKind == NoveltyItemKind.BoxingJack);
        Assert.That(jack.TelegraphFrames, Is.EqualTo(90));
        Assert.That(jack.ContactHit.LaunchSpeed, Is.EqualTo(6f));
        Assert.That(catalog.Items.Single(item => item.NoveltyKind == NoveltyItemKind.WindUpCar).ContactHit.KnocksDown, Is.True);
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Project/Scenes/CombatSandbox.unity"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator PooledAdapters_ApplyAllSixEffectsAndReleaseTrapOnRecycle()
    {
        EnsureContent();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatSandbox.unity");
        yield return new EnterPlayMode();
        yield return null;
        var round = Object.FindFirstObjectByType<RoundController>();
        round.Opponent.GetComponent<AIInput>().TogglePause();
        var spawner = Object.FindFirstObjectByType<ItemSpawner>();
        var table = (ItemSpawner.Entry[])Field(spawner, "table");
        Assert.That(table.Select(entry => entry.Item.Id).Distinct().Count(), Is.EqualTo(12));
        spawner.enabled = false;
        var owner = round.Player;
        var target = round.Opponent;
        var root = new GameObject("Controlled item pool");
        var catalog = Resources.Load<LaunchItemCatalog>("Items/LaunchItems");

        foreach (var definition in catalog.Items)
        {
            owner.ResetRound(new Vector3(-2f, .02f, 0f), 1);
            target.ResetRound(new Vector3(0f, .02f, 0f), -1);
            var item = Object.Instantiate(definition.Prefab, root.transform);
            Call(item, "Initialize", definition, root.transform);
            Call(item, "Spawn", new Vector3(-100f, .65f, 0f));
            Assert.That(Call(owner.GetComponent<ItemHolder>(), "TryPickup", item), Is.EqualTo(true));
            Assert.That(Call(owner.GetComponent<ItemHolder>(), "TryUse"), Is.EqualTo(true));
            var novelty = item.GetComponent<NoveltyItem>();
            int health = target.Health;
            switch (definition.NoveltyKind)
            {
                case NoveltyItemKind.CreamPie:
                    Call(novelty, "Contact", target);
                    Assert.That(target.Health, Is.EqualTo(health - 6));
                    Assert.That(target.IsItemSlowed, Is.True);
                    break;
                case NoveltyItemKind.Boomerang:
                    Call(novelty, "Contact", target);
                    int afterFirst = target.Health;
                    Call(novelty, "Contact", target);
                    Assert.That(target.Health, Is.EqualTo(afterFirst));
                    typeof(NoveltyItem).GetField("returning", Private).SetValue(novelty, true);
                    Call(novelty, "Contact", target);
                    Assert.That(target.Health, Is.LessThan(afterFirst));
                    int afterReturn = target.Health;
                    Call(novelty, "Contact", target);
                    Assert.That(target.Health, Is.EqualTo(afterReturn));
                    break;
                case NoveltyItemKind.BubbleWand:
                    Call(novelty, "Contact", target);
                    Assert.That(target.IsBubbleTrapped, Is.True);
                    Assert.That(target.Health, Is.EqualTo(health));
                    var move = ScriptableObject.CreateInstance<MoveDef>();
                    move.HitStopFrames = 0;
                    target.ReceiveHit(new HitInfo(owner, move, HurtZone.Body, target.transform.position, Vector3.right));
                    Assert.That(target.IsBubbleTrapped, Is.False);
                    Object.Destroy(move);
                    target.ResetRound(Vector3.zero, -1);
                    Call(item, "Recycle");
                    Call(item, "Spawn", new Vector3(-100f, .65f, 0));
                    Call(owner.GetComponent<ItemHolder>(), "TryPickup", item);
                    Call(owner.GetComponent<ItemHolder>(), "TryUse");
                    Call(novelty, "Contact", target);
                    Assert.That(target.IsBubbleTrapped, Is.True);
                    Call(item, "Recycle");
                    Assert.That(target.IsBubbleTrapped, Is.False);
                    break;
                case NoveltyItemKind.WindUpCar:
                    Call(novelty, "Contact", target);
                    Assert.That(target.State, Is.EqualTo(FighterState.KnockedDown));
                    Assert.That(target.Health, Is.EqualTo(health - 10));
                    break;
                case NoveltyItemKind.BoxingJack:
                    target.GetComponent<Rigidbody>().position = new Vector3(-.2f, .02f, 0f);
                    for (int tick = 0; tick < 89; tick++) Call(novelty, "Tick");
                    Assert.That(target.Health, Is.EqualTo(health));
                    Call(novelty, "Tick");
                    Assert.That(target.Health, Is.EqualTo(health - 12));
                    Assert.That(target.GetComponent<Rigidbody>().linearVelocity.y, Is.GreaterThanOrEqualTo(6f));
                    // Round-end recycling may occur inside the hit callback; it must leave the glove reset.
                    Call(item, "Recycle");
                    Call(item, "Spawn", new Vector3(-100f, .65f, 0));
                    Call(owner.GetComponent<ItemHolder>(), "TryPickup", item);
                    Call(owner.GetComponent<ItemHolder>(), "TryUse");
                    Call(target, "SetVitals", 1, target.Guard, target.Special, true);
                    Action<RoundResult> cleanup = result => { Call(item, "Recycle"); };
                    round.Ended += cleanup;
                    for (int tick = 0; tick < 90; tick++) Call(novelty, "Tick");
                    round.Ended -= cleanup;
                    Assert.That(item.gameObject.activeSelf, Is.False);
                    Assert.That(((Transform)Field(novelty, "actionVisual")).localScale,
                        Is.EqualTo((Vector3)Field(novelty, "actionScale")));
                    break;
                case NoveltyItemKind.PogoStick:
                    Assert.That(owner.GetComponent<Rigidbody>().linearVelocity.y, Is.EqualTo(10f));
                    owner.GetComponent<Rigidbody>().position = new Vector3(-1f, .02f, 0);
                    owner.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                    item.GetComponent<Rigidbody>().position = new Vector3(-1f, -.33f, 0);
                    for (int tick = 0; tick < 7; tick++) Call(novelty, "Tick");
                    Assert.That(target.Health, Is.EqualTo(health));
                    Assert.That(target.GetComponent<Rigidbody>().linearVelocity.x, Is.GreaterThan(5f));
                    break;
            }
            Call(item, "Recycle");
            Assert.That(item.gameObject.activeSelf, Is.False);
            target.ResetRound(Vector3.zero, -1);
            Assert.That(target.IsBubbleTrapped || target.IsItemSlowed, Is.False);
            Object.Destroy(item.gameObject);
        }
        Object.Destroy(root);
        yield return new ExitPlayMode();
    }

    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);

    private static void EnsureContent()
    {
        if (Resources.Load<LaunchItemCatalog>("Items/LaunchItems") == null)
            Type.GetType("NoveltyItemsContent, Assembly-CSharp-Editor", true).GetMethod("Build").Invoke(null, null);
    }
}
