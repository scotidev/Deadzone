using UnityEngine;

public class ExplosiveBarrelExclusive : ItemExclusive
{
    [Header("Explosive Barrel Exclusive Settings")]
    [SerializeField] private float damageOverTime = 5f;
    [SerializeField] private float damageInterval = 1f; // Tempo entre aplicações de dano
    [SerializeField] private float duration = 10f; // Duração do efeito de dano ao longo do tempo

    private void Awake()
    {
        SetupExclusive(5, "Deals damage over time to zombies.");
    }

    protected override void ApplyExclusiveEffects()
    {
        // Lógica para aplicar dano contínuo aos zumbis próximos após a explosão.
        Debug.Log($"Explosive Barrel Exclusive Activated: Deals {damageOverTime} damage over time (interval: {damageInterval}s, duration: {duration}s) to zombies.");
    }
}
