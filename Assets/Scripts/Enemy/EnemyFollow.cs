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

    private float idleSoundTimer = 0f;
    private float idleSoundNextTime = 0f;

    private Vector3 lastSetDestinationPosition;

    private NavMeshPath cachedPath;

    #endregion

    #region PROPERTIES

    private NavMeshAgent Agent {
        get {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            return agent;
        }
    }

    #endregion

    #region CONSTANTS

    private static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
    private const float DESTINATION_THRESHOLD = 1.0f;

    #endregion

    #region UNITY

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
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
    /// Checks if agent has velocity to determine if it is walking or idle.
    /// </summary>
    private void UpdateWalkAnimation() {
        if (animator == null)
            return;

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
        if (enabled && isStunned) {
            Logger.Log($"[EnemyFollow] SetMovementEnabled(true) called but stunned, rejecting");
            return;
        }

        if (Agent != null && Agent.isOnNavMesh) {
            Agent.isStopped = !enabled;
        } else {
            Logger.LogWarning($"[EnemyFollow] Agent is null or not on NavMesh!");
        }
    }

    /// <summary>
    /// Sets the stun lock flag. When true, SetMovementEnabled(true) calls are ignored.
    /// Called by BearTrap when applying stun, and cleared when stun duration expires.
    /// </summary>
    public void SetStunned(bool stunned) {
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

        agent.CalculatePath(playerTransform.position, cachedPath);
        return cachedPath.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Returns the Transform of the player (found by FindPlayer).
    /// Called by EnemyAttack.Start() to get the player reference without needing another Find call.
    /// </summary>
    public Transform GetPlayerTransform() {
        if (playerTransform == null)
            FindPlayer();
        return playerTransform;
    }

    /// <summary>
    /// Gets the player's Transform via PlayerCache, which only searches the scene once.
    /// PlayerCache uses a static cache that avoids scanning the scene hierarchy every time an enemy spawns.
    /// The first enemy to call it pays the cost of FindWithTag, all others get the reference for free.
    /// </summary>
    private void FindPlayer() {
        playerTransform = PlayerCache.Transform;
    }

    #endregion
}
