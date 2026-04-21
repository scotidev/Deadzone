using UnityEngine;

/// <summary>
/// Implements the Vest Exlusive effect.
/// </summary>
public class VestExclusive : ItemExclusive {

    [Header("Vest Exclusive Settings")]
    [SerializeField] private float regenerationRate = 2f;
    [SerializeField] private float regenerationInterval = 5f;

    protected override void Awake() {
        SetupExclusive(5, "Vest regenerates automatically.");
    }

    protected override void ApplyExclusiveEffects() {
        Debug.Log($"Vest Exclusive Activated: Vest will now regenerate automatically (Rate: {regenerationRate}/sec, Interval: {regenerationInterval}s).");
    }
}
