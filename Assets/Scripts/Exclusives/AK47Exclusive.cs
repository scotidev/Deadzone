using UnityEngine;

/// <summary>
/// This class implements the exclusive upgrade for the AK47. 
/// </summary>
public class AK47Exclusive : WeaponExclusive {

    [Header("AK47 Exclusive Settings")]
    [SerializeField] private int maxMagazineCapacity = 100;

    protected override void Awake() {
        base.Awake();
        SetupExclusive(9, "Magazine Capacity increased to 100");
    }

    protected override void ApplyExclusiveEffects() {
        if (weaponBehaviour != null) {
            Debug.Log($"AK47 Exclusive Activated: Max Magazine Capacity set to {maxMagazineCapacity}.");
        }
    }
}
