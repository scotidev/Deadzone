using System;
using UnityEngine;

/// <summary>
/// Abstract base class for all enemy types in the game.
/// Static event OnAnyEnemyDied allows the WaveManager to count deaths without
/// needing a direct reference to each individual enemy.
/// </summary>
[RequireComponent(typeof(EnemyFollow))]
[RequireComponent(typeof(EnemyAttack))]
public abstract class EnemyBase : MonoBehaviour {

    #region FIELDS

    protected float maxHealth = 100f;
    protected float moveSpeed = 3.5f;
    protected float attackDamage = 10f;
    protected float attackRange = 1.8f;
    protected float attackCooldown = 1.5f;

    private float currentHealth;
    private bool isDead;

    protected EnemyFollow enemyFollow;
    protected EnemyAttack enemyAttack;

    /// <summary>
    /// Triggered by any Enemy when it dies. The WaveManager listens to this to decrement the count of alive enemies.
    /// </summary>
    public static event Action OnAnyEnemyDied;

    #endregion

    #region UNITY

    protected virtual void Awake() {
        enemyFollow = GetComponent<EnemyFollow>();
        enemyAttack = GetComponent<EnemyAttack>();

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        InitializeStats();

        currentHealth = maxHealth;

        if (enemyFollow != null)
            enemyFollow.SetSpeed(moveSpeed);

        if (enemyAttack != null)
            enemyAttack.Configure(attackDamage, attackRange, attackCooldown);
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Called during Awake(). Each subclass defines its stats here.
    /// </summary>
    protected abstract void InitializeStats();

    /// <summary>
    /// Reduces the target's current health by the informed value.
    /// Triggers death if health = 0.
    /// Called by Projectile.cs when a case hits and enemy.
    /// </summary>
    public void TakeDamage(float amount) {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Handles enemy death logic.
    /// Awards currency to the player when enemy is killed.
    /// </summary>
    protected virtual void Die() {
        if (isDead) return;
        isDead = true;

        if (enemyFollow != null) enemyFollow.SetMovementEnabled(false);

        if (enemyAttack != null) enemyAttack.enabled = false;

        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.AddCurrency(100);
        }

        OnAnyEnemyDied?.Invoke();

        Destroy(gameObject, 1f);
    }

    /// <summary>
    /// Returns the current health fraction between 0 (dead) and 1 (full).
    /// </summary>
    public float GetHealthFraction() => currentHealth / maxHealth;

    #endregion

}
