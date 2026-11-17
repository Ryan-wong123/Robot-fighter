using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Gameplay;

public sealed class CombatItemTests
{
    [UnityTest]
    public IEnumerator RoundOpensWithSixPickupsAndChickenCanBeThrown()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatSandbox.unity");
        yield return new EnterPlayMode();
        yield return null;

        var pickups = Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None);
        Assert.That(pickups.Length, Is.EqualTo(6));
        Assert.That(pickups.Select(item => item.Definition.Id), Is.EquivalentTo(new[]
        {
            "Banana Peel", "Foam Glove", "Rubber Chicken",
            "Rocket Plunger", "Whoopee Cushion", "Anvil"
        }));

        var chicken = pickups.Single(item => item.Definition.Id == "Rubber Chicken");
        var player = Object.FindObjectsByType<Fighter>(FindObjectsSortMode.None).Single(fighter => fighter.Team == 0);
        var holder = player.GetComponent<ItemHolder>();
        player.GetComponent<Rigidbody>().position = new Vector3(chicken.transform.position.x, 0.02f, 0f);
        for (int step = 0; step < 10 && !holder.HasItem; step++)
            yield return new WaitForFixedUpdate();
        Assert.That(holder.HasItem, Is.True);
        Assert.That(chicken.transform.IsChildOf(player.transform), Is.True);

        var use = typeof(ItemHolder).GetMethod("TryUse", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(use.Invoke(holder, null), Is.EqualTo(true));
        Assert.That(holder.HasItem, Is.False);
        Assert.That(chicken.GetComponent<Rigidbody>().linearVelocity.x, Is.GreaterThan(0f));
        Assert.That(chicken.transform.position.z, Is.EqualTo(player.transform.position.z).Within(.01f));

        yield return new ExitPlayMode();
    }
}
