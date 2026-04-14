using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildable", menuName = "Deadzone/Buildable Item")]

///<summary> 
/// ScriptableObject that represents a buildable item in the game.
///</summary>
public class BuildableDataSO : ItemDataSO {
    [Header("Buildable Stats")]
    public float resistance;
    public float length;
    public int maxAmount;

    [Header("Prefabs")]

    public GameObject realPrefab;
    public GameObject ghostPrefab;

    [Header("Placement Rotation")]

    [Tooltip("Rotation applied, in degrees, to correct the model orientation when placed in the scene.")]
    public Vector3 placementRotationEuler = Vector3.zero;

    [Header("Space Check Size")]

    [Tooltip("Size of the box used to check for overlapping objects when placing the buildable item. Adjust this to ensure proper placement and avoid collisions with other objects.")]
    public Vector3 overlapBoxSize = new Vector3(1f, 1f, 1f);

    // Implement stat labels/values for shop UI (example for Barricade)
    public override string[] GetStatLabels() => new[] { "Resistance", "Length", "Ammo" };
    public override float[] GetStatValues() => new[] { resistance, length, maxAmount };
}
