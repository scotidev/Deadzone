using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A special zombie for the tutorial that stays still and only takes damage.
/// Deactivates specified barriers when it dies to allow player progression.
/// </summary>
public class TutorialDummyZombie : EnemyBase {

    #region SERIALIZED FIELDS

    [Header("Tutorial Settings")]
    [Tooltip("Health value for this dummy, set in the inspector.")]
    [SerializeField] private float tutorialHealth = 50f;

    [Tooltip("The Pistol pickup GameObject to activate when this zombie dies.")]
    [SerializeField] private GameObject pistolPickupObject;

    #endregion

    #region UNITY

    protected override void Awake() {
        // Force this to be a tutorial enemy so it doesn't count for waves
        isTutorialEnemy = true;

        base.Awake();

        // Deactivate movement and attack components so it stays standing still
        if (enemyFollow != null) enemyFollow.enabled = false;
        if (enemyAttack != null) enemyAttack.enabled = false;
    }

    #endregion

    #region PROTECTED METHODS

    /// <summary>
    /// Initializes stats using the value defined in the inspector.
    /// Overrides the abstract method from EnemyBase.
    /// </summary>
    protected override void InitializeStats() {
        maxHealth = tutorialHealth;
    }

    /// <summary>
    /// Handles dummy death: activates the pistol pickup for the player.
    /// </summary>
    protected override void Die() {
        // Ativa o pickup da pistola para o jogador coletar após a morte do zumbi
        if (pistolPickupObject != null) {
            pistolPickupObject.SetActive(true);
            Debug.Log("[TutorialDummyZombie] Pistol pickup activated!");
        }

        // Call base Die to handle currency, events, and destruction
        base.Die();
    }

    #endregion
}
