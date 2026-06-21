using System;
using System.Collections;
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

    #region SERIALIZED FIELDS

    [Header("Base Settings")]
    [Tooltip("If true, this enemy will not be counted by the WaveManager.")]
    [SerializeField] protected bool isTutorialEnemy = false;

    [Header("Wave Scaling Stats")]
    [Tooltip("Base health at wave 1.")]
    [SerializeField] private float initialHealth = 100f;
    [Tooltip("Maximum health when wave scaling reaches its max wave.")]
    [SerializeField] private float maxHealthCap = 500f;
    [Tooltip("Base attack damage at wave 1.")]
    [SerializeField] private float initialAttackDamage = 10f;
    [Tooltip("Maximum attack damage when wave scaling reaches its max wave.")]
    [SerializeField] private float maxAttackDamageCap = 30f;
    [Tooltip("Base currency reward at wave 1.")]
    [SerializeField] private int initialRewardCurrency = 100;
    [Tooltip("Maximum currency reward when wave scaling reaches its max wave.")]
    [SerializeField] private int maxRewardCurrencyCap = 300;

    #endregion

    #region FIELDS

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

    private PooledObject _pooledObject;
    private bool _isFirstEnable = true;

    #endregion

    #region PROPERTIES

    /// <summary>Public read-only access for the Easter egg system to read the reward before transforming enemies.</summary>
    public int RewardCurrency => rewardCurrency;

    /// <summary>Public read-only access so external systems (Easter egg) can distinguish tutorial enemies.</summary>
    public bool IsTutorialEnemy => isTutorialEnemy;

    #endregion

    #region EVENTS

    /// <summary>
    /// Triggered by any Enemy when it dies. The WaveManager listens to this to decrement the count of alive enemies.
    /// </summary>
    public static event Action OnAnyEnemyDied;

    #endregion

    #region CONSTANTS

    private static readonly int HashDeath = Animator.StringToHash("Death");

    #endregion

    #region UNITY

    protected virtual void Awake() {
        enemyFollow = GetComponent<EnemyFollow>();
        enemyAttack = GetComponent<EnemyAttack>();
        animator = GetComponent<Animator>();
        audioManagerService = FindFirstObjectByType<AudioManagerService>();

        _pooledObject = GetComponent<PooledObject>();

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        InitializeStats();

        if (!isTutorialEnemy)
            ApplyWaveScaling();

        currentHealth = maxHealth;

        if (enemyFollow != null) {
            enemyFollow.SetSpeed(moveSpeed);
            enemyFollow.SetEnemyBase(this);
        }

        if (enemyAttack != null) {
            enemyAttack.Configure(attackDamage, attackRange, attackCooldown);
            enemyAttack.SetEnemyBase(this);
        }
    }

    /// <summary>
    /// Called automatically when the GameObject is activated (first time or pool reuse).
    /// On first execution, Awake already handled initialization.
    /// On pool reactivations, resets state so the enemy spawns as new.
    /// </summary>
    protected virtual void OnEnable() {
        if (!_isFirstEnable) {
            ResetForPoolReuse();
        }
        _isFirstEnable = false;
    }

    /// <summary>
    /// Resets the enemy state when reused from the pool.
    /// Reapplies stats, health, colliders, and components disabled on previous death.
    /// </summary>
    private void ResetForPoolReuse() {
        isDead = false;

        InitializeStats();
        if (!isTutorialEnemy)
            ApplyWaveScaling();
        currentHealth = maxHealth;

        if (enemyFollow != null) {
            enemyFollow.SetSpeed(moveSpeed);
            enemyFollow.SetMovementEnabled(true);
        }

        if (enemyAttack != null) {
            enemyAttack.Configure(attackDamage, attackRange, attackCooldown);
            enemyAttack.enabled = true;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) {
            col.enabled = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = true;
        }

        if (animator != null) {
            animator.Rebind();
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Called during Awake(). Each subclass defines its stats here.
    /// </summary>
    protected abstract void InitializeStats();

    /// <summary>
    /// Reduces the target's current health by the informed value.
    /// Triggers death if health reaches 0.
    /// Called by Projectile.cs when a bullet hits an enemy.
    /// </summary>
    public void TakeDamage(float amount) {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Allows subclasses to override the death destroy delay.
    /// Default is 2 seconds. PenguinEnemy overrides to 1 second.
    /// </summary>
    protected virtual float GetDeathDestroyDelay() => 2f;

    /// <summary>
    /// Handles enemy death logic.
    /// Awards currency to the player when enemy is killed.
    /// Plays death animation and removes enemy after GetDeathDestroyDelay() seconds.
    /// Disables all physics interactions (colliders, rigidbody) to prevent body blocking and bullet interception.
    /// </summary>
    protected virtual void Die() {
        if (isDead) return;
        isDead = true;

        if (enemyFollow != null) enemyFollow.SetMovementEnabled(false);

        if (enemyAttack != null) enemyAttack.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) {
            col.enabled = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = true;
        }

        if (animator != null)
            animator.SetTrigger(HashDeath);

        PlayDeathSound();

        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.AddCurrency(rewardCurrency);
        }

        if (!isTutorialEnemy) {
            OnAnyEnemyDied?.Invoke();
        }

        if (_pooledObject != null) {
            StartCoroutine(PooledDeathRoutine());
        } else {
            Destroy(gameObject, GetDeathDestroyDelay());
        }
    }

    /// <summary>
    /// Waits for the death animation then returns the enemy to the pool.
    /// Instead of Destroy, returns to pool for reuse.
    /// The PooledObject OnDisable automatically stops coroutines when the object is deactivated.
    /// </summary>
    private IEnumerator PooledDeathRoutine() {
        yield return new WaitForSeconds(GetDeathDestroyDelay());
        if (_pooledObject != null) {
            _pooledObject.ReturnToPool();
        }
    }

    /// <summary>
    /// Applies wave-based stat scaling using an exponential curve.
    /// Scales health, attack damage, and currency reward based on the current wave.
    /// Only applies to non-tutorial enemies (isTutorialEnemy = false).
    /// </summary>
    private void ApplyWaveScaling() {
        if (WaveManager.Instance == null)
            return;

        int currentWave = WaveManager.Instance.CurrentWave;
        int maxScalingWave = WaveManager.Instance.MaxWaveScalingStats;

        if (maxScalingWave <= 1)
            return;

        float t = (currentWave - 1f) / (maxScalingWave - 1f);
        t = Mathf.Clamp01(t);

        float factor = t * t;

        maxHealth = Mathf.Lerp(initialHealth, maxHealthCap, factor);
        attackDamage = Mathf.Lerp(initialAttackDamage, maxAttackDamageCap, factor);
        rewardCurrency = Mathf.RoundToInt(Mathf.Lerp(initialRewardCurrency, maxRewardCurrencyCap, factor));
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
