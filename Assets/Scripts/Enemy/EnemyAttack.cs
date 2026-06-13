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

    // CONCEITO: NavMeshPath cacheado — reusa o MESMO objeto em vez de alocar um novo
    // a cada frame. new NavMeshPath() toda vez = pressão no GC.
    // Criado no Awake porque NavMeshPath usa código nativo da Unity e não pode
    // ser inicializado na declaração do campo (dá UnityException).
    private NavMeshPath cachedPath;

    // CONCEITO: Buffer pré-alocado pra OverlapSphereNonAlloc.
    // OverlapSphere comum aloca um Collider[] novo a cada chamada.
    // NonAlloc reusa o mesmo array, sem alocação.
    private Collider[] hitBuffer = new Collider[16];

    #endregion

    #region UNITY

    private void Awake() {
        enemyFollow = GetComponent<EnemyFollow>();
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        // CONCEITO: NavMeshPath precisa ser criado em Awake, não no field initializer,
        // porque chama código nativo (InitializeNavMeshPath) que exige o engine pronto.
        cachedPath = new NavMeshPath();
    }

    private void Start() {
        if (enemyFollow != null)
            playerTransform = enemyFollow.GetPlayerTransform();

        // CONCEITO: Fallback usando PlayerCache em vez de FindWithTag.
        // PlayerCache só procura na cena UMA VEZ e guarda a referência.
        if (playerTransform == null)
            playerTransform = PlayerCache.Transform;

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

        // CONCEITO: Reusa cachedPath em vez de criar new NavMeshPath() a cada frame.
        // NavMesh.CalculatePath preenche o objeto existente em vez de alocar um novo.
        navMeshAgent.CalculatePath(playerTransform.position, cachedPath);

        if (cachedPath.status == NavMeshPathStatus.PathComplete) {
            ClearCurrentTarget();
            return;
        }

        // Search near the last corner of the blocked path for obstacles
        Vector3 searchCenter = cachedPath.corners.Length > 0
            ? cachedPath.corners[cachedPath.corners.Length - 1]
            : transform.position;

        // CONCEITO: OverlapSphereNonAlloc reusa hitBuffer em vez de alocar um array novo.
        // Retorna a quantidade de colliders encontrados (útil pra iterar só até o count).
        int hitCount = Physics.OverlapSphereNonAlloc(searchCenter, barricadeSearchRadius, hitBuffer, barricadeLayer);
        IDamageable closestTarget = null;
        float closestDist = float.MaxValue;

        // CONCEITO: for loop em vez de foreach — evita alocação do enumerator.
        // Só itera até hitCount (quantidade real de hits) em vez do buffer inteiro.
        for (int i = 0; i < hitCount; i++) {
            Collider hit = hitBuffer[i];
            // CONCEITO: TryGetComponent é MAIS EFICIENTE que GetComponent<T>().
            // GetComponent<T>() aloca memória indiretamente via boxing em certos casos,
            // enquanto TryGetComponent é um método nativo da Unity que não aloca nada.
            // A diferença é pequena por chamada, mas num loop a cada frame, acumula.
            if (hit.TryGetComponent(out Barricade b) && !b.IsDestroyed) {
                float d = Vector3.Distance(transform.position, b.transform.position);
                if (d < closestDist && d <= barricadeCheckDistance) {
                    closestDist = d;
                    closestTarget = b;
                }
                continue;
            }

            if (hit.TryGetComponent(out ExplosiveBarrel barrel) && !barrel.IsExploding) {
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