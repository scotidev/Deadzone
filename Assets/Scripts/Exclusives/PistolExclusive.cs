using UnityEngine;

public class PistolExclusive : WeaponExclusive
{
    [Header("Pistol Exclusive Settings")]
    [SerializeField] private float criticalChanceIncrease = 0.30f; // +30% chance de crítico

    private void Awake()
    {
        base.Awake(); // Chama o Awake da classe base primeiro
        SetupExclusive(9, "+30% Critical Chance"); 
    }

    protected override void ApplyExclusiveEffects()
    {
        if (weaponBehaviour != null)
        {
            // Aplica o bônus de chance de crítico
            // Exemplo: weaponBehaviour.AddCriticalChance(criticalChanceIncrease);
            Debug.Log($"Pistol Exclusive Activated: +{criticalChanceIncrease * 100}% Critical Chance.");
        }
    }
}
