using UnityEngine;

/// <summary>
/// Caches a reference to the Player GameObject so other scripts don't need
/// to call GameObject.FindWithTag("Player") repeatedly.
/// GameObject.FindWithTag is expensive because it searches the entire scene
/// hierarchy every time it's called. Caching the result once eliminates this cost.
/// </summary>
public static class PlayerCache {

    private static Transform cachedTransform;
    private static GameObject cachedGameObject;
    private static bool hasSearched = false;

    /// <summary>
    /// Gets the Player's Transform. Only searches the scene once.
    /// Subsequent calls return the cached reference immediately (zero allocation).
    /// Recovers automatically if the cached reference was destroyed.
    /// </summary>
    public static Transform Transform {
        get {
            if (!hasSearched || cachedTransform == null) {
                FindPlayer();
                hasSearched = true;
            }
            return cachedTransform;
        }
    }

    /// <summary>
    /// Gets the Player's GameObject. Only searches the scene once.
    /// </summary>
    public static GameObject GameObject {
        get {
            if (!hasSearched || cachedGameObject == null) {
                FindPlayer();
                hasSearched = true;
            }
            return cachedGameObject;
        }
    }

    /// <summary>
    /// Forces a re-find of the player on the next access.
    /// Call this when loading a new scene or respawning the player.
    /// </summary>
    public static void Invalidate() {
        cachedTransform = null;
        cachedGameObject = null;
        hasSearched = false;
    }

    private static void FindPlayer() {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) {
            cachedGameObject = playerObj;
            cachedTransform = playerObj.transform;
        }
    }
}
