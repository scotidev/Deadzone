using UnityEngine;

/// <summary>
/// Caches a reference to the Player GameObject so other scripts don't need
/// to call GameObject.FindWithTag("Player") repeatedly.
/// GameObject.FindWithTag is expensive because it searches the entire scene
/// hierarchy every time it's called. Caching the result once eliminates this cost.
/// CONCEITO: Manter uma referência estática pro Player evita ter que
/// procurar ele na cena toda vez. O GameObject.FindWithTag("Player") é caro
/// pq a Unity precisa varrer toda a hierarquia da cena pra achar.
/// Com cache, só procuramos UMA VEZ e guardamos o resultado.
/// </summary>
public static class PlayerCache {

    private static Transform cachedTransform;
    private static GameObject cachedGameObject;
    private static bool hasSearched = false;

    /// <summary>
    /// Gets the Player's Transform. Only searches the scene once.
    /// Subsequent calls return the cached reference immediately (zero allocation).
    /// Auto-recupera-se se a referência cacheada foi destruída (ex: sessão anterior
    /// com "Reload Scene Only").
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
    /// CONCEITO: Se o player for recriado (ex: nova cena, respawn),
    /// precisamos invalidar o cache pra que a referência seja atualizada.
    /// </summary>
    public static void Invalidate() {
        cachedTransform = null;
        cachedGameObject = null;
        hasSearched = false;
    }

    private static void FindPlayer() {
        // CONCEITO: FindWithTag procura na cena INTEIRA por um GameObject
        // com a tag "Player". Isso é lento, mas só fazemos UMA VEZ.
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) {
            cachedGameObject = playerObj;
            cachedTransform = playerObj.transform;
        }
    }
}
