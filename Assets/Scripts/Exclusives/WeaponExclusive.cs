using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public abstract class WeaponExclusive : ItemExclusive
{
    protected WeaponBehaviour weaponBehaviour;

    protected virtual void Awake()
    {
        base.Awake(); // Chama o Awake da classe base primeiro

        // Tenta obter o componente WeaponBehaviour no mesmo GameObject.
        weaponBehaviour = GetComponent<WeaponBehaviour>();
        if (weaponBehaviour == null)
        {
            Debug.LogError("WeaponExclusive requires a WeaponBehaviour component on the same GameObject.");
        }
    }

    protected override void ApplyExclusiveEffects()
    {
        if (weaponBehaviour != null)
        {
            Debug.Log("Applying generic weapon exclusive effects.");
            // Lógica base para aplicar efeitos que podem ser comuns a todas as armas exclusivas
            // Exemplo placeholder: weaponBehaviour.ModifyStat("CriticalChance", 0.30f); 
        }
    }

    public void SetupExclusive(int level, string description)
    {
        SetUnlockLevel(level);
        SetExclusiveDescription(description);
    }
}
