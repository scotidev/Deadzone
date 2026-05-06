using UnityEngine;

// REFATORAÇÃO: maxReserveAmmo respeita maxAmount ou está de acordo com o que foi dito no script de PlayerProgress? Ou deveriamos ter maxReserveAmmo para armas e maxAmount para itens de progresso? Mais uma coisa, é necessario referenciar o ID de exclusivo? o proprio script de exclusivo colocado na arma já não seria o suficiente para saber que aquela arma tem um exclusivo?

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
    public int maxReserveAmmo = 300;
    [Range(0f, 100f)] public float baseCritChance = 5f;

    [Header("Upgrade Scaling")]

    [Range(0f, 0.5f)]
    public float damageScaling = 0.1f;

    [Range(0f, 0.5f)]
    public float fireRateScaling = 0.05f;

    public float maxFireRate = 500f;

    [Range(0f, 0.5f)]
    public float magazineScaling = 0.1f;

    [Range(0f, 0.2f)]
    public float critChanceScaling = 0.02f;

    [Header("Exclusive Power")]

    public string exclusivePowerID;

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

    public float GetCritChanceAtLevel(int level) {
        level = Mathf.Clamp(level, 1, 10);
        return Mathf.Clamp(baseCritChance + (critChanceScaling * level * 10f), 0f, 100f);
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

    public override string[] GetStatLabels() => new[] { "Damage", "Fire Rate", "Ammo", "Crit" };

    public override float[] GetStatValues() => GetStatValues(1);

    public override float[] GetStatValues(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return new[] { 
            GetDamageAtLevel(level), 
            GetFireRateAtLevel(level) / 100f, 
            (float)GetMagazineCapacityAtLevel(level),
            GetCritChanceAtLevel(level)
        };
    }

    #endregion
}
