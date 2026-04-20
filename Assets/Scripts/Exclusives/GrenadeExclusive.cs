using UnityEngine;

public class GrenadeExclusive : ItemExclusive
{
    [Header("Grenade Exclusive Settings")]
    [SerializeField] private float microExplosionDamage = 10f;
    [SerializeField] private float microExplosionRadius = 3f;

    private void Awake()
    {
        base.Awake(); // Chama o Awake da classe base primeiro
        SetupExclusive(5, "Causes micro-explosions on impact.");
    }

    protected override void ApplyExclusiveEffects()
    {
        Debug.Log($"Grenade Exclusive Activated: Causes micro-explosions (Damage: {microExplosionDamage}, Radius: {microExplosionRadius}).");
    }
}
