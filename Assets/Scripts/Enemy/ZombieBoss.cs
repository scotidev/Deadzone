using UnityEngine;

/// <summary>
/// Boss zombie enemy type. Much stronger and tankier than regular zombies.
/// Immune to crowd control effects like stuns from traps.
/// </summary>
public class ZombieBoss : EnemyBase {

    #region METHODS

    /// <summary>
    /// Initialize boss stats. Called during Awake() by EnemyBase.
    /// Boss has significantly higher health, damage, and attack speed than other zombie types.
    /// </summary>
    protected override void InitializeStats() {
        maxHealth = 300f;      // Much higher health than regular zombies
        moveSpeed = 3.0f;      // Slightly slower than fast zombies
        attackDamage = 25f;    // Deals more damage than regular zombies
        attackRange = 2.0f;    // Larger attack range
        attackCooldown = 1.2f; // Faster attack cooldown
    }

    #endregion
}
