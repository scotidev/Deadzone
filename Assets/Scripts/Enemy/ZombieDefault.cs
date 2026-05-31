using UnityEngine;

/// <summary>
/// Default Zombie with balanced stats.
/// </summary>
public class ZombieDefault : EnemyBase {

    [Header("Default Zombie Stats")]
    [SerializeField] private float defaultMaxHealth = 100f;
    [SerializeField] private float defaultMoveSpeed = 3.0f;
    [SerializeField] private float defaultAttackDamage = 10f;
    [SerializeField] private float defaultAttackRange = 1.8f;
    [SerializeField] private float defaultAttackCooldown = 1.5f;

    [Header("Default Zombie Audio")]
    [SerializeField] private AudioClip idleSound;
    [Range(0f, 1f)]
    [SerializeField] private float idleSoundVolume = 1f;

    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)]
    [SerializeField] private float attackSoundVolume = 1f;

    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)]
    [SerializeField] private float deathSoundVolume = 1f;

    [Header("Default Zombie Reward")]
    [SerializeField] private int defaultRewardCurrency = 100;

    /// <summary>
    /// Initialize default zombie stats. Called during Awake() by EnemyBase.
    /// Uses serialized fields if set, otherwise uses fallback values.
    /// </summary>
    protected override void InitializeStats() {
        maxHealth = defaultMaxHealth > 0 ? defaultMaxHealth : 100f;
        moveSpeed = defaultMoveSpeed > 0 ? defaultMoveSpeed : 3.0f;
        attackDamage = defaultAttackDamage > 0 ? defaultAttackDamage : 10f;
        attackRange = defaultAttackRange > 0 ? defaultAttackRange : 1.8f;
        attackCooldown = defaultAttackCooldown > 0 ? defaultAttackCooldown : 1.5f;
        rewardCurrency = defaultRewardCurrency > 0 ? defaultRewardCurrency : 100;
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
