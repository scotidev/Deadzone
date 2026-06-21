using UnityEngine;

/// <summary>
/// Attached to pooled GameObjects to allow them to be returned to the pool
/// instead of destroyed. The OnDisable() method is called automatically by Unity
/// whenever the GameObject is deactivated (whether by ReturnToPool() or by external code).
/// </summary>
public class PooledObject : MonoBehaviour {

    /// <summary>
    /// Returns this GameObject to the pool. The object is deactivated and
    /// queued for later reuse. After calling this, the object is effectively
    /// "asleep" until retrieved again via GameObjectPool.Get().
    /// </summary>
    public void ReturnToPool() {
        GameObjectPool.Return(gameObject);
    }

    /// <summary>
    /// Called automatically by Unity when the GameObject is deactivated.
    /// We stop all coroutines here to prevent them from running while the
    /// object is inactive (which would waste CPU and cause errors).
    /// </summary>
    private void OnDisable() {
        StopAllCoroutines();
    }
}
