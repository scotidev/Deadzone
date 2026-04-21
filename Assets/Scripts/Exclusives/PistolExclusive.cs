using UnityEngine;

/// <summary>
/// Implements the Pistol Exlusive effect.
/// </summary>
public class PistolExclusive : WeaponExclusive {

    [Header("Pistol Exclusive Settings")]
    [SerializeField] private float criticalChanceIncrease = 0.30f;

    protected override void Awake() {
        base.Awake();
        SetupExclusive(9, "+30% Critical Chance");
    }

    protected override void ApplyExclusiveEffects() {
        if (weaponBehaviour != null) {
            Debug.Log($"Pistol Exclusive Activated: +{criticalChanceIncrease * 100}% Critical Chance.");
        }
    }
}
