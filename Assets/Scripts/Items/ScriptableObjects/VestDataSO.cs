using UnityEngine;

[CreateAssetMenu(fileName = "VestData", menuName = "Deadzone/Vest Data")]
public class VestDataSO : ItemDataSO {
    [Header("Vest Stats")]
    public float resistance;

    public override string[] GetStatLabels() => new[] { "Resistance" };
    public override float[] GetStatValues() => new[] { resistance };
}
