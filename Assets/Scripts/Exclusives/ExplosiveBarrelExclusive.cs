using UnityEngine;

/// <summary>
/// Implements the Explosive Barrel Exlusive effect.
/// </summary>
public class ExplosiveBarrelExclusive : ItemExclusive {

    [Header("Explosive Barrel Exclusive Settings")]
    [SerializeField] private float damageOverTime = 5f;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private float duration = 5f;

    protected override void Awake() {
        SetupExclusive(5, "Deals damage over time to zombies.");
    }

    protected override void ApplyExclusiveEffects() {
        Debug.Log($"Explosive Barrel Exclusive Activated: Deals {damageOverTime} damage over time (interval: {damageInterval}s, duration: {duration}s) to zombies.");
    }
}
