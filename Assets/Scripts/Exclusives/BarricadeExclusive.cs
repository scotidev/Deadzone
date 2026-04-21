using UnityEngine;

/// <summary>
/// Implements the Barricade Exclusive.
/// </summary>
public class BarricadeExclusive : ItemExclusive {
    protected override void Awake() {
        SetupExclusive(5, "Indestructible barricades!");
    }

    protected override void ApplyExclusiveEffects() {
        Debug.Log("Barricade Exclusive Activated: Barricades are now indestructible.");
    }
}
