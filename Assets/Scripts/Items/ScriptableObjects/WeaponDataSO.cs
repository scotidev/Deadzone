using UnityEngine;

/// <summary>
/// ScriptableObject that defines a weapon's base stats and how they scale with upgrades.
/// Each weapon has one of these assets.
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "Deadzone/Weapon Data")]
public class WeaponDataSO : ItemDataSO {

    #region FIELDS

    [Header("Base Stats")]
    public bool isAutomatic = true;
    public float baseDamage = 10f;
    public float baseFireRate = 200f;

    [Header("Upgrade Scaling")]

    [Range(0f, 0.5f)]
    public float damageScaling = 0.1f;

    [Range(0f, 0.5f)]
    public float fireRateScaling = 0.05f;

    #endregion

    #region PROPERTIES

    #endregion

    #region METHODS

    /// <summary>
    /// Calculates the damage stat for a given upgrade level.
    /// Formula: baseDamage * (1 + damageScaling * (level - 1))
    /// </summary>
    public float GetDamageAtLevel(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return baseDamage * (1 + damageScaling * (level - 1));
    }

    /// <summary>
    /// Calculates the fire rate stat for a given upgrade level.
    /// Higher fire rate = faster shooting.
    /// </summary>
    public float GetFireRateAtLevel(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return baseFireRate * (1 + fireRateScaling * (level - 1));
    }

    public override string[] GetStatLabels() => new[] { "Damage", "Fire Rate", "Ammo" };

    public override float[] GetStatValues() => GetStatValues(1);

    public override float[] GetStatValues(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return new[] { 
            GetDamageAtLevel(level), 
            GetFireRateAtLevel(level),
            (float)GetMaxAmmoAtLevel(level)
        };
    }

    #endregion
}
