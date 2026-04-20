using UnityEngine;

public class AK47Exclusive : WeaponExclusive
{
    [Header("AK47 Exclusive Settings")]
    [SerializeField] private int maxMagazineCapacity = 100;

    private void Awake()
    {
        base.Awake();
        SetupExclusive(9, "Max Magazine Capacity increased to 100");
    }

    protected override void ApplyExclusiveEffects()
    {
        if (weaponBehaviour != null)
        {
            // Aplica o bônus de capacidade máxima do pente
            // Exemplo: weaponBehaviour.SetMaxMagazineCapacity(maxMagazineCapacity);
            Debug.Log($"AK47 Exclusive Activated: Max Magazine Capacity set to {maxMagazineCapacity}.");
        }
    }
}
