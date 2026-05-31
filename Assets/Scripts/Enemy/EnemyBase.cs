using System;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

/// <summary>
/// Abstract base class for all enemy types in the game.
/// Static event OnAnyEnemyDied allows the WaveManager to count deaths without
/// needing a direct reference to each individual enemy.
/// </summary>
[RequireComponent(typeof(EnemyFollow))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(Animator))]
public abstract class EnemyBase : MonoBehaviour {

    #region FIELDS

    [Header("Base Settings")]
    [Tooltip("If true, this enemy will not be counted by the WaveManager.")]
    [SerializeField] protected bool isTutorialEnemy = false;

    protected float maxHealth = 100f;
    protected float moveSpeed = 3.5f;
    protected float attackDamage = 10f;
    protected float attackRange = 1.8f;
    protected float attackCooldown = 1.5f;
    protected int rewardCurrency = 100;

    private float currentHealth;
    private bool isDead;

    protected EnemyFollow enemyFollow;
    protected EnemyAttack enemyAttack;
    protected Animator animator;
    private AudioManagerService audioManagerService;

    private static readonly int HashDeath = Animator.StringToHash("Death");

    /// <summary>
    /// Triggered by any Enemy when it dies. The WaveManager listens to this to decrement the count of alive enemies.
    /// </summary>
    public static event Action OnAnyEnemyDied;

    #endregion

    #region UNITY

    protected virtual void Awake() {
        enemyFollow = GetComponent<EnemyFollow>();
        enemyAttack = GetComponent<EnemyAttack>();
        animator = GetComponent<Animator>();
        audioManagerService = FindObjectOfType<AudioManagerService>();

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        InitializeStats();

        currentHealth = maxHealth;

        if (enemyFollow != null) {
            enemyFollow.SetSpeed(moveSpeed);
            enemyFollow.SetEnemyBase(this);
        }

        if (enemyAttack != null)
            enemyAttack.Configure(attackDamage, attackRange, attackCooldown);
            enemyAttack.SetEnemyBase(this);
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
    /// Plays death animation and removes enemy after 2 seconds.
    /// </summary>
    protected virtual void Die() {
        if (isDead) return;
        isDead = true;

        if (enemyFollow != null) enemyFollow.SetMovementEnabled(false);

        if (enemyAttack != null) enemyAttack.enabled = false;

        // Trigger death animation
        if (animator != null)
            animator.SetTrigger(HashDeath);

        // Play death sound (overridden by each zombie type)
        PlayDeathSound();

        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.AddCurrency(rewardCurrency);
        }

        // If this is a tutorial enemy, we don't notify the WaveManager to avoid "Wave Clear" messages
        if (!isTutorialEnemy) {
            OnAnyEnemyDied?.Invoke();
        }

        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// Returns the current health fraction between 0 (dead) and 1 (full).
    /// </summary>
    public float GetHealthFraction() => currentHealth / maxHealth;

    /// <summary>
    /// Plays the idle/grunt sound for this zombie type.
    /// Each zombie type can override this to play their own sound.
    /// Called periodically by EnemyFollow.
    /// </summary>
    public virtual void PlayIdleSound() { }

    /// <summary>
    /// Plays the attack sound for this zombie type.
    /// Each zombie type can override this to play their own sound.
    /// Called by EnemyAttack when attacking.
    /// </summary>
    public virtual void PlayAttackSound() { }

    /// <summary>
    /// Plays the death sound for this zombie type.
    /// Each zombie type can override this to play their own sound.
    /// Called by Die() when the zombie is killed.
    /// </summary>
    public virtual void PlayDeathSound() { }

    /// <summary>
    /// Protected helper method for zombie types to play 3D audio.
    /// Uses AudioManagerService.PlaySFX3DAttached for spatial audio.
    /// </summary>
    protected void Play3DSound(AudioClip clip, float volumeScale = 1f, float minDistance = 5f, float maxDistance = 50f) {
        if (clip == null || audioManagerService == null)
            return;

        audioManagerService.PlaySFX3DAttached(clip, transform, volumeScale, minDistance, maxDistance);
    }

    #endregion

}
