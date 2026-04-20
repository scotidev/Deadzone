using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls the melee attack behavior for all enemy types.
///
/// When the player enters the attack range (attackRange), the enemy
/// stops moving and applies damage periodically via IDamageable.
/// When the player moves away, movement is automatically resumed.
/// If a barricade blocks the path to the player, the enemy will attack and destroy it.
/// </summary>
public class EnemyAttack : MonoBehaviour {

    [Header("Barricade Settings")]
    [SerializeField] private float barricadeCheckDistance = 10f;
    [SerializeField] private LayerMask barricadeLayer;

    private float attackDamage;
    private float attackRange;
    private float attackCooldown;

    private float lastAttackTime;

    private EnemyFollow enemyFollow;
    private Transform playerTransform;
    private IDamageable playerDamageable;
    private Barricade currentBarricade;

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

        if (barricadeLayer.value == 0)
            barricadeLayer = LayerMask.GetMask("Obstacle");
    }

    private void Update() {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (currentBarricade != null && !currentBarricade.IsDestroyed) {
            HandleBarricadeAttack(distanceToPlayer);
            return;
        }

        CheckForBarricadeOnPath();

        bool inAttackRange = distanceToPlayer <= attackRange;

        if (enemyFollow != null)
            enemyFollow.SetMovementEnabled(!inAttackRange);

        if (inAttackRange && Time.time - lastAttackTime >= attackCooldown) {
            AttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    private void CheckForBarricadeOnPath() {
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float checkDist = Mathf.Min(distanceToPlayer, barricadeCheckDistance);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, checkDist, barricadeLayer)) {
            Barricade barricade = hit.collider.GetComponent<Barricade>();
            if (barricade != null && !barricade.IsDestroyed) {
                currentBarricade = barricade;
            }
        }
    }

    private void HandleBarricadeAttack(float distanceToPlayer) {
        if (currentBarricade.IsDestroyed) {
            currentBarricade = null;
            return;
        }

        if (enemyFollow != null)
            enemyFollow.SetMovementEnabled(false);

        if (Time.time - lastAttackTime >= attackCooldown) {
            AttackBarricade();
            lastAttackTime = Time.time;
        }
    }

    private void AttackPlayer() {
        if (animator != null)
            animator.SetTrigger(HashAttack);

        playerDamageable?.TakeDamage(attackDamage);
    }

    private void AttackBarricade() {
        if (animator != null)
            animator.SetTrigger(HashAttack);

        currentBarricade?.TakeDamage(attackDamage);
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
}