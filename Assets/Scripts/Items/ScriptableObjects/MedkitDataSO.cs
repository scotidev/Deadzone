using UnityEngine;

/// <summary>
/// Scriptable Object that defines the data for a medkit item, including its heal amount and maximum ammo capacity.
/// </summary>
[CreateAssetMenu(fileName = "MedkitData", menuName = "Deadzone/Medkit Data")]
public class MedkitDataSO : ItemDataSO {

    [Header("Medkit Stats")]
    public float healAmount;
    public int maxAmount;

    [Header("Upgrade Settings")]
    [Tooltip("Heal amount increase per level. 0.1f = +10% per level.")]
    [SerializeField] private float healScaling = 0.1f;

    public override string[] GetStatLabels() => new[] { "Heal", "Ammo" };

    public override float[] GetStatValues() => new[] { healAmount, (float)maxAmount };

    public override float[] GetStatValues(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        float levelFactor = 1f + healScaling * (level - 1);
        return new[] { 
            healAmount * levelFactor, 
            (float)maxAmount 
        };
    }
}
