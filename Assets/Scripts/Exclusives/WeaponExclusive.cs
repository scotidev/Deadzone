using InfimaGames.LowPolyShooterPack;
using UnityEngine;

/// <summary>
/// Abstract class representing an exclusive upgrade for a weapon. Inherits from ItemExclusive and provides a reference to the WeaponBehaviour component, allowing derived classes to implement specific exclusive effects related to weapons.
/// </summary>
public abstract class WeaponExclusive : ItemExclusive {
    protected WeaponBehaviour weaponBehaviour;

    protected override void Awake() {
        base.Awake();

        weaponBehaviour = GetComponent<WeaponBehaviour>();
    }

    protected override void ApplyExclusiveEffects() {
        if (weaponBehaviour != null) {
            Debug.Log("Applying generic weapon exclusive effects.");
        }
    }

    /// <summary>
    /// Configures the exclusive mode by setting the unlock level and description. 
    /// </summary>
    /// <param name="level">The unlock level to assign. Must be a non-negative integer.</param>
    /// <param name="description">The description to associate with the exclusive mode. Cannot be null.</param>
    public void SetupExclusive(int level, string description) {
        SetUnlockLevel(level);
        SetExclusiveDescription(description);
    }
}
