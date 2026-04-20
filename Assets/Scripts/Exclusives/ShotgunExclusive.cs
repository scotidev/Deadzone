using UnityEngine;

public class ShotgunExclusive : WeaponExclusive
{
    [Header("Shotgun Exclusive Settings")]
    [SerializeField] private float fireRateIncrease = 0.30f; // +30% de taxa de disparo

    private void Awake()
    {
        base.Awake();
        SetupExclusive(9, "+30% Fire Rate");
    }

    protected override void ApplyExclusiveEffects()
    {
        if (weaponBehaviour != null)
        {
            // Aplica o bônus de taxa de disparo
            // Exemplo: weaponBehaviour.AddFireRate(fireRateIncrease);
            Debug.Log($"Shotgun Exclusive Activated: +{fireRateIncrease * 100}% Fire Rate.");
        }
    }
}
