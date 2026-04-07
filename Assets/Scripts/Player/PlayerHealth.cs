using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the player's health. Implements IDamageable so enemies can deal damage via interface.
/// Integrates with PlayerArmor - damage is applied to armor first, then to health.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable {

    [Header("Health Settings")]
    [Tooltip("Maximum health points.")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Armor Integration")]
    [Tooltip("Reference to the PlayerArmor component. If assigned, armor absorbs damage before health.")]
    [SerializeField] private PlayerArmor playerArmor;

    [Header("Poison Damage (Outside SafeZone)")]
    [Tooltip("Damage applied per tick while the player is in the poison area.")]
    [SerializeField] private float poisonDamagePerTick = 5f;

    [Tooltip("Interval in seconds between each poison damage tick.")]
    [SerializeField] private float poisonTickInterval = 1f;

    private float currentHealth;

    private Coroutine poisonCoroutine;

    public event Action<float> OnHealthChanged;

    public event Action OnPlayerDied;

    public event Action<bool> OnPoisonStateChanged;

    // Awake() é chamado quando o GameObject é criado, antes de tudo
    private void Awake() {
        // Inicializa a vida atual com o valor máximo (player começa com vida cheia)
        currentHealth = maxHealth;

        // Tenta encontrar o componente PlayerArmor automaticamente se não foi atribuído no Inspector
        // == null verifica se a variável está vazia/não atribuída
        if (playerArmor == null) {
            // GetComponent<>() busca um componente do tipo especificado no mesmo GameObject
            // É uma busca local - só olha neste objeto, não nos filhos ou pai
            playerArmor = GetComponent<PlayerArmor>();
        }
    }

    /// <summary>
    /// Applies damage to the player. Called by EnemyAttack when attacking.
    /// Implements the IDamageable interface contract.
    /// First attempts to absorb damage with armor (if available), then applies remaining damage to health.
    /// </summary>
    public void TakeDamage(float amount) {
        // Se o player já está morto (vida <= 0), não processa mais dano
        // return sai da função imediatamente
        if (currentHealth <= 0f) return;

        // Variável para armazenar quanto dano vai ser aplicado à vida
        // Inicialmente assume que todo o dano vai para a vida
        float damageToHealth = amount;

        // && é o operador AND (E lógico) - ambas condições devem ser verdadeiras
        // Verifica: 1) playerArmor existe (não é null)  E  2) o player tem armor (HasArmor retorna true)
        if (playerArmor != null && playerArmor.HasArmor()) {
            // O armor tenta absorver o dano e retorna o que sobrou (overflow)
            // Ex: se armor = 30 e dano = 50, retorna 20
            // Ex: se armor = 100 e dano = 30, retorna 0 (absorveu tudo)
            damageToHealth = playerArmor.AbsorbDamage(amount);
            
            // Se o dano restante for <= 0, significa que o armor absorveu tudo
            // Não precisa aplicar dano à vida, então retornamos aqui
            if (damageToHealth <= 0f) {
                // $ antes das aspas permite usar {variáveis} dentro do texto
                Debug.Log($"[PlayerHealth] Damage fully absorbed by armor. No health lost.");
                return; // Sai da função, vida não perde nada
            }
            
            // Se chegou aqui, significa que havia dano residual
            // amount - damageToHealth = quanto o armor absorveu
            Debug.Log($"[PlayerHealth] Armor absorbed {amount - damageToHealth} damage. {damageToHealth} damage applied to health.");
        }

        // Aplica o dano restante (que o armor não absorveu) à vida
        // Mathf.Max retorna o MAIOR valor entre dois números
        // Garante que currentHealth nunca seja negativo (mínimo é 0)
        currentHealth = Mathf.Max(0f, currentHealth - damageToHealth);

        // ?. é o operador null-conditional - só executa se OnHealthChanged não for null
        // Invoke() dispara o evento, notificando a UI e outros sistemas inscritos
        // Passamos a fração da vida (0.0 a 1.0) para a UI saber quanto mostrar
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        // Se a vida chegou a 0 ou menos, o player morreu
        if (currentHealth <= 0f)
            Die(); // Chama o método que lida com a morte do player
    }

    /// <summary>
    /// Starts the poison damage tick. Called by SafeZone.OnTriggerExit.
    /// Does nothing if the player is already being poisoned.
    /// </summary>
    public void StartPoisonDamage() {
        if (poisonCoroutine != null) return;
        poisonCoroutine = StartCoroutine(PoisonTick());
        OnPoisonStateChanged?.Invoke(true);
        Debug.Log("[PlayerHealth] Poison activated — taking damage per second.");
    }

    /// <summary>
    /// Stops the poison damage tick. Called by SafeZone.OnTriggerEnter.
    /// Does nothing if the player is not being poisoned.
    /// </summary>
    public void StopPoisonDamage() {
        if (poisonCoroutine == null) return;
        StopCoroutine(poisonCoroutine);
        poisonCoroutine = null;
        OnPoisonStateChanged?.Invoke(false);
        Debug.Log("[PlayerHealth] Poison deactivated — inside the safe zone.");
    }

    /// <summary>
    /// True while the player is taking poison damage.
    /// </summary>
    public bool IsInPoison => poisonCoroutine != null;

    /// <summary>
    /// Coroutine that applies damage every poisonTickInterval seconds.
    /// Respects Time.timeScale — automatically pauses during Pause Menu.
    /// </summary>
    private IEnumerator PoisonTick() {
        while (true) {
            yield return new WaitForSeconds(poisonTickInterval);
            TakeDamage(poisonDamagePerTick);
        }
    }

    /// <summary>
    /// Restores health by the specified amount, without exceeding maxHealth.
    /// Can be called by healing items, medkits, etc.
    /// </summary>
    public void Heal(float amount) {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void Die() {
        OnPlayerDied?.Invoke();

        Debug.Log("[PlayerHealth] Player died.");
    }

    /// <summary>
    /// Returns the current health as a fraction between 0 and 1.
    /// </summary>
    public float GetHealthFraction() => currentHealth / maxHealth;

    /// <summary>
    /// Returns the current health value.
    /// </summary>
    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Returns the maximum health value.
    /// </summary>
    public float GetMaxHealth() => maxHealth;
}
