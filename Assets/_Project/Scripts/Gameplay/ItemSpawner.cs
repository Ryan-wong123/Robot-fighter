using System;
using UnityEngine;
using WobbleBrawl.Core;

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
        [SerializeField] private bool spawnAtRoundStart;
        [SerializeField] private bool spawnOpeningLineup;
        [SerializeField, Min(1)] private int intervalFrames = 300;
        [SerializeField] private int seed = 1;
        [SerializeField] private WobbleBrawl.Data.TrainingSandboxLayout trainingLayout;
        private bool persistentRack;
        private int[] dispenseCooldown;
        private int[] dispenseCursor;
        public bool HasPersistentRack => persistentRack;

        private PooledItem[][] pools;
        private int[] weights;
        private System.Random random;
        private int remainingFrames;

        internal bool TryGetPickup(Vector3 from, float range, out Vector3 position)
        {
            position = default;
            if (pools == null)
                return false;
            bool found = false;
            float nearest = range * range;
            foreach (var pool in pools)
            {
                foreach (var item in pool)
                {
                    if (!item.IsPickup)
                        continue;
                    Vector3 point = item.transform.position;
                    float distance = (point - from).sqrMagnitude;
                    if (distance >= nearest)
                        continue;
                    nearest = distance;
                    position = point;
                    found = true;
                }
            }
            return found;
        }

        private void Awake()
        {
            var catalog = Resources.Load<LaunchItemCatalog>("Items/LaunchItems");
            if (catalog != null && catalog.Items != null)
            {
                var entries = new System.Collections.Generic.List<Entry>(table ?? Array.Empty<Entry>());
                foreach (var definition in catalog.Items)
                {
                    bool exists = false;
                    foreach (var entry in entries)
                        exists |= entry.Item != null && entry.Item.Id == definition.Id;
                    if (!exists)
                        entries.Add(new Entry { Item = definition, Weight = 1, Capacity = 1 });
                }
                table = entries.ToArray();
            }
            int count = table?.Length ?? 0;
            persistentRack = trainingLayout != null && TrainingSandboxRules.AllowsShortcuts(gameObject.scene.name);
            dispenseCooldown = new int[count];
            dispenseCursor = new int[count];
            pools = new PooledItem[count][];
            weights = new int[count];
            for (int index = 0; index < count; index++)
            {
                Entry entry = table[index];
                int capacity = entry.Item != null && entry.Item.Prefab != null && entry.Weight > 0
                    ? Mathf.Max(0, entry.Capacity) : 0;
                if (persistentRack && capacity > 0) capacity = trainingLayout.CopiesPerItem + 1;
                pools[index] = new PooledItem[capacity];
                for (int slot = 0; slot < capacity; slot++)
                {
                    var item = Instantiate(entry.Item.Prefab, transform);
                    item.Initialize(entry.Item, transform);
                    if (persistentRack && slot == 0) item.ConfigureDispenser(this, index);
                    pools[index][slot] = item;
                }
            }
        }

        public void BeginRound(int roundIndex) => BeginRound(roundIndex, MatchRules.Default);

        public void BeginRound(int roundIndex, MatchRules rules)
        {
            EndRound();
            if (!rules.ItemsEnabled || !isActiveAndEnabled)
                return;
            random = new System.Random(unchecked(seed + roundIndex));
            Array.Clear(dispenseCooldown, 0, dispenseCooldown.Length);
            Array.Clear(dispenseCursor, 0, dispenseCursor.Length);
            remainingFrames = Mathf.Max(1, intervalFrames);
            if ((persistentRack || spawnOpeningLineup) && spawnPoints != null)
            {
                for (int index = 0; index < Mathf.Min(pools.Length, spawnPoints.Length); index++)
                    if (pools[index].Length > 0 && spawnPoints[index] != null)
                        pools[index][0].Spawn(spawnPoints[index].position);
            }
            else if (spawnAtRoundStart)
                SpawnNext();
        }

        public void EndRound()
        {
            random = null;
            if (pools == null)
                return;
            foreach (var pool in pools)
                foreach (var item in pool)
                {
                    if (persistentRack && isActiveAndEnabled && item == pool[0]) continue;
                    if (!item.IsAvailable)
                        item.Recycle();
                }
        }

        private void OnDisable() => EndRound();

        private void FixedUpdate()
        {
            if (persistentRack)
            {
                for (int index = 0; index < dispenseCooldown.Length; index++)
                    if (dispenseCooldown[index] > 0) dispenseCooldown[index]--;
                return;
            }
            if (random == null || --remainingFrames > 0)
                return;
            remainingFrames = Mathf.Max(1, intervalFrames);
            SpawnNext();
        }

        private void SpawnNext()
        {
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

        internal void TryDispense(int index, Fighter fighter)
        {
            if (random == null || !fighter.CanInteractWithItems || fighter.Items == null || fighter.Items.HasItem ||
                dispenseCooldown[index] > 0) return;
            var pool = pools[index];
            PooledItem copy = null;
            for (int slot = 1; slot < pool.Length; slot++)
                if (pool[slot].IsAvailable) { copy = pool[slot]; break; }
            if (copy == null)
            {
                for (int offset = 0; offset < pool.Length - 1; offset++)
                {
                    int slot = 1 + (dispenseCursor[index] + offset) % (pool.Length - 1);
                    if (!pool[slot].IsDeployed) continue;
                    copy = pool[slot];
                    copy.Recycle();
                    dispenseCursor[index] = slot % (pool.Length - 1);
                    break;
                }
            }
            if (copy == null) return;
            copy.Spawn(spawnPoints[index].position);
            if (fighter.Items.TryPickup(copy)) dispenseCooldown[index] = trainingLayout.DispenseCooldownFrames;
            else copy.Recycle();
        }
    }
}
