using UnityEngine;

/// <summary>
/// Attached to pooled GameObjects to allow them to be returned to the pool
/// instead of destroyed. The OnDisable() method is called automatically by Unity
/// whenever the GameObject is deactivated (whether by ReturnToPool() or by external code).
/// CONCEITO: Este componente é o que permite um objeto ser "devolvido" ao pool.
/// Quando o objeto é desativado (SetActive(false)), a Unity chama OnDisable()
/// automaticamente. Aproveitamos isso pra parar corrotinas e evitar leaks.
/// </summary>
public class PooledObject : MonoBehaviour {

    /// <summary>
    /// Returns this GameObject to the pool. The object is deactivated and
    /// queued for later reuse. After calling this, the object is effectively
    /// "asleep" until retrieved again via GameObjectPool.Get().
    /// CONCEITO: Em vez de Destroy(gameObject), chamamos este método.
    /// O objeto é desativado mas continua existindo na memória,
    /// pronto pra ser reutilizado sem custo de alocação.
    /// </summary>
    public void ReturnToPool() {
        // CONCEITO: GameObjectPool.Return desativa o objeto e o coloca na fila.
        // O OnDisable() abaixo será chamado automaticamente pela Unity.
        GameObjectPool.Return(gameObject);
    }

    /// <summary>
    /// Called automatically by Unity when the GameObject is deactivated.
    /// We stop all coroutines here to prevent them from running while the
    /// object is inactive (which would waste CPU and cause errors).
    /// CONCEITO: Unity chama OnDisable automaticamente quando SetActive(false).
    /// Parar corrotinas é importante porque elas continuam rodando
    /// mesmo com o objeto desativado, causando warnings e gastando CPU.
    /// </summary>
    private void OnDisable() {
        // CONCEITO: StopAllCoroutines garante que nenhuma coroutine
        // continue executando enquanto o objeto está no pool (desativado).
        StopAllCoroutines();
    }
}
