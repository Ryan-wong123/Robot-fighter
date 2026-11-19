using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WobbleBrawl.Gameplay;

public sealed class HandGripTests
{
    [TestCase("BlobbyFighter")]
    [TestCase("MadamePlushVisual")]
    public void GripFollowsHandAndKeepsItemScaleThroughReuse(string prefab)
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/" + prefab + ".prefab");
        var pool = new GameObject("Test item pool");
        var itemObject = new GameObject("Test item", typeof(Rigidbody), typeof(BoxCollider));
        var definition = ScriptableObject.CreateInstance<ItemDef>();
        try
        {
            var visual = root.GetComponentInChildren<Animator>(true).gameObject;
            var repair = System.Type.GetType("HandGripContent, Assembly-CSharp-Editor");
            Assert.That(repair, Is.Not.Null);
            var bind = repair.GetMethod("BindVisual");
            bind.Invoke(null, new object[] { visual });
            Assert.That(bind.Invoke(null, new object[] { visual }), Is.EqualTo(false), "Repair is idempotent.");
            var socket = visual.GetComponent<FighterVisual>().ItemSocket;
            Assert.That(socket.parent.name, Is.EqualTo("Hand.L"));
            Assert.That(Vector3.Distance(socket.position, socket.parent.position), Is.LessThan(.25f));
            itemObject.transform.SetParent(pool.transform, false);
            itemObject.transform.localScale = Vector3.one * .37f;
            var item = itemObject.AddComponent<PooledItem>();
            Invoke(item, "Initialize", definition, pool.transform);
            Invoke(item, "Spawn", Vector3.zero);
            Invoke(item, "Attach", null, socket);
            Assert.That(item.transform.position, Is.EqualTo(socket.position));
            Assert.That(item.transform.lossyScale.x, Is.EqualTo(.37f).Within(.001f));
            socket.parent.localRotation *= Quaternion.Euler(0f, 0f, 35f);
            Assert.That(Vector3.Distance(item.transform.position, socket.position), Is.LessThan(.0001f));
            Invoke(item, "Recycle");
            Assert.That(item.transform.parent, Is.EqualTo(pool.transform));
            Assert.That(item.transform.localScale, Is.EqualTo(Vector3.one * .37f));
        }
        finally
        {
            Object.DestroyImmediate(itemObject);
            Object.DestroyImmediate(pool);
            Object.DestroyImmediate(definition);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Invoke(PooledItem item, string method, params object[] args) =>
        typeof(PooledItem).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(item, args);
}
