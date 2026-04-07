using UnityEngine;

/// <summary>
/// ScriptableObject that defines a weapon's base stats and how they scale with upgrades.
/// Each weapon (Pistol, SMG, Shotgun, etc.) has one of these assets.
/// </summary>
/// CONCEITO PEDAGÓGICO: ScriptableObject
/// ScriptableObjects são ASSETS (arquivos .asset) que armazenam dados reutilizáveis
/// Vantagens sobre MonoBehaviour:
/// 1. Não precisa estar em uma cena (persiste entre cenas)
/// 2. Um único asset pode ser referenciado por múltiplos objetos (economia de memória)
/// 3. Mudanças no Inspector são salvas no asset, não em instâncias
/// 4. Perfeito para dados de configuração (stats de armas, itens, inimigos, etc)
/// 
/// SEPARAÇÃO DE DADOS E LÓGICA:
/// - Este ScriptableObject: DADOS (quanto de dano, qual cadência de tiro)
/// - MonoBehaviour (Weapon.cs): LÓGICA (como atirar, quando recarregar)
[CreateAssetMenu(fileName = "WeaponData", menuName = "Deadzone/Weapon Data")]
public class WeaponDataSO : ScriptableObject {

    [Header("Identification")]
    [Tooltip("Unique identifier for this weapon (matches shop items and inventory).")]
    public string weaponID;

    [Tooltip("Display name shown in UI.")]
    public string weaponName;

    [Header("Base Stats (Level 1)")]
    [Tooltip("Base damage per shot/hit.")]
    public float baseDamage = 10f;

    [Tooltip("Base fire rate in rounds per minute.")]
    public float baseFireRate = 200f;

    [Tooltip("Base magazine capacity (ammo per clip).")]
    public int baseMagazineCapacity = 30;

    [Tooltip("Maximum reserve ammo the player can carry for this weapon.")]
    public int maxReserveAmmo = 300;

    [Header("Upgrade Scaling (Levels 1-10)")]
    [Tooltip("Percentage increase in damage per upgrade level (e.g., 0.1 = +10% per level).")]
    [Range(0f, 0.5f)]
    /// CONCEITO: Scaling Percentual
    /// Em vez de adicionar valores fixos (+5 de dano), usamos percentuais (+10% de dano)
    /// Isso torna os upgrades mais significativos em níveis altos
    /// Exemplo: 0.1 = 10% de aumento por nível
    ///   Nível 1: 10 de dano base
    ///   Nível 5: 10 × (1 + 0.1×5) = 10 × 1.5 = 15 de dano
    ///   Nível 10: 10 × (1 + 0.1×10) = 10 × 2.0 = 20 de dano (DOBRO!)
    public float damageScaling = 0.1f; // +10% per level

    [Tooltip("Percentage increase in fire rate per upgrade level.")]
    [Range(0f, 0.5f)]
    /// [Range] limita o valor no Inspector (previne valores absurdos como 500% de scaling)
    public float fireRateScaling = 0.05f; // +5% per level

    [Tooltip("Percentage increase in magazine capacity per upgrade level.")]
    [Range(0f, 0.5f)]
    public float magazineScaling = 0.1f; // +10% per level

    [Header("Exclusive Power (Level 10)")]
    [Tooltip("Reference to the exclusive power script for this weapon at max level.")]
    public string exclusivePowerID; // e.g., "PistolExclusive", "SMGExclusive"

    [Tooltip("Description of the exclusive power for UI display.")]
    [TextArea(2, 4)]
    public string exclusivePowerDescription;

    /// <summary>
    /// Calculates the damage stat for a given upgrade level.
    /// Formula: baseDamage × (1 + damageScaling × level)
    /// Example: Level 5 with 10% scaling = base × 1.5 (50% more damage)
    /// </summary>
    /// <param name="level">Current upgrade level (1-10).</param>
    /// <returns>The calculated damage value.</returns>
    public float GetDamageAtLevel(int level) {
        // Clamp level between 1 and 10 to prevent out-of-range values
        // Mathf.Clamp ensures the value stays within min and max
        level = Mathf.Clamp(level, 1, 10);
        
        // Calculate scaled damage: base × (1 + scaling × level)
        // Example: 10 base, 0.1 scaling, level 5 = 10 × (1 + 0.1×5) = 10 × 1.5 = 15
        return baseDamage * (1 + damageScaling * level);
    }

    /// <summary>
    /// Calculates the fire rate stat for a given upgrade level.
    /// Higher fire rate = faster shooting.
    /// </summary>
    /// <param name="level">Current upgrade level (1-10).</param>
    /// <returns>The calculated fire rate in rounds per minute.</returns>
    public float GetFireRateAtLevel(int level) {
        // Clamp to valid level range
        level = Mathf.Clamp(level, 1, 10);
        
        // Calculate scaled fire rate
        return baseFireRate * (1 + fireRateScaling * level);
    }

    /// <summary>
    /// Calculates the magazine capacity for a given upgrade level.
    /// Rounded to nearest integer since you can't have partial bullets.
    /// </summary>
    /// <param name="level">Current upgrade level (1-10).</param>
    /// <returns>The calculated magazine capacity.</returns>
    public int GetMagazineCapacityAtLevel(int level) {
        // Clamp to valid level range
        level = Mathf.Clamp(level, 1, 10);
        
        // Calculate scaled capacity and round to integer
        // Mathf.RoundToInt converts float to nearest whole number
        float scaledCapacity = baseMagazineCapacity * (1 + magazineScaling * level);
        return Mathf.RoundToInt(scaledCapacity);
    }

    /// <summary>
    /// Checks if this weapon has reached maximum level and unlocked its exclusive power.
    /// </summary>
    /// <param name="level">Current upgrade level.</param>
    /// <returns>True if level is 10 and exclusive power exists.</returns>
    public bool HasExclusivePower(int level) {
        // Must be level 10 and have an exclusive power ID defined
        return level >= 10 && !string.IsNullOrEmpty(exclusivePowerID);
    }

    /// <summary>
    /// Validates the weapon data configuration in the Unity Editor.
    /// Called automatically when values change in the Inspector.
    /// Ensures stats make sense and warns about potential issues.
    /// </summary>
    private void OnValidate() {
        // Ensure weapon ID is not empty
        if (string.IsNullOrEmpty(weaponID)) {
            Debug.LogWarning($"[WeaponDataSO] {name} has no weaponID assigned!", this);
        }

        // Ensure base stats are positive
        if (baseDamage <= 0) {
            Debug.LogWarning($"[WeaponDataSO] {name} has non-positive base damage!", this);
        }

        if (baseFireRate <= 0) {
            Debug.LogWarning($"[WeaponDataSO] {name} has non-positive base fire rate!", this);
        }

        if (baseMagazineCapacity <= 0) {
            Debug.LogWarning($"[WeaponDataSO] {name} has non-positive magazine capacity!", this);
        }

        // Warn if max reserve ammo is less than magazine capacity
        if (maxReserveAmmo < baseMagazineCapacity) {
            Debug.LogWarning($"[WeaponDataSO] {name} has reserve ammo less than magazine capacity!", this);
        }

        // Check if level 10 stats would be reasonable
        float maxDamage = GetDamageAtLevel(10);
        if (maxDamage > baseDamage * 3) {
            Debug.LogWarning($"[WeaponDataSO] {name} damage at level 10 ({maxDamage:F1}) is over 3x base. Consider lowering scaling.", this);
        }
    }
}
