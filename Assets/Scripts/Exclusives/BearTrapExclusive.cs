using UnityEngine;

public class BearTrapExclusive : ItemExclusive
{
    [Header("Bear Trap Exclusive Settings")]
    [SerializeField] private float upwardForce = 1000f;

    private void Awake()
    {
        SetupExclusive(5, "Launches zombies flying into infinity.");
    }

    protected override void ApplyExclusiveEffects()
    {
        // Lógica para aplicar a força para cima no zumbi atingido.
        Debug.Log($"Bear Trap Exclusive Activated: Zombies are launched upwards with a force of {upwardForce}.");
    }
}
