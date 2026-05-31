using UnityEngine;

/// <summary>
/// Boss zombie enemy type. Much stronger and tankier than regular zombies.
/// Immune to crowd control effects like stuns from traps.
/// </summary>
public class ZombieBoss : EnemyBase {

    [Header("Boss Zombie Stats")]
    [SerializeField] private float defaultMaxHealth = 300f;
    [SerializeField] private float defaultMoveSpeed = 3.0f;
    [SerializeField] private float defaultAttackDamage = 25f;
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

    [Header("Boss Zombie Reward")]
    [SerializeField] private int defaultRewardCurrency = 250;

    /// <summary>
    /// Initialize boss stats. Called during Awake() by EnemyBase.
    /// Boss has significantly higher health, damage, and attack speed than other zombie types.
    /// Uses serialized fields if set, otherwise uses fallback values.
    /// </summary>
    protected override void InitializeStats() {
        maxHealth = defaultMaxHealth > 0 ? defaultMaxHealth : 300f;
        moveSpeed = defaultMoveSpeed > 0 ? defaultMoveSpeed : 3.0f;
        attackDamage = defaultAttackDamage > 0 ? defaultAttackDamage : 25f;
        attackRange = defaultAttackRange > 0 ? defaultAttackRange : 2.0f;
        attackCooldown = defaultAttackCooldown > 0 ? defaultAttackCooldown : 1.2f;
        rewardCurrency = defaultRewardCurrency > 0 ? defaultRewardCurrency : 250;
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
