using UnityEngine;

/// <summary>
/// ScriptableObject that defines a weapon's base stats and how they scale with upgrades.
/// Each weapon has one of these assets.
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "Deadzone/Weapon Data")]
public class WeaponDataSO : ItemDataSO {

    #region FIELDS

    [Header("Base Stats")]
    public float baseDamage = 10f;
    public float baseFireRate = 200f;
    public int baseMagazineCapacity = 30;

    [Header("Upgrade Scaling")]

    [Range(0f, 0.5f)]
    public float damageScaling = 0.1f;

    [Range(0f, 0.5f)]
    public float fireRateScaling = 0.05f;

    public float maxFireRate = 500f;

    [Range(0f, 0.5f)]
    public float magazineScaling = 0.1f;

    #endregion

    #region PROPERTIES

    public override int MaxAmmo => 300;

    #endregion

    #region METHODS

    /// <summary>
    /// Calculates the damage stat for a given upgrade level.
    /// Formula: baseDamage × (1 + damageScaling × level)
    /// Example: Level 5 with 10% scaling = base × 1.5 (50% more damage)
    /// </summary>
    /// <param name="level">Current upgrade level.</param>
    /// <returns>The calculated damage value.</returns>
    public float GetDamageAtLevel(int level) {
        level = Mathf.Clamp(level, 1, 10);

        return baseDamage * (1 + damageScaling * level);
    }

    /// <summary>
    /// Calculates the fire rate stat for a given upgrade level.
    /// Higher fire rate = faster shooting.
    /// </summary>
    /// <param name="level">Current upgrade level.</param>
    /// <returns>The calculated fire rate in rounds per minute (capped at maxFireRate).</returns>
    public float GetFireRateAtLevel(int level) {
        level = Mathf.Clamp(level, 1, 10);

        float calculatedFireRate = baseFireRate * (1 + fireRateScaling * level);

        return Mathf.Min(calculatedFireRate, maxFireRate);
    }

    /// <summary>
    /// Calculates the magazine capacity for a given upgrade level.
    /// Rounded to nearest integer since you can't have partial bullets.
    /// </summary>
    /// <param name="level">Current upgrade level.</param>
    /// <returns>The calculated magazine capacity.</returns>
    public int GetMagazineCapacityAtLevel(int level) {
        level = Mathf.Clamp(level, 1, 10);

        float scaledCapacity = baseMagazineCapacity * (1 + magazineScaling * level);
        return Mathf.RoundToInt(scaledCapacity);
    }

    public override string[] GetStatLabels() => new[] { "Damage", "Fire Rate", "Ammo" };

    public override float[] GetStatValues() => GetStatValues(1);

    public override float[] GetStatValues(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return new[] { 
            GetDamageAtLevel(level), 
            GetFireRateAtLevel(level), // Don't divide by 100f - let normalization handle it
            (float)GetMagazineCapacityAtLevel(level)
        };
    }

    #endregion
}
