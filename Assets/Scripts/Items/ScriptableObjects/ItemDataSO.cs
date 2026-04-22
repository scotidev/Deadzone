using UnityEngine;

// RFATORAÇÃO: de onde sai e pra onde vai os stats reais das armas? daqui?

/// <summary>
/// Base ScriptableObject for all items.
/// </summary>
public abstract class ItemDataSO : ScriptableObject {

    [Header("Identification")]
    public string itemID;
    public string itemName;

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
}
