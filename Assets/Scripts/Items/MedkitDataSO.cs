using UnityEngine;

[CreateAssetMenu(fileName = "MedkitData", menuName = "Deadzone/Medkit Data")]
public class MedkitDataSO : ItemDataSO {
    [Header("Medkit Stats")]
    public float healAmount;
    public float healSpeed;
    public int maxAmount;

    public override string[] GetStatLabels() => new[] { "Heal", "Heal Speed", "Ammo" };
    public override float[] GetStatValues() => new[] { healAmount, healSpeed, maxAmount };
}
