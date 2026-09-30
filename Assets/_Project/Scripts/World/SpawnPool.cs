using System;
using System.Collections.Generic;
using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>Anything placed on the track. Pooled by key and recycled once behind the player.</summary>
    public abstract class Spawnable : MonoBehaviour
    {
        public string PoolKey { get; set; }

        /// <summary>Called every time the object is taken from the pool.</summary>
        public virtual void OnSpawned() { }

        /// <summary>Called by the track each frame while the run is live.</summary>
        public virtual void Tick(float dt, PlayerController player) { }

        public bool Released { get; private set; }

        public void MarkReleased(bool released) => Released = released;
    }

    public class SpawnPool
    {
        readonly Dictionary<string, Func<Spawnable>> _factories = new Dictionary<string, Func<Spawnable>>();
        readonly Dictionary<string, Stack<Spawnable>> _free = new Dictionary<string, Stack<Spawnable>>();
        readonly Transform _storage;

        public SpawnPool(Transform storage)
        {
            _storage = storage;
        }

        public void Register(string key, Func<Spawnable> factory)
        {
            _factories[key] = factory;
            _free[key] = new Stack<Spawnable>();
        }

        public Spawnable Get(string key, Transform parent, Vector3 localPos)
        {
            var stack = _free[key];
            var item = stack.Count > 0 ? stack.Pop() : Create(key);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPos;
            item.transform.localRotation = Quaternion.identity;
            item.MarkReleased(false);
            item.gameObject.SetActive(true);
            item.OnSpawned();
            return item;
        }

        public void Release(Spawnable item)
        {
            if (item == null || item.Released) return;
            item.MarkReleased(true);
            item.gameObject.SetActive(false);
            item.transform.SetParent(_storage, false);
            _free[item.PoolKey].Push(item);
        }

        Spawnable Create(string key)
        {
            var item = _factories[key]();
            item.PoolKey = key;
            return item;
        }
    }
}
