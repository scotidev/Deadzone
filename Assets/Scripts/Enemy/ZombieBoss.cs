using UnityEngine;

/// <summary>
/// Boss zombie enemy type. Much stronger and tankier than regular zombies.
/// Immune to crowd control effects like stuns from traps.
/// </summary>
public class ZombieBoss : EnemyBase {

    #region SERIALIZED FIELDS

    [Header("Boss Zombie Stats")]
    [SerializeField] private float defaultMoveSpeed = 3.0f;
    [SerializeField] private float defaultAttackRange = 2.0f;
    [SerializeField] private float defaultAttackCooldown = 1.2f;

    [Header("Boss Zombie Audio")]
    [SerializeField] private AudioClip idleSound;
    [Range(0f, 1f)]
    [SerializeField] private float idleSoundVolume = 1f;

    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)]
    [SerializeField] private float attackSoundVolume = 1f;

    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)]
    [SerializeField] private float deathSoundVolume = 1f;

    #endregion

    #region METHODS

    /// <summary>
    /// Initialize boss stats. Called during Awake() by EnemyBase.
    /// Boss has significantly higher health, damage, and attack speed than other zombie types.
    /// Note: health, damage, and reward are set by ApplyWaveScaling() in EnemyBase.
    /// </summary>
    protected override void InitializeStats() {
        moveSpeed = defaultMoveSpeed > 0 ? defaultMoveSpeed : 3.0f;
        attackRange = defaultAttackRange > 0 ? defaultAttackRange : 2.0f;
        attackCooldown = defaultAttackCooldown > 0 ? defaultAttackCooldown : 1.2f;
    }

    /// <summary>
    /// Plays the idle/grunt sound for this zombie type.
    /// </summary>
    public override void PlayIdleSound() {
        Play3DSound(idleSound, idleSoundVolume);
    }

    /// <summary>
    /// Plays the attack sound for this zombie type.
    /// </summary>
    public override void PlayAttackSound() {
        Play3DSound(attackSound, attackSoundVolume);
    }

    /// <summary>
    /// Plays the death sound for this zombie type.
    /// </summary>
    public override void PlayDeathSound() {
        Play3DSound(deathSound, deathSoundVolume);
    }

    #endregion
}
