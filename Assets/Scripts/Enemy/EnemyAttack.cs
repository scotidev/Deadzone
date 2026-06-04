using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Controls the melee attack behavior for all enemy types.
/// </summary>
public class EnemyAttack : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Barricade Settings")]
    [SerializeField] private float barricadeCheckDistance = 10f;
    [SerializeField] private LayerMask barricadeLayer;
    [Tooltip("Distance at which the enemy starts attacking an obstacle.")]
    [SerializeField] private float barricadeAttackRange = 2.5f;
    [Tooltip("Radius to search for obstacles near the blocked NavMesh corner.")]
    [SerializeField] private float barricadeSearchRadius = 3f;

    #endregion

    #region FIELDS

    private float attackDamage;
    private float attackRange;
    private float attackCooldown;

    private float lastAttackTime;

    private EnemyFollow enemyFollow;
    private EnemyBase enemyBase;
    private Transform playerTransform;
    private IDamageable playerDamageable;
    private IDamageable currentTarget;

    private Animator animator;
    private NavMeshAgent navMeshAgent;

    private static readonly int HashAttack = Animator.StringToHash("Attack");

    #endregion

    #region UNITY

    private void Awake() {
        enemyFollow = GetComponent<EnemyFollow>();
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
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

        CheckForObstacleOnPath();

        if (currentTarget != null) {
            HandleObstacleAttack();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
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
    /// Receives reference to EnemyBase for calling audio methods.
    /// </summary>
    public void SetEnemyBase(EnemyBase enemyBase) {
        this.enemyBase = enemyBase;
    }

    /// <summary>
    /// Checks whether an obstacle (barricade or explosive barrel) is blocking the path to the player.
    /// Uses the NavMesh to detect blockage, then searches near the blocked corner for obstacles.
    /// </summary>
    private void CheckForObstacleOnPath() {
        if (enemyFollow == null) return;

        // If the player is reachable via NavMesh, no obstacle is blocking the path
        if (enemyFollow.CanReachPlayer()) {
            ClearCurrentTarget();
            return;
        }

        // NavMesh path is blocked — find the blocked corner
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;

        NavMeshPath path = new NavMeshPath();
        navMeshAgent.CalculatePath(playerTransform.position, path);

        if (path.status == NavMeshPathStatus.PathComplete) {
            ClearCurrentTarget();
            return;
        }

        // Search near the last corner of the blocked path for obstacles
        Vector3 searchCenter = path.corners.Length > 0
            ? path.corners[path.corners.Length - 1]
            : transform.position;

        Collider[] hits = Physics.OverlapSphere(searchCenter, barricadeSearchRadius, barricadeLayer);
        IDamageable closestTarget = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits) {
            Barricade b = hit.GetComponent<Barricade>();
            if (b != null && !b.IsDestroyed) {
                float d = Vector3.Distance(transform.position, b.transform.position);
                if (d < closestDist && d <= barricadeCheckDistance) {
                    closestDist = d;
                    closestTarget = b;
                }
                continue;
            }

            ExplosiveBarrel barrel = hit.GetComponent<ExplosiveBarrel>();
            if (barrel != null && !barrel.IsExploding) {
                float d = Vector3.Distance(transform.position, barrel.transform.position);
                if (d < closestDist && d <= barricadeCheckDistance) {
                    closestDist = d;
                    closestTarget = barrel;
                }
            }
        }

        if (closestTarget != null) {
            currentTarget = closestTarget;
            enemyFollow.SetOverrideDestination(((MonoBehaviour)closestTarget).transform);
        } else {
            ClearCurrentTarget();
        }
    }

    /// <summary>
    /// Handles attacking the current obstacle target.
    /// Navigates toward it and attacks when within barricadeAttackRange.
    /// </summary>
    private void HandleObstacleAttack() {
        if (currentTarget == null) {
            ClearCurrentTarget();
            return;
        }

        // Check if target game object was destroyed
        MonoBehaviour targetBehaviour = currentTarget as MonoBehaviour;
        if (targetBehaviour == null || targetBehaviour.gameObject == null) {
            ClearCurrentTarget();
            return;
        }

        // Check if barricade was destroyed
        if (currentTarget is Barricade barricade && barricade.IsDestroyed) {
            ClearCurrentTarget();
            return;
        }

        // Check if barrel has already started exploding
        if (currentTarget is ExplosiveBarrel barrel && barrel.IsExploding) {
            ClearCurrentTarget();
            return;
        }

        // Re-check if the player is now reachable
        if (enemyFollow != null && enemyFollow.CanReachPlayer()) {
            ClearCurrentTarget();
            enemyFollow.SetMovementEnabled(true);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetBehaviour.transform.position);

        if (distanceToTarget <= barricadeAttackRange) {
            if (enemyFollow != null)
                enemyFollow.SetMovementEnabled(false);

            if (Time.time - lastAttackTime >= attackCooldown) {
                AttackObstacle();
                lastAttackTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Attack the player by triggering the attack animation and applying damage through the IDamageable interface.
    /// Attempts to use TakeDamageFromZombie() if PlayerHealth is available (for correct damage sound).
    /// Falls back to TakeDamage() for other IDamageable implementations.
    /// Also plays the attack sound for this zombie.
    /// </summary>
    private void AttackPlayer() {
        if (animator != null)
            animator.SetTrigger(HashAttack);

        // Play attack sound
        if (enemyBase != null)
            enemyBase.PlayAttackSound();

        // Try to cast to PlayerHealth for specialized zombie damage with correct audio
        if (playerDamageable is PlayerHealth playerHealth) {
            playerHealth.TakeDamageFromZombie(attackDamage);
        } else {
            // Fallback for other IDamageable implementations
            playerDamageable?.TakeDamage(attackDamage);
        }
    }

    /// <summary>
    /// Performs an attack action on the current obstacle (barricade or explosive barrel).
    /// Also plays the attack sound for this zombie.
    /// </summary>
    private void AttackObstacle() {
        if (animator != null)
            animator.SetTrigger(HashAttack);

        // Play attack sound
        if (enemyBase != null)
            enemyBase.PlayAttackSound();

        currentTarget?.TakeDamage(attackDamage);
    }

    /// <summary>
    /// Clears the current target and resets enemy navigation to the player.
    /// </summary>
    private void ClearCurrentTarget() {
        currentTarget = null;
        if (enemyFollow != null)
            enemyFollow.ClearOverrideDestination();
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