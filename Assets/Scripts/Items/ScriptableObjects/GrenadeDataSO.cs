using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeData", menuName = "Deadzone/Grenade Data")]
public class GrenadeDataSO : ItemDataSO {
    [Header("Grenade Stats")]
    public float damage;
    public float radius;
    public int maxAmount;

    public override string[] GetStatLabels() => new[] { "Damage", "Radius", "Ammo" };
    public override float[] GetStatValues() => new[] { damage, radius, maxAmount };
}
