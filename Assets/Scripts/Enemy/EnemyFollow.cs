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
    /// </summary>
    public void SetMovementEnabled(bool enabled) {
        if (Agent != null && Agent.isOnNavMesh)
            Agent.isStopped = !enabled;
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
