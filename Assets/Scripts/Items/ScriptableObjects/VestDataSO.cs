using UnityEngine;

/// <summary>
/// Scriptable Object that defines the data for a vest item, including its resistance value.
/// </summary>
[CreateAssetMenu(fileName = "VestData", menuName = "Deadzone/Vest Data")]
public class VestDataSO : ItemDataSO {

    #region SERIALIZED FIELDS

    [Header("Vest Stats")]
    [SerializeField] private float resistance = 100f;
    [SerializeField] private float resistanceScaling = 0.25f;

    #endregion

    #region PROPERTIES

    public float Resistance => resistance;

    #endregion

    #region OVERRIDES

    public override string[] GetStatLabels() => new[] { "Resistance" };

    public override float[] GetStatValues() => GetStatValues(1);

    public override float[] GetStatValues(int level) => new[] { GetResistanceAtLevel(level) };

    #endregion

    #region METHODS

    /// <summary>
    /// Gets the resistance value at a specific upgrade level.
    /// Level 1 = base resistance, each level adds resistanceScaling percentage.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>The resistance value at the given level.</returns>
    public float GetResistanceAtLevel(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return resistance * (1f + resistanceScaling * (level - 1));
    }

    #endregion
}
