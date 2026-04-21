using UnityEngine;

/// <summary>
/// Implements the Bear Trap Exlusive effect.
/// </summary>
public class BearTrapExclusive : ItemExclusive {

    [Header("Bear Trap Exclusive Settings")]
    [SerializeField] private float upwardForce = 1000f;

    protected override void Awake() {
        SetupExclusive(5, "Launches zombies flying into infinity.");
    }

    protected override void ApplyExclusiveEffects() {
        Debug.Log($"Bear Trap Exclusive Activated: Zombies are launched upwards with a force of {upwardForce}.");
    }
}
