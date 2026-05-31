using UnityEngine;

/// <summary>
/// Zombie Cop - A more aggressive zombie variant with high damage output.
/// </summary>
public class ZombieCop : EnemyBase {

    [Header("Cop Zombie Stats")]
    [SerializeField] private float defaultMaxHealth = 140f;
    [SerializeField] private float defaultMoveSpeed = 3.2f;
    [SerializeField] private float defaultAttackDamage = 22f;
    [SerializeField] private float defaultAttackRange = 1.9f;
    [SerializeField] private float defaultAttackCooldown = 1.3f;

    [Header("Cop Zombie Audio")]
    [SerializeField] private AudioClip idleSound;
    [Range(0f, 1f)]
    [SerializeField] private float idleSoundVolume = 1f;

    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)]
    [SerializeField] private float attackSoundVolume = 1f;

    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)]
    [SerializeField] private float deathSoundVolume = 1f;

    [Header("Cop Zombie Reward")]
    [SerializeField] private int defaultRewardCurrency = 150;

    /// <summary>
    /// Initialize cop zombie stats. Called during Awake() by EnemyBase.
    /// Cop has high damage output but moderate health compared to other variants.
    /// Uses serialized fields if set, otherwise uses fallback values.
    /// </summary>
    protected override void InitializeStats() {
        maxHealth = defaultMaxHealth > 0 ? defaultMaxHealth : 140f;
        moveSpeed = defaultMoveSpeed > 0 ? defaultMoveSpeed : 3.2f;
        attackDamage = defaultAttackDamage > 0 ? defaultAttackDamage : 22f;
        attackRange = defaultAttackRange > 0 ? defaultAttackRange : 1.9f;
        attackCooldown = defaultAttackCooldown > 0 ? defaultAttackCooldown : 1.3f;
        rewardCurrency = defaultRewardCurrency > 0 ? defaultRewardCurrency : 150;
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
