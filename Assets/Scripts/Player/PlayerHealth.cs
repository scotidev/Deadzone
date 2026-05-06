using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the player's health. Implements IDamageable so enemies can deal damage via interface.
/// Integrates with PlayerArmor - damage is applied to armor first, then to health.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable {

    #region SERIALIZED FIELDS

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Armor Integration")]
    [SerializeField] private PlayerArmor playerArmor;

    [Header("Poison Damage")]
    [SerializeField] private float poisonDamagePerTick = 5f;
    [SerializeField] private float poisonTickInterval = 1f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;

    #endregion

    #region FIELDS

    private float currentHealth;

    private Coroutine poisonCoroutine;

    #endregion

    #region EVENTS

    public event Action<float> OnHealthChanged;

    public event Action OnPlayerDied;

    public event Action<bool> OnPoisonStateChanged;

    #endregion

    #region UNITY

    private void Awake() {
        currentHealth = maxHealth;

        if (playerArmor == null) {
            playerArmor = GetComponent<PlayerArmor>();
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Applies damage to the player. Called by EnemyAttack when attacking.
    /// Implements the IDamageable interface contract.
    /// First attempts to absorb damage with armor (if available), then applies remaining damage to health.
    /// </summary>
    public void TakeDamage(float amount) {
        if (currentHealth <= 0f) return;

        float damageToHealth = amount;

        if (playerArmor != null && playerArmor.HasArmor()) {
            damageToHealth = playerArmor.AbsorbDamage(amount);

            if (damageToHealth <= 0f) {
                return;
            }
        }

        currentHealth = Mathf.Max(0f, currentHealth - damageToHealth);

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

        Debug.Log("[PlayerHealth] Player died."); // disparar evento de morte, carregando o canvas, sfx, etc.
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

    #endregion
}
