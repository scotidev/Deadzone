using UnityEngine;

// RFATORAÇÃO: de onde sai e pra onde vai os stats reais das armas? daqui?

/// <summary>
/// Scriptable Object that defines the data for a grenade item, including its damage, explosion radius, and maximum ammo capacity.
/// </summary>
[CreateAssetMenu(fileName = "GrenadeData", menuName = "Deadzone/Grenade Data")]
public class GrenadeDataSO : ItemDataSO {
    [Header("Grenade Stats")]
    public float damage;
    public float radius;
    public int maxAmount;

    public override string[] GetStatLabels() => new[] { "Damage", "Radius", "Ammo" };

    public override float[] GetStatValues() => new[] { damage, radius, maxAmount };

    public override float[] GetStatValues(int level) {
        float levelFactor = 1f + (level - 1) * 0.1f;
        return new[] { 
            damage * levelFactor, 
            radius * levelFactor, 
            maxAmount 
        };
    }
}
