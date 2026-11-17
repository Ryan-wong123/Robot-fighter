using System;
using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    public sealed class ItemSpawner : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public ItemDef Item;
            [Min(1)] public int Weight;
            [Min(1)] public int Capacity;
        }

        [SerializeField] private Entry[] table;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField, Min(1)] private int intervalFrames = 300;
        [SerializeField] private int seed = 1;

        private PooledItem[][] pools;
        private int[] weights;
        private System.Random random;
        private int remainingFrames;

        private void Awake()
        {
            int count = table?.Length ?? 0;
            pools = new PooledItem[count][];
            weights = new int[count];
            for (int index = 0; index < count; index++)
            {
                Entry entry = table[index];
                int capacity = entry.Item != null && entry.Item.Prefab != null && entry.Weight > 0
                    ? Mathf.Max(0, entry.Capacity) : 0;
                pools[index] = new PooledItem[capacity];
                for (int slot = 0; slot < capacity; slot++)
                {
                    var item = Instantiate(entry.Item.Prefab, transform);
                    item.Initialize(entry.Item, transform);
                    pools[index][slot] = item;
                }
            }
        }

        public void BeginRound(int roundIndex)
        {
            EndRound();
            random = new System.Random(unchecked(seed + roundIndex));
            remainingFrames = Mathf.Max(1, intervalFrames);
        }

        public void EndRound()
        {
            random = null;
            if (pools == null)
                return;
            foreach (var pool in pools)
                foreach (var item in pool)
                    if (!item.IsAvailable)
                        item.Recycle();
        }

        private void OnDisable() => EndRound();

        private void FixedUpdate()
        {
            if (random == null || --remainingFrames > 0)
                return;
            remainingFrames = Mathf.Max(1, intervalFrames);
            if (spawnPoints == null || spawnPoints.Length == 0)
                return;
            for (int index = 0; index < pools.Length; index++)
            {
                weights[index] = 0;
                foreach (var item in pools[index])
                    if (item.IsAvailable)
                    {
                        weights[index] = table[index].Weight;
                        break;
                    }
            }
            int selected = ItemWeights.Select(weights, random);
            if (selected < 0)
                return;
            Transform point = spawnPoints[random.Next(spawnPoints.Length)];
            if (point == null)
                return;
            foreach (var item in pools[selected])
                if (item.IsAvailable)
                {
                    item.Spawn(point.position);
                    return;
                }
        }
    }
}
