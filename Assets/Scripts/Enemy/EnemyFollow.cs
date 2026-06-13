using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Responsible for making the enemy follow the player using Unity's NavMeshAgent.
/// The scene must have a baked NavMesh for this to work.
/// Handles walk animation and idle sound playback.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFollow : MonoBehaviour {

    #region FIELDS

    private NavMeshAgent agent;
    private Transform playerTransform;
    private bool isStunned = false;
    private Transform overrideDestination;
    private Animator animator;
    private EnemyBase enemyBase;

    private static readonly int HashIsWalking = Animator.StringToHash("IsWalking");

    // Idle sound timing
    private float idleSoundTimer = 0f;
    private float idleSoundNextTime = 0f;

    // SetDestination threshold
    // CONCEITO: Só recalcula pathfinding quando o player se moveu mais que isso.
    // Evita recálculo desnecessário quando o player está parado ou se movendo pouco.
    private Vector3 lastSetDestinationPosition;
    private const float DESTINATION_THRESHOLD = 1.0f;

    // NavMeshPath cacheado pra CanReachPlayer
    // CONCEITO: Reusa o mesmo objeto em vez de new NavMeshPath() a cada chamada.
    // Criado no Awake porque NavMeshPath usa código nativo da Unity.
    private NavMeshPath cachedPath;

    private NavMeshAgent Agent {
        get {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            return agent;
        }
    }

    #endregion

    #region UNITY

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        // CONCEITO: NavMeshPath precisa ser criado em Awake, não no field initializer.
        cachedPath = new NavMeshPath();
        FindPlayer();
        ResetIdleSoundTimer();
    }

    private void Update() {
        if (playerTransform == null || Agent == null || !Agent.enabled || Agent.isStopped)
            return;

        if (overrideDestination != null && overrideDestination.gameObject.activeInHierarchy)
        {
            Agent.SetDestination(overrideDestination.position);
        }
        else
        {
            // CONCEITO: Só recalcula pathfinding se o player moveu mais que o threshold.
            // SetDestination dispara um recálculo interno do NavMesh, que é caro.
            // Com 15 inimigos, pular 90% dos recálculos quando o player está parado
            // é uma economia gigante de CPU.
            if (Vector3.Distance(playerTransform.position, lastSetDestinationPosition) > DESTINATION_THRESHOLD)
            {
                Agent.SetDestination(playerTransform.position);
                lastSetDestinationPosition = playerTransform.position;
            }
        }

        UpdateWalkAnimation();
        UpdateIdleSound();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Public API for Enemy to set the NavMeshAgent's movement speed after stats are initialized.
    /// Defines the movement speed of the NavMeshAgent. Called by Enemy.Awake() after InitializeStats() sets moveSpeed.
    /// </summary>
    public void SetSpeed(float speed) {
        Agent.speed = speed;
    }

    /// <summary>
    /// Receives reference to EnemyBase for calling audio methods.
    /// </summary>
    public void SetEnemyBase(EnemyBase enemyBase) {
        this.enemyBase = enemyBase;
    }

    /// <summary>
    /// Updates the IsWalking animator parameter based on NavMeshAgent movement.
    /// Checks if agent has velocity to determine if it's walking or idle.
    /// </summary>
    private void UpdateWalkAnimation() {
        if (animator == null)
            return;

        // Check if the agent is actually moving (velocity > 0)
        bool isMoving = Agent.velocity.sqrMagnitude > 0.1f;
        animator.SetBool(HashIsWalking, isMoving);
    }

    /// <summary>
    /// Updates idle sound playback at random intervals (3-8 seconds).
    /// Only plays when agent is moving (walking).
    /// </summary>
    private void UpdateIdleSound() {
        if (enemyBase == null || Agent.isStopped)
            return;

        idleSoundTimer += Time.deltaTime;

        if (idleSoundTimer >= idleSoundNextTime) {
            enemyBase.PlayIdleSound();
            ResetIdleSoundTimer();
        }
    }

    /// <summary>
    /// Resets the idle sound timer to a new random interval (3-8 seconds).
    /// </summary>
    private void ResetIdleSoundTimer() {
        idleSoundTimer = 0f;
        idleSoundNextTime = Random.Range(3f, 8f);
    }

    /// <summary>
    /// Enables or disables the NavMeshAgent's movement.
    /// When stunned, this respects the stun lock and only allows re-enabling if not stunned.
    /// This prevents EnemyAttack from re-enabling movement while the enemy is stunned by a trap.
    /// </summary>
    public void SetMovementEnabled(bool enabled) {
        // If we're trying to enable movement but the enemy is stunned, don't allow it
        if (enabled && isStunned) {
            Logger.Log($"[EnemyFollow] SetMovementEnabled(true) called but stunned, rejecting");
            return;
        }

        Logger.Log($"[EnemyFollow] SetMovementEnabled({enabled}) - isStopped will be: {!enabled}, Agent valid: {Agent != null && Agent.isOnNavMesh}");
        
        if (Agent != null && Agent.isOnNavMesh) {
            Agent.isStopped = !enabled;
            Logger.Log($"[EnemyFollow] NavMeshAgent.isStopped set to: {Agent.isStopped}");
        } else {
            Logger.LogWarning($"[EnemyFollow] Agent is null or not on NavMesh!");
        }
    }

    /// <summary>
    /// Sets the stun lock flag. When true, SetMovementEnabled(true) calls are ignored.
    /// Called by BearTrap when applying stun, and cleared when stun duration expires.
    /// </summary>
    public void SetStunned(bool stunned) {
        Logger.Log($"[EnemyFollow] SetStunned({stunned})");
        isStunned = stunned;
    }

    /// <summary>
    /// Overrides the enemy's destination to a specific target instead of the player.
    /// Used by EnemyAttack to make enemies navigate toward obstacles (barricades/barrels).
    /// </summary>
    public void SetOverrideDestination(Transform target) {
        overrideDestination = target;
    }

    /// <summary>
    /// Clears the override destination, returning the enemy to chasing the player.
    /// </summary>
    public void ClearOverrideDestination() {
        overrideDestination = null;
    }

    /// <summary>
    /// Checks whether the player is reachable via the NavMesh.
    /// Returns true if there is a complete path (no obstruction).
    /// Used by EnemyAttack to decide if a barricade is actually blocking the way.
    /// </summary>
    public bool CanReachPlayer() {
        if (playerTransform == null || agent == null || !agent.isOnNavMesh)
            return false;

        // CONCEITO: Reusa cachedPath em vez de alocar um novo NavMeshPath a cada chamada.
        // EnemyAttack chama este método todo frame, então a alocação acumula rápido.
        agent.CalculatePath(playerTransform.position, cachedPath);
        return cachedPath.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Returns the Transform of the player (found by FindPlayer).
    /// Called by EnemyAttack.Start() to get the player reference
    /// without needing another GameObject.FindWithTag() call.
    /// </summary>
    public Transform GetPlayerTransform() {
        if (playerTransform == null)
            FindPlayer();
        return playerTransform;
    }

    /// <summary>
    /// Gets the player's Transform via PlayerCache, which only searches the scene once.
    /// CONCEITO: PlayerCache usa um cache estático que evita varrer
    /// a hierarquia da cena toda vez que um inimigo nasce.
    /// O primeiro inimigo a chamar paga o custo do FindWithTag,
    /// todos os outros ganham a referência de graça.
    /// </summary>
    private void FindPlayer() {
        playerTransform = PlayerCache.Transform;
    }

    #endregion
}
