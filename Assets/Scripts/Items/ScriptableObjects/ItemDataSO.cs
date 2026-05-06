using UnityEngine;

// RFATORAÇÃO: de onde sai e pra onde vai os stats reais das armas? daqui?

/// <summary>
/// Base ScriptableObject for all items.
/// </summary>
public abstract class ItemDataSO : ScriptableObject {

    [Header("Identification")]
    public string itemID;
    public string itemName;

    [Header("Upgrade Settings")]
    [Tooltip("Maximum upgrade level for this item. Default is 10 for weapons.")]
    [SerializeField] private int maxUpgradeLevel = 10;

    /// <summary>
    /// Gets the maximum upgrade level for this item. Can be overridden by derived classes.
    /// </summary>
    public virtual int MaxUpgradeLevel => maxUpgradeLevel;

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
    /// <returns>An array of floating-point numbers representing the calculated statistical values. The array may be empty if no
    /// values are available.</returns>
    public abstract float[] GetStatValues();

    /// <summary>
    /// Gets an array of statistical values for a specific upgrade level.
    /// Used to show current stats and preview stats after upgrade.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>An array of statistical values at the specified level.</returns>
    public abstract float[] GetStatValues(int level);
}
