using System;
using UnityEngine;

/// <summary>
/// Manages the player's armor (vest). Acts as a shield that absorbs damage before health.
/// When the player has active armor, damage is applied to armor first. Once armor reaches zero,
/// damage passes through to health.
/// </summary>
public class PlayerArmor : MonoBehaviour {

    #region FIELDS

    [Header("Armor Settings")]
    [SerializeField] private float maxArmor = 100f;

    private float currentArmor;

    public event Action<float> OnArmorChanged;
    public event Action OnArmorDepleted;

    #endregion

    #region UNITY

    private void Awake() {
        currentArmor = maxArmor;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Absorbs damage from the armor. Returns the remaining damage that wasn't absorbed.
    /// If armor absorbs all damage, returns 0. If armor is depleted, returns the overflow damage.
    /// </summary>
    /// <param name="incomingDamage">The amount of damage to absorb</param>
    /// <returns>The amount of damage that wasn't absorbed by armor</returns>
    public float AbsorbDamage(float incomingDamage) {
        if (currentArmor <= 0f) {
            return incomingDamage;
        }

        float absorbedDamage = Mathf.Min(currentArmor, incomingDamage);

        currentArmor -= absorbedDamage;

        float remainingDamage = incomingDamage - absorbedDamage;

        OnArmorChanged?.Invoke(currentArmor / maxArmor);

        if (currentArmor <= 0f) {
            currentArmor = 0f;
            OnArmorDepleted?.Invoke();
        }

        return remainingDamage;
    }

    /// <summary>
    /// Adds armor points without exceeding maxArmor.
    /// Can be called when purchasing armor from the shop.
    /// </summary>
    public void AddArmor(float amount) {
        currentArmor = Mathf.Min(maxArmor, currentArmor + amount);

        OnArmorChanged?.Invoke(currentArmor / maxArmor);
    }

    /// <summary>
    /// Repairs armor by the specified amount, without exceeding maxArmor.
    /// </summary>
    public void RepairArmor(float amount) {
        currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
    }

    /// <summary>
    /// Returns the current armor as a fraction between 0 and 1.
    /// </summary>
    public float GetArmorFraction() => currentArmor / maxArmor;

    /// <summary>
    /// Returns the current armor value.
    /// </summary>
    public float GetCurrentArmor() => currentArmor;

    /// <summary>
    /// Returns the maximum armor value.
    /// </summary>
    public float GetMaxArmor() => maxArmor;

    /// <summary>
    /// Checks if the player currently has any armor.
    /// </summary>
    public bool HasArmor() => currentArmor > 0f;

    #endregion
}
