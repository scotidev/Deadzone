using System.Collections;
using UnityEngine;

/// <summary>
/// Implements the Medkit Exlusive effect.
/// </summary>
public class MedkitExclusive : ItemExclusive {

    [Header("Medkit Exclusive Settings")]
    [SerializeField] private float continuousHealAmount = 5f;
    [SerializeField] private float healDuration = 30f;

    protected override void Awake() {
        base.Awake();
        SetupExclusive(5, "Continuous healing for 30s after use.");
    }

    protected override void ApplyExclusiveEffects() {
        Debug.Log($"Medkit Exclusive Activated: Continuous healing ({continuousHealAmount}/sec for {healDuration}s) applied.");
    }

    private IEnumerator ApplyContinuousHealing() {
        yield return new WaitForSeconds(healDuration);
        Debug.Log("Healing over time...");
    }
}
