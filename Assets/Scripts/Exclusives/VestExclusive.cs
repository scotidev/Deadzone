using UnityEngine;

public class VestExclusive : ItemExclusive
{
    [Header("Vest Exclusive Settings")]
    [SerializeField] private float regenerationRate = 2f; // Pontos de colete regenerados por segundo
    [SerializeField] private float regenerationInterval = 5f; // Tempo entre cada regeneração

    private void Awake()
    {
        SetupExclusive(5, "Vest regenerates automatically.");
    }

    protected override void ApplyExclusiveEffects()
    {
        // Lógica para habilitar a regeneração automática do colete.
        Debug.Log($"Vest Exclusive Activated: Vest will now regenerate automatically (Rate: {regenerationRate}/sec, Interval: {regenerationInterval}s).");
    }
}
