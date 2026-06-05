using UnityEngine;

/// <summary>
/// EnemyBase subclass for the Penguin transformation.
/// Has 1 HP, zero attack damage (deals no damage but EnemyAttack still runs
/// to stop the penguin when close to the player).
/// Keeps wave-scaled reward from the original zombie.
/// </summary>
public class PenguinEnemy : EnemyBase {

    #region SERIALIZED FIELDS

    [Header("Penguin Audio")]
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip dieClip;

    #endregion

    #region METHODS

    /// <summary>
    /// Called by EnemyBase.Awake().
    /// HP, attack damage, and reward scaling are handled by ApplyWaveScaling()
    /// using the serialized values configured in the Inspector.
    /// Attack range and cooldown are kept normal so that EnemyFollow stops
    /// the penguin when it reaches the player (EnemyAttack deals 0 damage).
    /// </summary>
    protected override void InitializeStats() {
        moveSpeed = 3.5f;
        attackRange = 1.8f;
        attackCooldown = 1.5f;
    }

    /// <summary>
    /// Allows the Easter egg system to override the reward
    /// to match the original zombie's wave-scaled value.
    /// </summary>
    public void SetReward(int value) {
        rewardCurrency = value;
    }

    /// <summary>
    /// Plays the idle sound while the penguin is moving.
    /// </summary>
    public override void PlayIdleSound() {
        if (idleClip == null) return;

        Play3DSound(idleClip, 0.6f, 3f, 30f);
    }

    /// <summary>
    /// Penguins disappear faster than zombies (1s vs 2s).
    /// </summary>
    protected override float GetDeathDestroyDelay() => 0.8f;

    /// <summary>
    /// Plays the death sound when the penguin is killed.
    /// </summary>
    public override void PlayDeathSound() {
        if (dieClip == null) return;

        Play3DSound(dieClip, 0.7f, 3f, 30f);
    }

    #endregion
}
