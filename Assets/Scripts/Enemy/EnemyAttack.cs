using UnityEngine;

/// <summary>
/// Controls the melee attack behavior for all enemy types.
///
/// When the player enters the attack range (attackRange), the enemy
/// stops moving and applies damage periodically via IDamageable.
/// When the player moves away, movement is automatically resumed.
/// </summary>
public class EnemyAttack : MonoBehaviour {

    private float attackDamage;
    private float attackRange;
    private float attackCooldown;

    private float lastAttackTime;

    private EnemyFollow enemyFollow;
    private Transform playerTransform;
    private IDamageable playerDamageable;

    private Animator animator;

    private static readonly int HashAttack = Animator.StringToHash("Attack");

    private void Awake() {
        enemyFollow = GetComponent<EnemyFollow>();
        animator = GetComponent<Animator>();
    }

    private void Start() {
        if (enemyFollow != null)
            playerTransform = enemyFollow.GetPlayerTransform();

        if (playerTransform == null) {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        if (playerTransform != null)
            playerDamageable = playerTransform.GetComponent<IDamageable>();
    }

    private void Update() {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        bool inAttackRange = distanceToPlayer <= attackRange;

        if (enemyFollow != null)
            enemyFollow.SetMovementEnabled(!inAttackRange);

        if (inAttackRange && Time.time - lastAttackTime >= attackCooldown) {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    /// <summary>
    /// Receives the attack stats from the base Enemy class.
    /// Called by Enemy.Awake() after InitializeStats() configures the stats.
    /// </summary>
    public void Configure(float damage, float range, float cooldown) {
        attackDamage = damage;
        attackRange = range;
        attackCooldown = cooldown;
    }

    /// <summary>
    /// Triggers the enemy's attack animation and applies damage to the player if the player is damageable.
    /// </summary>
    /// <remarks>This method activates the 'Attack' trigger in the associated Animator, which must be configured in
    /// the Animator Controller. If the player implements the damageable interface, the method reduces the player's health
    /// by the configured attack damage. No action is taken if the player does not support damage reception.</remarks>
    private void Attack() {
        if (animator != null)
            animator.SetTrigger(HashAttack);

        playerDamageable?.TakeDamage(attackDamage);
    }
}
