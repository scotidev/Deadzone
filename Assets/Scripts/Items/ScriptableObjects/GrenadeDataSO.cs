using UnityEngine;

/// <summary>
/// Scriptable Object that defines the data for a grenade item, including its damage, explosion radius, and maximum ammo capacity.
/// </summary>
[CreateAssetMenu(fileName = "GrenadeData", menuName = "Deadzone/Grenade Data")]
public class GrenadeDataSO : ItemDataSO {
    [Header("Grenade Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float radius;

    [Header("Upgrade Scaling")]
    [Tooltip("Damage increase per level. 0.1f = +10% per level.")]
    [SerializeField] private float damageScaling = 0.1f;
    [Tooltip("Radius increase per level. 0.1f = +10% per level.")]
    [SerializeField] private float radiusScaling = 0.1f;

    #region PROPERTIES

    public float Damage => damage;
    public float Radius => radius;
    // MaxAmmo is inherited from ItemDataSO, allowing it to be configured per item in the Inspector

    #endregion

    #region METHODS

    /// <summary>
    /// Gets the scaled damage value at the specified upgrade level.
    /// CONCEITO: Este método aplica um fator de escala multiplicativo.
    /// Level 1 = dano base, Level 2 = base * (1 + 0.1), Level 3 = base * (1 + 0.2), etc.
    /// Isso mantém uma progressão linear e previsível.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>The damage value scaled by damageScaling per level.</returns>
    public float GetDamageAtLevel(int level) {
        // CONCEITO: Mathf.Clamp garante que level sempre está no intervalo válido
        // para evitar valores inesperados se nível inválido for passado.
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        
        // CONCEITO: Fórmula de escala linear: base * (1 + scaling * (level - 1))
        // Exemplo com damageScaling=0.1: level 1 = 10, level 2 = 11, level 3 = 12
        return damage * (1f + damageScaling * (level - 1));
    }

    /// <summary>
    /// Gets the scaled radius value at the specified upgrade level.
    /// CONCEITO: Similar ao GetDamageAtLevel, o raio também escala proporcionalmente.
    /// A área afetada aumenta com o upgrade, tornando a granada mais potente.
    /// </summary>
    /// <param name="level">The upgrade level (1-based).</param>
    /// <returns>The radius value scaled by radiusScaling per level.</returns>
    public float GetRadiusAtLevel(int level) {
        // CONCEITO: Mesmo padrão de validação que GetDamageAtLevel para consistência.
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        
        // CONCEITO: Raio escala com a mesma fórmula que o dano.
        // Isso mantém proporção entre raio e dano consistente durante upgrades.
        return radius * (1f + radiusScaling * (level - 1));
    }

    #endregion

    public override string[] GetStatLabels() => new[] { "Damage", "Radius", "Ammo" };

    public override float[] GetStatValues() => new[] { damage, radius, (float)MaxAmmo };

    public override float[] GetStatValues(int level) {
        // CONCEITO: Clamp garante que level sempre é válido antes de usar em cálculos.
        level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
        
        // CONCEITO: Retorna array com valores escalados. Cada valor usa seu próprio
        // método de scaling (GetDamageAtLevel, GetRadiusAtLevel) para flexibilidade.
        return new[] { 
            GetDamageAtLevel(level), 
            GetRadiusAtLevel(level), 
            (float)GetMaxAmmoAtLevel(level) 
        };
    }
}
