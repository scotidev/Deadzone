using UnityEngine;

/// <summary>
/// ScriptableObject that defines a weapon's base stats and how they scale with upgrades.
/// Each weapon has one of these assets.
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "Deadzone/Weapon Data")]
public class WeaponDataSO : ItemDataSO {

    [Header("Base Stats")]
    public float baseDamage = 10f;

    [Tooltip("Base fire rate in rounds per minute.")]
    public float baseFireRate = 200f;

    [Tooltip("Base magazine capacity (ammo per clip).")]
    public int baseMagazineCapacity = 30;

    [Tooltip("Maximum reserve ammo the player can carry for this weapon.")]
    public int maxReserveAmmo = 300;

    [Header("Upgrade Scaling")]
    [Tooltip("Percentage increase in damage per upgrade level (e.g., 0.1 = +10% per level).")]
    [Range(0f, 0.5f)]
    public float damageScaling = 0.1f;

    [Tooltip("Percentage increase in fire rate per upgrade level.")]
    [Range(0f, 0.5f)]
    public float fireRateScaling = 0.05f;

    [Tooltip("Maximum fire rate cap (prevents fire rate from going too high with upgrades).")]
    public float maxFireRate = 500f;

    [Tooltip("Percentage increase in magazine capacity per upgrade level.")]
    [Range(0f, 0.5f)]
    public float magazineScaling = 0.1f;

    [Header("Exclusive Power")]
    [Tooltip("Reference to the exclusive power script for this weapon at max level.")]
    public string exclusivePowerID;

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

    // FAZER LÓGICA DINÂMICA PARA QUE O NÍVEL NAO PRECISE SER 10 NECESSARIAMENTE, MAS SIM O NÍVEL MÁXIMO DEFINIDO PARA A ARMA, PARA QUE SEJA MAIS FLEXÍVEL PARA FUTURAS ARMAS COM NÍVEIS MÁXIMOS DIFERENTES
    /// <summary>
    /// Checks if this weapon has reached maximum level and unlocked its exclusive power.
    /// </summary>
    /// <param name="level">Current upgrade level.</param>
    /// <returns>True if level at exclusive power and exclusive power exists.</returns>
    public bool HasExclusivePower(int level) {
        return level >= 10 && !string.IsNullOrEmpty(exclusivePowerID);
    }

    public override string[] GetStatLabels() => new[] { "Damage", "Fire Rate", "Ammo" };
    public override float[] GetStatValues() => new[] { baseDamage, baseFireRate, baseMagazineCapacity };
}
