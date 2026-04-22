using UnityEngine;

/// <summary>
/// Controls the melee attack behavior for all enemy types.
/// </summary>
public class EnemyAttack : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Barricade Settings")]
    [SerializeField] private float barricadeCheckDistance = 10f;
    [SerializeField] private LayerMask barricadeLayer;

    #endregion

    #region FIELDS

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

    #endregion

    #region UNITY

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

        CheckForBarricadeOnPath();

        if (currentBarricade != null && !currentBarricade.IsDestroyed) {
            HandleBarricadeAttack(distanceToPlayer);
            return;
        }

        bool inAttackRange = distanceToPlayer <= attackRange;

        if (enemyFollow != null)
            enemyFollow.SetMovementEnabled(!inAttackRange);

        if (inAttackRange && Time.time - lastAttackTime >= attackCooldown) {
            AttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Checks for the presence of an undestroyed barricade along the path between the current object and the player,
    /// and updates the current barricade reference if one is detected.
    /// </summary>
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

    /// <summary>
    /// Handles the logic for attacking the current barricade when the enemy is within range and the attack cooldown has
    /// elapsed.
    /// </summary>
    /// <param name="distanceToPlayer">The distance, in world units, between the enemy and the player. Used to determine if the enemy is close enough
    /// to attack the barricade.</param>
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

    /// <summary>
    /// Attack the player by triggering the attack animation and applying damage through the IDamageable interface.
    /// </summary>
    private void AttackPlayer() {
        if (animator != null)
            animator.SetTrigger(HashAttack);

        playerDamageable?.TakeDamage(attackDamage);
    }

    /// <summary>
    /// Performs an attack action on the current barricade, applying damage if a barricade is present.
    /// </summary>
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

    #endregion
}