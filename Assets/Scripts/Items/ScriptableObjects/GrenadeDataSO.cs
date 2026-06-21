using UnityEngine;

/// <summary>
/// Scriptable Object that defines the data for a grenade item, including its damage, explosion radius, and maximum ammo capacity.
/// </summary>
[CreateAssetMenu(fileName = "GrenadeData", menuName = "Deadzone/Grenade Data")]
public class GrenadeDataSO : ItemDataSO {
    [Header("Grenade Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float radius;

    [Header("Upgrade Scaling")]
    [Tooltip("Damage increase per level. 0.1f = +10% per level.")]
    [SerializeField] private float damageScaling = 0.1f;
    [Tooltip("Radius increase per level. 0.1f = +10% per level.")]
    [SerializeField] private float radiusScaling = 0.1f;

    #region PROPERTIES

    public float Damage => damage;
    public float Radius => radius;

    #endregion

    #region METHODS

    /// <summary>
    /// Gets the scaled damage value at the specified upgrade level.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>The damage value scaled by damageScaling per level.</returns>
    public float GetDamageAtLevel(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return damage * (1f + damageScaling * (level - 1));
    }

    /// <summary>
    /// Gets the scaled radius value at the specified upgrade level.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>The radius value scaled by radiusScaling per level.</returns>
    public float GetRadiusAtLevel(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return radius * (1f + radiusScaling * (level - 1));
    }

    #endregion

    public override string[] GetStatLabels() => new[] { "Damage", "Radius", "Ammo" };

    public override float[] GetStatValues() => new[] { damage, radius, (float)MaxAmmo };

    public override float[] GetStatValues(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return new[] { 
            GetDamageAtLevel(level), 
            GetRadiusAtLevel(level), 
            (float)GetMaxAmmoAtLevel(level) 
        };
    }
}
