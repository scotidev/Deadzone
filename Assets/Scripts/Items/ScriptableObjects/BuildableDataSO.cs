using UnityEngine;

public enum BuildableStatType {
    Damage,
    Resistance,
    Ammo,
    Radius
}

[CreateAssetMenu(fileName = "NewBuildable", menuName = "Deadzone/Buildable Item")]
/// <summary> 
/// ScriptableObject that represents a buildable item in the game.
/// </summary>
public class BuildableDataSO : ItemDataSO {

    #region SERIALIZED FIELDS

    [Header("Buildable Stats")]
    [SerializeField] private int maxAmount;
    [SerializeField] private float damage;
    [SerializeField] private int explosionRadius;
    [Tooltip("Renamed from 'health' to 'resistance'")]
    [SerializeField] private float health = 100f;

    [Header("Stats Display")]
    [Tooltip("Select which stats to display in the shop UI for this buildable.")]
    [SerializeField] private BuildableStatType[] displayStats;

    [Header("Upgrade Scaling")]
    [Tooltip("Damage increase per level. 0.1f = +10% per level.")]
    [SerializeField] private float damageScaling = 0.1f;
    [Tooltip("Resistance increase per level. 0.1f = +10% per level.")]
    [SerializeField] private float resistanceScaling = 0.1f;

    [Header("Prefabs")]
    [SerializeField] private GameObject realPrefab;
    [SerializeField] private GameObject ghostPrefab;

    [Header("Placement Rotation")]
    [Tooltip("Rotation applied, in degrees, to correct the model orientation when placed in the scene.")]
    [SerializeField] private Vector3 placementRotationEuler = Vector3.zero;

    [Header("Space Check Size")]
    [Tooltip("Size of the box used to check for overlapping objects when placing the buildable item. Adjust this to ensure proper placement and avoid collisions with other objects.")]
    [SerializeField] private Vector3 overlapBoxSize = new Vector3(1f, 1f, 1f);

    #endregion

    #region PROPERTIES

    public float Damage => damage;
    public int ExplosionRadius => explosionRadius;
    public float Resistance => health;
    public int MaxAmount => maxAmount;
    public GameObject RealPrefab => realPrefab;
    public GameObject GhostPrefab => ghostPrefab;
    public Vector3 PlacementRotationEuler => placementRotationEuler;
    public Vector3 OverlapBoxSize => overlapBoxSize;
    public BuildableStatType[] DisplayStats => displayStats;

    #endregion

    #region METHODS

    private string GetStatLabel(BuildableStatType stat) {
        switch (stat) {
            case BuildableStatType.Damage: return "Damage";
            case BuildableStatType.Resistance: return "Resistance";
            case BuildableStatType.Ammo: return "Ammo";
            case BuildableStatType.Radius: return "Radius";
            default: return "Unknown";
        }
    }

    public override string[] GetStatLabels() {
        if (displayStats == null || displayStats.Length == 0) {
            return new[] { "Ammo" };
        }
        
        string[] labels = new string[displayStats.Length];
        for (int i = 0; i < displayStats.Length; i++) {
            labels[i] = GetStatLabel(displayStats[i]);
        }
        return labels;
    }

    public override float[] GetStatValues() => GetStatValues(1);

    public override float[] GetStatValues(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        float levelFactor = 1f + (level - 1) * 0.1f;

        if (displayStats == null || displayStats.Length == 0) {
            return new[] { (float)maxAmount };
        }

        float[] values = new float[displayStats.Length];
        
        for (int i = 0; i < displayStats.Length; i++) {
            switch (displayStats[i]) {
                case BuildableStatType.Damage:
                    values[i] = damage * (1f + damageScaling * (level - 1));
                    break;
                case BuildableStatType.Resistance:
                    values[i] = health * (1f + resistanceScaling * (level - 1));
                    break;
                case BuildableStatType.Ammo:
                    values[i] = maxAmount;
                    break;
                case BuildableStatType.Radius:
                    values[i] = explosionRadius;
                    break;
            }
        }
        
        return values;
    }

    #endregion

}
