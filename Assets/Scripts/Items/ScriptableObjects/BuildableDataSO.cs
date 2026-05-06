using UnityEngine;

// REFATORAÇÃO: maxAmount deve ser unificado em um único contador para armas e  buildables e medkits, granadas... qualquer item deve conter um contador de quantidade chamado de ammoAmount, e cada item tem um currentAmount que é atualizado quando o item é usado ou recarregado. Isso simplifica a lógica de gerenciamento de quantidade e evita confusão entre diferentes tipos de itens.

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
    [SerializeField] private float resistance = 100f;

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
    public float Resistance => resistance;
    public float MaxAmount => maxAmount;
    public GameObject RealPrefab => realPrefab;
    public GameObject GhostPrefab => ghostPrefab;
    public Vector3 PlacementRotationEuler => placementRotationEuler;
    public Vector3 OverlapBoxSize => overlapBoxSize;

    #endregion

    #region METHODS

    public override string[] GetStatLabels() => new[] { "Damage", "Resistance", "Ammo", "Explosion Radius" };

    public override float[] GetStatValues() => new[] { damage, resistance, maxAmount, explosionRadius };

    public override float[] GetStatValues(int level) {
        float levelFactor = 1f + (level - 1) * 0.1f;
        return new[] { 
            damage * levelFactor, 
            resistance * levelFactor, 
            maxAmount, 
            explosionRadius 
        };
    }

    #endregion

}
