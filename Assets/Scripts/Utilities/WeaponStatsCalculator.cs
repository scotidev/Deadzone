using UnityEngine;

//ATUALIZAÇÃO NECESSARIA: ter as  barras de HP, radius,resistance, heal etc... para os itens de medkit, vest, grenande, buildables...

/// <summary>
/// Utility class that normalizes weapon stats to a 0-5 bar display system.
/// Converts raw stat values from WeaponDataSO into proportional bar fills for UI.
/// </summary>
public class WeaponStatsCalculator {

    #region CONSTANTS

    /// <summary>Maximum damage value that fits in 5 bars (each bar = 20 damage).</summary>
    public const float MAX_DAMAGE = 100f;

    /// <summary>Maximum fire rate value that fits in 5 bars (each bar = 2 fire rate).</summary>
    public const float MAX_FIRE_RATE = 10f;

    /// <summary>Maximum ammo capacity that fits in 5 bars (each bar = 40 ammo).</summary>
    public const float MAX_AMMO_CAPACITY = 200f;

    /// <summary>Number of display bars for stats.</summary>
    public const int STAT_BARS = 5;

    #endregion

    #region METHODS

    /// <summary>
    /// Converts a raw damage value to normalized bar count (0-5).
    /// Uses clamping to ensure the value never exceeds MAX_DAMAGE.
    /// </summary>
    /// <param name="damageValue">Raw damage value from weapon.</param>
    /// <returns>Normalized value in range 0-5 bars.</returns>
    public static float NormalizeDamage(float damageValue) {
        float clamped = Mathf.Clamp(damageValue, 0f, MAX_DAMAGE);

        float normalized = (clamped / MAX_DAMAGE) * STAT_BARS;

        return normalized;
    }

    /// <summary>
    /// Converts a raw fire rate value to normalized bar count (0-5).
    /// Fire rate is clamped to MAX_FIRE_RATE to prevent UI overflow.
    /// </summary>
    /// <param name="fireRateValue">Raw fire rate value from weapon.</param>
    /// <returns>Normalized value in range 0-5 bars.</returns>
    public static float NormalizeFireRate(float fireRateValue) {
        float clamped = Mathf.Clamp(fireRateValue, 0f, MAX_FIRE_RATE);

        float normalized = (clamped / MAX_FIRE_RATE) * STAT_BARS;

        return normalized;
    }

    /// <summary>
    /// Converts a raw ammo capacity value to normalized bar count (0-5).
    /// Ammo is clamped to MAX_AMMO_CAPACITY.
    /// </summary>
    /// <param name="ammoValue">Raw ammo capacity value from weapon.</param>
    /// <returns>Normalized value in range 0-5 bars.</returns>
    public static float NormalizeAmmo(float ammoValue) {
        float clamped = Mathf.Clamp(ammoValue, 0f, MAX_AMMO_CAPACITY);

        float normalized = (clamped / MAX_AMMO_CAPACITY) * STAT_BARS;

        return normalized;
    }

    /// <summary>
    /// Calculates the damage value for a given upgrade level using WeaponDataSO.
    /// Then normalizes it to bar count (0-5).
    /// </summary>
    /// <param name="weaponData">Reference to the weapon's data asset.</param>
    /// <param name="level">Current upgrade level (1-10).</param>
    /// <returns>Normalized damage value in range 0-5 bars.</returns>
    public static float CalculateAndNormalizeDamage(WeaponDataSO weaponData, int level) {
        if (weaponData == null) return 0f;

        float damageAtLevel = weaponData.GetDamageAtLevel(level);

        return NormalizeDamage(damageAtLevel);
    }

    /// <summary>
    /// Calculates the fire rate value for a given upgrade level using WeaponDataSO.
    /// Then normalizes it to bar count (0-5).
    /// </summary>
    /// <param name="weaponData">Reference to the weapon's data asset.</param>
    /// <param name="level">Current upgrade level (1-10).</param>
    /// <returns>Normalized fire rate value in range 0-5 bars.</returns>
    public static float CalculateAndNormalizeFireRate(WeaponDataSO weaponData, int level) {
        if (weaponData == null) return 0f;

        float fireRateAtLevel = weaponData.GetFireRateAtLevel(level);

        float fireRateUnits = fireRateAtLevel / 100f;

        return NormalizeFireRate(fireRateUnits);
    }

    /// <summary>
    /// Calculates the ammo capacity for a given upgrade level using WeaponDataSO.
    /// Then normalizes it to bar count (0-5).
    /// </summary>
    /// <param name="weaponData">Reference to the weapon's data asset.</param>
    /// <param name="level">Current upgrade level (1-10).</param>
    /// <returns>Normalized ammo value in range 0-5 bars.</returns>
    public static float CalculateAndNormalizeAmmo(WeaponDataSO weaponData, int level) {
        if (weaponData == null) return 0f;

        int ammoAtLevel = weaponData.GetMagazineCapacityAtLevel(level);

        return NormalizeAmmo(ammoAtLevel);
    }

    #endregion
}
