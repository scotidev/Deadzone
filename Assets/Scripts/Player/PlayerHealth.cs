using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the player's health. Implements IDamageable so enemies can deal damage via interface.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable {

    [Header("Health Settings")]
    [Tooltip("Maximum health points.")]
    [SerializeField] private float maxHealth = 100f;

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

    private void Awake() {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage to the player. Called by EnemyAttack when attacking.
    /// Implements the IDamageable interface contract.
    /// </summary>
    public void TakeDamage(float amount) {
        if (currentHealth <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0f)
            Die();
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
