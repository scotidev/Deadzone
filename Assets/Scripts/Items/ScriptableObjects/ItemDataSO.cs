using UnityEngine;

/// <summary>
/// Base ScriptableObject for all items.
/// </summary>
public abstract class ItemDataSO : ScriptableObject {

    #region  SERIALIZED FIELDS

    [Header("Item Data")]
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [SerializeField] private int maxUpgradeLevel = 10;
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private int baseAmmo = 10;
    [SerializeField] private float ammoScaling = 0.1f;

    [Tooltip("Maximum quantity that can be carried in hand. FOR WEAPONS: This is the Magazine Capacity.")]
    [SerializeField] private int baseCurrentCapacity = 1;

    [Tooltip("Scaling for current capacity per upgrade level. FOR WEAPONS: This scales the magazine size.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float currentCapacityScaling = 0f;

    #endregion

    #region PROPERTIES

    public string ItemID => itemID;
    public string ItemName => itemName;
    public virtual int MaxUpgradeLevel => maxUpgradeLevel;
    public virtual int MaxAmmo => maxAmmo;
    public int BaseAmmo => baseAmmo;
    public float AmmoScaling => ammoScaling;
    public int BaseCurrentCapacity => baseCurrentCapacity;
    public float CurrentCapacityScaling => currentCapacityScaling;

    #endregion

    #region METHODS

    /// <summary>
    /// Gets an array of strings representing the labels for the statistical values calculated by the implementing class.
    /// </summary>
    public abstract string[] GetStatLabels();

    /// <summary>
    /// Gets an array of statistical values calculated by the implementing class.
    /// </summary>
    public abstract float[] GetStatValues();

    /// <summary>
    /// Gets an array of statistical values for a specific upgrade level.
    /// Used to show current stats and preview stats after upgrade.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>An array of statistical values at the specified level.</returns>
    public abstract float[] GetStatValues(int level);

    /// <summary>
    /// Calculates the maximum ammo/quantity at a given upgrade level.
    /// Formula: min(baseAmmo * (1 + ammoScaling * (level - 1)), MaxAmmo)
    /// </summary>
    public int GetMaxAmmoAtLevel(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        float scaledAmmo = baseAmmo * (1f + ammoScaling * (level - 1));
        return Mathf.Min((int)scaledAmmo, MaxAmmo);
    }

    /// <summary>
    /// Calculates the current capacity (magazine/hand quantity) at a given upgrade level.
    /// Formula: round(baseCurrentCapacity * (1 + currentCapacityScaling * (level - 1)))
    /// </summary>
    public virtual int GetMaxCurrentCapacityAtLevel(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        float scaledCapacity = baseCurrentCapacity * (1f + currentCapacityScaling * (level - 1));
        int capacity = Mathf.RoundToInt(scaledCapacity);
        return Mathf.Min(capacity, GetMaxAmmoAtLevel(level));
    }

    #endregion
}
