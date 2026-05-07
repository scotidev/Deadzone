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

    #endregion

    #region PROPERTIES

    public string ItemID => itemID;
    public string ItemName => itemName;
    public virtual int MaxUpgradeLevel => maxUpgradeLevel;
    public virtual int MaxAmmo => maxAmmo;

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

    #endregion
}
