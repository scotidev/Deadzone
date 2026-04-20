using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildable", menuName = "Deadzone/Buildable Item")]

///<summary> 
/// ScriptableObject that represents a buildable item in the game.
///</summary>
public class BuildableDataSO : ItemDataSO {

    #region SERIALIZED FIELDS

    [Header("Buildable Stats")]
    [SerializeField] private int maxAmount;
    [SerializeField] private float damage;
    [SerializeField] private int explosionRadius;
    [SerializeField] private float health = 100f;
    [SerializeField] private float length;

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
    public float Health => health;
    public float Length => length;
    public float MaxAmount => maxAmount;
    public GameObject RealPrefab => realPrefab;
    public GameObject GhostPrefab => ghostPrefab;
    public Vector3 PlacementRotationEuler => placementRotationEuler;
    public Vector3 OverlapBoxSize => overlapBoxSize;

    #endregion

    #region METHODS

    public override string[] GetStatLabels() => new[] { "Damage", "Health", "Length", "Ammo", "Explosion Radius" };
    public override float[] GetStatValues() => new[] { damage, health, length, maxAmount, explosionRadius };

    #endregion

}
