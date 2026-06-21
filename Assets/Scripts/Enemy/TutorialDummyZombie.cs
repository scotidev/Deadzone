using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A special zombie for the tutorial that stays still and only takes damage.
/// Deactivates specified barriers when it dies to allow player progression.
/// Plays idle sounds at random intervals and a death sound on destruction.
/// </summary>
public class TutorialDummyZombie : EnemyBase {

    #region SERIALIZED FIELDS

    [Header("Tutorial Settings")]
    [Tooltip("Health value for this dummy, set in the inspector.")]
    [SerializeField] private float tutorialHealth = 50f;

    [Tooltip("The Pistol pickup GameObject to activate when this zombie dies.")]
    [SerializeField] private GameObject pistolPickupObject;

    [Header("Idle Sounds")]
    [Tooltip("Array of idle sounds played at random intervals.")]
    [SerializeField] private AudioClip[] idleSounds;
    [Range(0f, 1f)]
    [SerializeField] private float idleSoundVolume = 1f;
    [Tooltip("Random interval range between idle sound plays (min, max).")]
    [SerializeField] private Vector2 idleIntervalRange = new Vector2(5f, 7f);

    [Header("Death Sound")]
    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)]
    [SerializeField] private float deathSoundVolume = 1f;

    #endregion

    #region FIELDS

    private float idleSoundTimer = 0f;
    private float idleSoundNextTime = 0f;

    #endregion

    #region UNITY

    protected override void Awake() {
        isTutorialEnemy = true;

        base.Awake();

        if (enemyFollow != null) enemyFollow.enabled = false;
        if (enemyAttack != null) enemyAttack.enabled = false;

        ResetIdleSoundTimer();
    }

    private void Update() {
        HandleIdleSound();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Initializes stats using the value defined in the inspector.
    /// Overrides the abstract method from EnemyBase.
    /// </summary>
    protected override void InitializeStats() {
        maxHealth = tutorialHealth;
    }

    /// <summary>
    /// Plays a random idle sound from the idleSounds array at random intervals.
    /// </summary>
    private void HandleIdleSound() {
        if (idleSounds == null || idleSounds.Length == 0)
            return;

        idleSoundTimer += Time.deltaTime;

        if (idleSoundTimer >= idleSoundNextTime) {
            AudioClip clip = idleSounds[Random.Range(0, idleSounds.Length)];
            Play3DSound(clip, idleSoundVolume);
            ResetIdleSoundTimer();
        }
    }

    /// <summary>
    /// Resets the idle sound timer to a new random interval within the configured range.
    /// </summary>
    private void ResetIdleSoundTimer() {
        idleSoundTimer = 0f;
        idleSoundNextTime = Random.Range(idleIntervalRange.x, idleIntervalRange.y);
    }

    /// <summary>
    /// Plays the death sound when this dummy zombie dies.
    /// Overrides the virtual method from EnemyBase, which is called by Die().
    /// </summary>
    public override void PlayDeathSound() {
        Play3DSound(deathSound, deathSoundVolume);
    }

    /// <summary>
    /// Handles dummy death: activates the pistol pickup for the player.
    /// </summary>
    protected override void Die() {
        if (pistolPickupObject != null) {
            pistolPickupObject.SetActive(true);
        }

        base.Die();
    }

    #endregion
}
