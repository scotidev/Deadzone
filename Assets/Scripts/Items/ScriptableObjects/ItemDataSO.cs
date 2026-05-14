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

    [Tooltip("Maximum quantity that can be carried in hand. For weapons: magazine capacity. For consumables/buildables: 1 (always)")]
    [SerializeField] private int baseCurrentCapacity = 1;

    [Tooltip("Scaling for current capacity per upgrade level. Only applies to weapons (for magazine expansion). 0 for consumables.")]
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
    /// <returns>An array of strings representing the labels for the statistical values. The array may be empty if no labels are available.</returns>
    public abstract string[] GetStatLabels();

    /// <summary>
    /// Gets an array of statistical values calculated by the implementing class.
    /// </summary>
    /// <remarks>The specific statistical values returned depend on the implementation of this method in
    /// derived classes. Ensure to check the array length before accessing its elements.</remarks>
    /// <returns>An array of floating-point numbers representing the calculated statistical values. The array may be empty if no values are available.</returns>
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
    /// This ensures that ammo scaling never exceeds the MaxAmmo cap.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>The maximum ammo/quantity capped by MaxAmmo.</returns>
    public int GetMaxAmmoAtLevel(int level) {
        // Clamp level to valid range to prevent edge cases
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);

        // Apply scaling formula: baseAmmo * (1 + ammoScaling * (level - 1))
        // This gives: level 1 = baseAmmo, level 2 = baseAmmo * (1 + ammoScaling), etc.
        float scaledAmmo = baseAmmo * (1f + ammoScaling * (level - 1));

        // Cap by MaxAmmo and convert to int
        return Mathf.Min((int)scaledAmmo, MaxAmmo);
    }

    /// <summary>
    /// Calculates the current capacity (magazine/hand quantity) at a given upgrade level.
    /// Formula: round(baseCurrentCapacity * (1 + currentCapacityScaling * (level - 1)))
    /// For consumables/buildables, this is always 1. For weapons, this scales the magazine.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>The current capacity (capped sensibly based on total ammo).</returns>
    public int GetMaxCurrentCapacityAtLevel(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);

        // Apply scaling formula: baseCurrentCapacity * (1 + currentCapacityScaling * (level - 1))
        float scaledCapacity = baseCurrentCapacity * (1f + currentCapacityScaling * (level - 1));

        // Convert to int and ensure it never exceeds the total ammo available
        int capacity = Mathf.RoundToInt(scaledCapacity);
        return Mathf.Min(capacity, GetMaxAmmoAtLevel(level));
    }

    #endregion
}
