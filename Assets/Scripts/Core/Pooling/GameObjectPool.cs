using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages GameObject reuse to avoid costly Instantiate/Destroy calls.
/// Reduces garbage collection (GC) pressure caused by frequent allocations,
/// which is the #1 cause of stutter in Unity games.
/// </summary>
public static class GameObjectPool {

    private static Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();

    private static Dictionary<GameObject, int> activeToPrefabId = new Dictionary<GameObject, int>();

    /// <summary>
    /// Retrieves an object from the pool matching the given prefab.
    /// If the pool is empty, a new object is instantiated and the PooledObject
    /// component is added automatically so it can be returned later.
    /// Recovers automatically if the dequeued object was destroyed (e.g. previous session).
    /// </summary>
    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation) {
        int id = prefab.GetInstanceID();

        if (!pools.ContainsKey(id))
            pools[id] = new Queue<GameObject>();

        Queue<GameObject> pool = pools[id];
        GameObject obj = null;

        while (pool.Count > 0) {
            obj = pool.Dequeue();
            if (obj != null) break;
        }

        if (obj != null) {
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
        } else {
            obj = Object.Instantiate(prefab, position, rotation);
            obj.AddComponent<PooledObject>();
        }

        activeToPrefabId[obj] = id;
        return obj;
    }

    /// <summary>
    /// Returns an object to the pool. The object is deactivated and
    /// queued for reuse. If the object wasn't obtained from the pool,
    /// it falls back to Destroy.
    /// </summary>
    public static void Return(GameObject obj) {
        if (obj == null) return;

        if (activeToPrefabId.TryGetValue(obj, out int prefabId)) {
            obj.SetActive(false);
            pools[prefabId].Enqueue(obj);
            activeToPrefabId.Remove(obj);
        } else {
            Object.Destroy(obj);
        }
    }

    /// <summary>
    /// Pre-warms the pool by creating inactive instances in advance.
    /// Call this in Start() for frequently spawned objects (projectiles, casings, VFX).
    /// </summary>
    public static void Prewarm(GameObject prefab, int count) {
        int id = prefab.GetInstanceID();
        if (!pools.ContainsKey(id))
            pools[id] = new Queue<GameObject>();

        Queue<GameObject> pool = pools[id];
        for (int i = 0; i < count; i++) {
            GameObject obj = Object.Instantiate(prefab);
            obj.SetActive(false);
            obj.hideFlags = HideFlags.HideInHierarchy;
            obj.AddComponent<PooledObject>();
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// Clears all pools and destroys their inactive objects.
    /// Call this when loading a new scene to free memory.
    /// </summary>
    public static void Clear() {
        foreach (var kvp in pools) {
            foreach (GameObject obj in kvp.Value) {
                if (obj != null)
                    Object.Destroy(obj);
            }
        }
        pools.Clear();
        activeToPrefabId.Clear();
    }
}
