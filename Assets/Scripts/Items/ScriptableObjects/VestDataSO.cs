using UnityEngine;

// RFATORAÇÃO: de onde sai e pra onde vai os stats reais das armas? daqui?

/// <summary>
/// Scriptable Object that defines the data for a vest item, including its resistance value.
/// </summary>
[CreateAssetMenu(fileName = "VestData", menuName = "Deadzone/Vest Data")]
public class VestDataSO : ItemDataSO {
    [Header("Vest Stats")]
    public float resistance;

    public override string[] GetStatLabels() => new[] { "Resistance" };
    public override float[] GetStatValues() => new[] { resistance };
}
