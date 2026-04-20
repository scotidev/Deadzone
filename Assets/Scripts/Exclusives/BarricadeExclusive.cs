using UnityEngine;

public class BarricadeExclusive : ItemExclusive
{
    private void Awake()
    {
        SetupExclusive(5, "Barricades become indestructible.");
    }

    protected override void ApplyExclusiveEffects()
    {
        // Lógica para tornar a barricada indestrutível.
        Debug.Log("Barricade Exclusive Activated: Barricades are now indestructible.");
    }
}
