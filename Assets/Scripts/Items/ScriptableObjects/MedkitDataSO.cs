using UnityEngine;

/// <summary>
/// Scriptable Object that defines the data for a medkit item, including its heal amount and maximum ammo capacity.
/// </summary>
[CreateAssetMenu(fileName = "MedkitData", menuName = "Deadzone/Medkit Data")]
public class MedkitDataSO : ItemDataSO {

    [Header("Medkit Stats")]
    public float healAmount;

    [Header("Upgrade Settings")]
    [Tooltip("Multiplicador de cura por nível. 2f = dobra a cada nível.")]
    [SerializeField] private float levelMultiplier = 2f;

    #region PROPERTIES

    #endregion

    public override string[] GetStatLabels() => new[] { "Heal", "Ammo" };

    public override float[] GetStatValues() => new[] { healAmount, (float)MaxAmmo };

    /// <summary>
    /// Gets the stat values at a specific upgrade level. Heal amount scales exponentially with levelMultiplier.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>An array containing the heal amount and max ammo at the specified level.</returns>
    public override float[] GetStatValues(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return new[] { 
            healAmount * Mathf.Pow(levelMultiplier, level - 1), 
            (float)GetMaxAmmoAtLevel(level) 
        };
    }
}
