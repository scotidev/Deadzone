using UnityEngine;

/// <summary>
/// Utility class that normalizes weapon stats to a 0-5 bar display system.
/// Converts raw stat values from WeaponDataSO into proportional bar fills for UI.
/// </summary>
public class WeaponStatsCalculator
{
    // CONCEITO: Constantes de Normalização
    // Essas constantes definem o "teto" (valor máximo) para cada stat
    // Quando você tem um valor real, divide pelo máximo e multiplica por 5 barras
    // Exemplo: Damage atual = 50, Max = 100, então (50/100)*5 = 2.5 barras
    
    /// <summary>Maximum damage value that fits in 5 bars (each bar = 20 damage).</summary>
    public const float MAX_DAMAGE = 100f;
    
    /// <summary>Maximum fire rate value that fits in 5 bars (each bar = 2 fire rate).</summary>
    public const float MAX_FIRE_RATE = 10f;
    
    /// <summary>Maximum ammo capacity that fits in 5 bars (each bar = 40 ammo).</summary>
    public const float MAX_AMMO_CAPACITY = 200f;

    /// <summary>Number of display bars for stats.</summary>
    public const int STAT_BARS = 5;

    /// <summary>
    /// Converts a raw damage value to normalized bar count (0-5).
    /// Uses clamping to ensure the value never exceeds MAX_DAMAGE.
    /// </summary>
    /// <param name="damageValue">Raw damage value from weapon.</param>
    /// <returns>Normalized value in range 0-5 bars.</returns>
    public static float NormalizeDamage(float damageValue)
    {
        // CONCEITO: Clamping
        // Mathf.Clamp garante que o valor fica entre min e max
        // Previne valores negativos ou absurdamente altos
        float clamped = Mathf.Clamp(damageValue, 0f, MAX_DAMAGE);
        
        // FÓRMULA DE NORMALIZAÇÃO: (valor / máximo) * 5 barras
        // Exemplo: 50 damage com max 100 = (50/100)*5 = 2.5 barras
        float normalized = (clamped / MAX_DAMAGE) * STAT_BARS;
        
        return normalized;
    }

    /// <summary>
    /// Converts a raw fire rate value to normalized bar count (0-5).
    /// Fire rate is clamped to MAX_FIRE_RATE to prevent UI overflow.
    /// </summary>
    /// <param name="fireRateValue">Raw fire rate value from weapon.</param>
    /// <returns>Normalized value in range 0-5 bars.</returns>
    public static float NormalizeFireRate(float fireRateValue)
    {
        // Clamp the fire rate to maximum
        float clamped = Mathf.Clamp(fireRateValue, 0f, MAX_FIRE_RATE);
        
        // Normalize to 5 bars
        float normalized = (clamped / MAX_FIRE_RATE) * STAT_BARS;
        
        return normalized;
    }

    /// <summary>
    /// Converts a raw ammo capacity value to normalized bar count (0-5).
    /// Ammo is clamped to MAX_AMMO_CAPACITY.
    /// </summary>
    /// <param name="ammoValue">Raw ammo capacity value from weapon.</param>
    /// <returns>Normalized value in range 0-5 bars.</returns>
    public static float NormalizeAmmo(float ammoValue)
    {
        // Clamp the ammo to maximum
        float clamped = Mathf.Clamp(ammoValue, 0f, MAX_AMMO_CAPACITY);
        
        // Normalize to 5 bars
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
    public static float CalculateAndNormalizeDamage(WeaponDataSO weaponData, int level)
    {
        // CONCEITO: Guard Clause (Early Return)
        // Se dados estão faltando, retorna 0 imediatamente
        // Isso previne NullReferenceException
        if (weaponData == null) return 0f;
        
        // Get the damage at this level from WeaponDataSO
        float damageAtLevel = weaponData.GetDamageAtLevel(level);
        
        // Normalize to 5 bars
        return NormalizeDamage(damageAtLevel);
    }

    /// <summary>
    /// Calculates the fire rate value for a given upgrade level using WeaponDataSO.
    /// Then normalizes it to bar count (0-5).
    /// </summary>
    /// <param name="weaponData">Reference to the weapon's data asset.</param>
    /// <param name="level">Current upgrade level (1-10).</param>
    /// <returns>Normalized fire rate value in range 0-5 bars.</returns>
    public static float CalculateAndNormalizeFireRate(WeaponDataSO weaponData, int level)
    {
        // Guard clause: if no data, return 0
        if (weaponData == null) return 0f;
        
        // Get the fire rate at this level (in RPM, but we normalize)
        float fireRateAtLevel = weaponData.GetFireRateAtLevel(level);
        
        // Convert RPM to "fire rate units" (divide by 100 to get reasonable values)
        // CONCEITO: Unit Conversion
        // RPM pode ser 200-1000+, mas queremos escala de 0-10 para os bars
        // Dividindo por 100 traz para escala usável
        float fireRateUnits = fireRateAtLevel / 100f;
        
        // Normalize to 5 bars
        return NormalizeFireRate(fireRateUnits);
    }

    /// <summary>
    /// Calculates the ammo capacity for a given upgrade level using WeaponDataSO.
    /// Then normalizes it to bar count (0-5).
    /// </summary>
    /// <param name="weaponData">Reference to the weapon's data asset.</param>
    /// <param name="level">Current upgrade level (1-10).</param>
    /// <returns>Normalized ammo value in range 0-5 bars.</returns>
    public static float CalculateAndNormalizeAmmo(WeaponDataSO weaponData, int level)
    {
        // Guard clause: if no data, return 0
        if (weaponData == null) return 0f;
        
        // Get the magazine capacity at this level
        int ammoAtLevel = weaponData.GetMagazineCapacityAtLevel(level);
        
        // Normalize to 5 bars
        return NormalizeAmmo(ammoAtLevel);
    }
}
