using UnityEngine;

// RFATORAÇÃO: de onde sai e pra onde vai os stats reais das armas? daqui?

/// <summary>
/// Scriptable Object that defines the data for a vest item, including its resistance value.
/// </summary>
[CreateAssetMenu(fileName = "VestData", menuName = "Deadzone/Vest Data")]
public class VestDataSO : ItemDataSO {

    [Header("Vest Stats")]
    public float resistance = 100f;

    [Header("Vest Upgrade Settings")]
    [Tooltip("Resistance increase per level. 0.25f = +25% per level.")]
    [SerializeField] private float resistanceScaling = 0.25f;

    public override string[] GetStatLabels() => new[] { "Resistance" };
    public override float[] GetStatValues() => new[] { GetResistanceAtLevel(1) };

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
}
