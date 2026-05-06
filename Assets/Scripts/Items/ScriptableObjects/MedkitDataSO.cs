using UnityEngine;

// RFATORAÇÃO: de onde sai e pra onde vai os stats reais das armas? daqui?

/// <summary>
/// Scriptable Object that defines the data for a medkit item, including its heal amount, heal speed, and maximum ammo capacity.
/// </summary>
[CreateAssetMenu(fileName = "MedkitData", menuName = "Deadzone/Medkit Data")]
public class MedkitDataSO : ItemDataSO {
    [Header("Medkit Stats")]
    public float healAmount;
    public float healSpeed;
    public int maxAmount;

    public override string[] GetStatLabels() => new[] { "Heal", "Heal Speed", "Ammo" };

    public override float[] GetStatValues() => new[] { healAmount, healSpeed, maxAmount };

    public override float[] GetStatValues(int level) {
        float levelFactor = 1f + (level - 1) * 0.1f;
        return new[] { 
            healAmount * levelFactor, 
            healSpeed * levelFactor, 
            maxAmount 
        };
    }
}
