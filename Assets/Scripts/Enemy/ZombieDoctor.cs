using UnityEngine;

/// <summary>
/// Zombie Doctor - A more durable zombie variant with high health pool.
/// </summary>
public class ZombieDoctor : EnemyBase {

    [Header("Doctor Zombie Stats")]
    [SerializeField] private float defaultMoveSpeed = 2.8f;
    [SerializeField] private float defaultAttackRange = 1.8f;
    [SerializeField] private float defaultAttackCooldown = 1.6f;

    [Header("Doctor Zombie Audio")]
    [SerializeField] private AudioClip idleSound;
    [Range(0f, 1f)]
    [SerializeField] private float idleSoundVolume = 1f;

    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)]
    [SerializeField] private float attackSoundVolume = 1f;

    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)]
    [SerializeField] private float deathSoundVolume = 1f;

    /// <summary>
    /// Initialize doctor zombie stats. Called during Awake() by EnemyBase.
    /// Note: health, damage, and reward are set by ApplyWaveScaling() in EnemyBase.
    /// </summary>
    protected override void InitializeStats() {
        moveSpeed = defaultMoveSpeed > 0 ? defaultMoveSpeed : 2.8f;
        attackRange = defaultAttackRange > 0 ? defaultAttackRange : 1.8f;
        attackCooldown = defaultAttackCooldown > 0 ? defaultAttackCooldown : 1.6f;
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
}
