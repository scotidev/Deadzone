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

    #region FIELDS

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

    protected float maxHealth = 100f;
    protected float moveSpeed = 3.5f;
    protected float attackDamage = 10f;
    protected float attackRange = 1.8f;
    protected float attackCooldown = 1.5f;
    protected int rewardCurrency = 100;

    /// <summary>Public read-only access for the Easter egg system to read the reward before transforming enemies.</summary>
    public int RewardCurrency => rewardCurrency;

    /// <summary>Public read-only access so external systems (Easter egg) can distinguish tutorial enemies.</summary>
    public bool IsTutorialEnemy => isTutorialEnemy;

    private float currentHealth;
    private bool isDead;

    protected EnemyFollow enemyFollow;
    protected EnemyAttack enemyAttack;
    protected Animator animator;
    private AudioManagerService audioManagerService;

    // Pooling support
    // CONCEITO: Se o inimigo veio do pool, _pooledObject não é null.
    // Usamos pra devolver ao pool em vez de Destroy na morte.
    private PooledObject _pooledObject;
    // CONCEITO: Flag que controla se é a primeira vez que o OnEnable roda.
    // Na primeira vez (Instantiate), o Awake já configurou tudo.
    // Nas vezes seguintes (pool), precisamos resetar o estado.
    private bool _isFirstEnable = true;

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
        audioManagerService = FindFirstObjectByType<AudioManagerService>();

        // Cache do PooledObject — se existir, esse inimigo pode ser reutilizado pelo pool
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
    /// Called automaticamente quando o GameObject é ativado (primeira vez ou pool).
    /// CONCEITO: Na primeira execução, o Awake já fez tudo, então ignoramos.
    /// Nas reativações do pool, resetamos o estado pro inimigo nascer "novo".
    /// </summary>
    protected virtual void OnEnable() {
        if (!_isFirstEnable) {
            // CONCEITO: Reutilização do pool — resetar estado pro inimigo parecer novo
            ResetForPoolReuse();
        }
        _isFirstEnable = false;
    }

    /// <summary>
    /// Reseta o estado do inimigo quando reutilizado do pool.
    /// Reaplica stats, saúde, colliders e componentes desativados na morte anterior.
    /// </summary>
    private void ResetForPoolReuse() {
        isDead = false;

        // Reaplica stats e scaling (a onda pode ser diferente de quando foi criado)
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

        // Reativa colliders (desativados no Die())
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) {
            col.enabled = true;
        }

        // Reseta rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = true;
        }

        // CONCEITO: Animator.Rebind reseta todos os parâmetros do animator
        // pro estado padrão, como se o objeto tivesse acabado de ser instanciado.
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

        // Disable all colliders to prevent body blocking and bullet interception
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) {
            col.enabled = false;
        }

        // Make rigidbody kinematic to prevent physics interactions
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = true;
        }

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

        // CONCEITO: Se tem PooledObject, devolve ao pool após a animação de morte.
        // Senão, usa Destroy normal (fallback pra inimigos não-pooled como tutoriais).
        if (_pooledObject != null) {
            StartCoroutine(PooledDeathRoutine());
        } else {
            Destroy(gameObject, GetDeathDestroyDelay());
        }
    }

    /// <summary>
    /// Aguarda a animação de morte e devolve o inimigo ao pool.
    /// CONCEITO: Em vez de Destroy, que libera memória e causa GC,
    /// devolvemos ao pool pra reutilização. O OnDisable do PooledObject
    /// vai parar automaticamente as corrotinas quando o objeto for desativado.
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

        // Exponential curve: starts slow, accelerates in later waves
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
