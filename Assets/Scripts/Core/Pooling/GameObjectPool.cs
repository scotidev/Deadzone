using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages GameObject reuse to avoid costly Instantiate/Destroy calls.
/// Reduces garbage collection (GC) pressure caused by frequent allocations,
/// which is the #1 cause of stutter in Unity games.
/// CONCEITO: Pooling reutiliza objetos em vez de criar/destruir o tempo todo.
/// Isso evita que o Garbage Collector (GC) precise limpar memória constantemente,
/// que é o que causa as pausas (stutters) no jogo.
/// </summary>
public static class GameObjectPool {

    // Dictionary keyed by prefab InstanceID, value is a queue of inactive objects
    // CONCEITO: Dictionary é uma estrutura de dados que mapeia uma chave única
    // (o ID do prefab) para uma fila de GameObjects prontos pra reuso.
    private static Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();

    // Tracks which prefab each active object came from
    // CONCEITO: Precisamos saber a qual prefab cada objeto ativo pertence
    // pra saber em qual fila colocá-lo quando for devolvido.
    private static Dictionary<GameObject, int> activeToPrefabId = new Dictionary<GameObject, int>();

    /// <summary>
    /// Retrieves an object from the pool matching the given prefab.
    /// If the pool is empty, a new object is instantiated and the PooledObject
    /// component is added automatically so it can be returned later.
    /// Auto-recupera-se se o objeto desenfileirado foi destruído (ex: sessão anterior).
    /// CONCEITO: Se o pool estiver vazio, precisamos criar um novo objeto
    /// (Instantiate). Isso é mais barato que sempre criar porque,
    /// depois de criado, ele será reutilizado várias vezes.
    /// </summary>
    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation) {
        int id = prefab.GetInstanceID();

        // Lazy initialization: cria o dicionário se não existir
        if (!pools.ContainsKey(id))
            pools[id] = new Queue<GameObject>();

        Queue<GameObject> pool = pools[id];
        GameObject obj = null;

        // CONCEITO: Desenfileira objetos um por um até encontrar um válido.
        // Se a sessão anterior deixou objetos destruídos no pool (Reload Scene Only),
        // pulamos eles e usamos um objeto íntegro.
        while (pool.Count > 0) {
            obj = pool.Dequeue();
            if (obj != null) break;
        }

        if (obj != null) {
            // CONCEITO: Tem um objeto válido! Só reposicionamos e reativamos.
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
        } else {
            // CONCEITO: Fila vazia ou só tinha objetos destruídos, criar um novo.
            obj = Object.Instantiate(prefab, position, rotation);
            // Adiciona o componente que permite devolver ao pool depois
            obj.AddComponent<PooledObject>();
        }

        // Registra que este objeto veio deste prefab
        activeToPrefabId[obj] = id;
        return obj;
    }

    /// <summary>
    /// Returns an object to the pool. The object is deactivated and
    /// queued for reuse. If the object wasn't obtained from the pool,
    /// it falls back to Destroy.
    /// CONCEITO: Em vez de destruir, desativamos e guardamos na fila.
    /// Desativar é MUITO mais barato que destruir porque a memória
    /// não precisa ser liberada e realocada depois.
    /// </summary>
    public static void Return(GameObject obj) {
        if (obj == null) return;

        if (activeToPrefabId.TryGetValue(obj, out int prefabId)) {
            // Desativa o objeto e coloca de volta na fila do pool
            obj.SetActive(false);
            pools[prefabId].Enqueue(obj);
            activeToPrefabId.Remove(obj);
        } else {
            // Objeto não veio do pool — destrói normalmente
            Object.Destroy(obj);
        }
    }

    /// <summary>
    /// Pre-warms the pool by creating inactive instances in advance.
    /// Call this in Start() for frequently spawned objects (projectiles, casings, VFX).
    /// CONCEITO: "Aquecer" o pool significa criar objetos ANTES de precisar deles.
    /// Isso evita o custo do Instantiate durante o jogo.
    /// Por exemplo, criar 20 projéteis na inicialização da fase.
    /// </summary>
    public static void Prewarm(GameObject prefab, int count) {
        int id = prefab.GetInstanceID();
        if (!pools.ContainsKey(id))
            pools[id] = new Queue<GameObject>();

        Queue<GameObject> pool = pools[id];
        for (int i = 0; i < count; i++) {
            GameObject obj = Object.Instantiate(prefab);
            obj.SetActive(false);
            // CONCEITO: Esconde o objeto na hierarquia pra não poluir a cena
            obj.hideFlags = HideFlags.HideInHierarchy;
            obj.AddComponent<PooledObject>();
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// Clears all pools and destroys their inactive objects.
    /// Call this when loading a new scene to free memory.
    /// CONCEITO: Limpar o pool libera toda a memória dos objetos em espera.
    /// Necessário ao carregar uma nova cena pra não acumular objetos de cenas anteriores.
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
