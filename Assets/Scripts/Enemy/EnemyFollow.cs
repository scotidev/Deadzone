using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Responsible for making the enemy follow the player using Unity's NavMeshAgent.
/// The scene must have a baked NavMesh for this to work.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFollow : MonoBehaviour {

    #region FIELDS

    private NavMeshAgent agent;
    private Transform playerTransform;
    private bool isStunned = false;

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
        FindPlayer();
    }

    private void Update() {
        if (playerTransform == null || Agent == null || !Agent.enabled || Agent.isStopped)
            return;

        Agent.SetDestination(playerTransform.position);
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
    /// Enables or disables the NavMeshAgent's movement.
    /// When stunned, this respects the stun lock and only allows re-enabling if not stunned.
    /// This prevents EnemyAttack from re-enabling movement while the enemy is stunned by a trap.
    /// </summary>
    public void SetMovementEnabled(bool enabled) {
        // If we're trying to enable movement but the enemy is stunned, don't allow it
        if (enabled && isStunned) {
            Debug.Log($"[EnemyFollow] SetMovementEnabled(true) called but stunned, rejecting");
            return;
        }

        Debug.Log($"[EnemyFollow] SetMovementEnabled({enabled}) - isStopped will be: {!enabled}, Agent valid: {Agent != null && Agent.isOnNavMesh}");
        
        if (Agent != null && Agent.isOnNavMesh) {
            Agent.isStopped = !enabled;
            Debug.Log($"[EnemyFollow] NavMeshAgent.isStopped set to: {Agent.isStopped}");
        } else {
            Debug.LogWarning($"[EnemyFollow] Agent is null or not on NavMesh!");
        }
    }

    /// <summary>
    /// Sets the stun lock flag. When true, SetMovementEnabled(true) calls are ignored.
    /// Called by BearTrap when applying stun, and cleared when stun duration expires.
    /// </summary>
    public void SetStunned(bool stunned) {
        Debug.Log($"[EnemyFollow] SetStunned({stunned})");
        isStunned = stunned;
    }

    /// <summary>
    /// Checks whether the player is reachable via the NavMesh.
    /// Returns true if there is a complete path (no obstruction).
    /// Used by EnemyAttack to decide if a barricade is actually blocking the way.
    /// </summary>
    public bool CanReachPlayer() {
        if (playerTransform == null || agent == null || !agent.isOnNavMesh)
            return false;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(playerTransform.position, path);
        return path.status == NavMeshPathStatus.PathComplete;
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
    /// Searches the entire scene for the first GameObject with the tag "Player" and stores its Transform.
    /// </summary>
    private void FindPlayer() {
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            return;
    }

    #endregion
}
