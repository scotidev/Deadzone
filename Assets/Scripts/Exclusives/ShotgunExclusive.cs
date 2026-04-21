using UnityEngine;

/// <summary>
/// Implements Shotgun Exclusive effect.
/// </summary>
public class ShotgunExclusive : WeaponExclusive {

    [Header("Shotgun Exclusive Settings")]
    [SerializeField] private float fireRateIncrease = 0.30f;

    protected override void Awake() {
        base.Awake();
        SetupExclusive(9, "+30% Fire Rate");
    }

    protected override void ApplyExclusiveEffects() {
        if (weaponBehaviour != null) {
            Debug.Log($"Shotgun Exclusive Activated: +{fireRateIncrease * 100}% Fire Rate.");
        }
    }
}
