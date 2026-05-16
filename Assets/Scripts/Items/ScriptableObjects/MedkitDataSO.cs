using UnityEngine;

/// <summary>
/// Scriptable Object that defines the data for a medkit item, including its heal amount and maximum ammo capacity.
/// </summary>
[CreateAssetMenu(fileName = "MedkitData", menuName = "Deadzone/Medkit Data")]
public class MedkitDataSO : ItemDataSO {

    [Header("Medkit Stats")]
    public float healAmount;

    [Header("Upgrade Settings")]
    [Tooltip("Multiplicador de cura por nível. 2f = dobra a cada nível.")]
    [SerializeField] private float levelMultiplier = 2f;

    #region PROPERTIES

    // MaxAmmo is inherited from ItemDataSO, allowing it to be configured per medkit in the Inspector

    #endregion

    public override string[] GetStatLabels() => new[] { "Heal", "Ammo" };

    public override float[] GetStatValues() => new[] { healAmount, (float)MaxAmmo };

    public override float[] GetStatValues(int level) {
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        return new[] { 
            healAmount * Mathf.Pow(levelMultiplier, level - 1), 
            (float)GetMaxAmmoAtLevel(level) 
        };
    }
}
