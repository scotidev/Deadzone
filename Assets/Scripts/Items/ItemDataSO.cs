using UnityEngine;

/// <summary>
/// Base ScriptableObject for all items.
/// </summary>
public abstract class ItemDataSO : ScriptableObject {
    [Header("Identification")]
    public string itemID;
    public string itemName;

    /// <summary>
    /// Returns the stat display values for the shop UI.
    /// Override in derived classes to provide relevant stats.
    /// </summary>
    public abstract string[] GetStatLabels();
    public abstract float[] GetStatValues();
}
