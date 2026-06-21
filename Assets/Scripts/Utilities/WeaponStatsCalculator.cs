using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Calculates weapon statistics from modifiers and provides normalization
/// values for stat bars by scanning shop item data at runtime.
/// </summary>
public static class WeaponStatsCalculator {

    #region CONSTANTS

    #region WEAPON CONSTANTS

    public const float MAX_DAMAGE_WEAPON = 44f;
    public const float MAX_FIRE_RATE = 500f;
    public const float MAX_AMMO_WEAPON = 33f;

    #endregion

    #region MEDKIT CONSTANTS

    public const float MAX_HEAL = 110f;
    public const float MAX_AMMO_MEDKIT = 3f;

    #endregion

    #region GRENADE CONSTANTS

    public const float MAX_GRENADE_DAMAGE = 55f;
    public const float MAX_GRENADE_RADIUS = 22f;
    public const float MAX_AMMO_GRENADE = 10f;

    #endregion

    #region BUILDABLE CONSTANTS

    public const float MAX_BUILDABLE_DAMAGE = 110f;
    public const float MAX_BUILDABLE_RESISTANCE = 110f;
    public const float MAX_BUILDABLE_RADIUS = 55f;
    public const float MAX_AMMO_BUILDABLE = 5f;

    #endregion

    #endregion

    #region FIELDS

    private static float _globalMaxDamage = 1f;
    private static float _globalMaxFireRate = 1f;
    private static float _globalMaxAmmo = 1f;
    private static float _globalMaxVestResistance = 1f;
    private static float _globalMaxRadius = 1f;
    private static float _globalMaxHeal = 1f;
    private static bool _hasCalculatedGlobalMax = false;

    #endregion

    #region METHODS

    /// <summary>
    /// Calculates the global maximum values for weapon stats by scanning provided shop items.
    /// </summary>
    /// <param name="shopItemDatas">List of all shop items containing the ItemData to scan.</param>
    public static void CalculateGlobalMaxValues(System.Collections.Generic.List<ShopItemDataSO> shopItemDatas = null) {
        if (_hasCalculatedGlobalMax && (shopItemDatas != null && shopItemDatas.Count > 0)) {
            _hasCalculatedGlobalMax = false;
        }

        if (_hasCalculatedGlobalMax) return;

        _globalMaxDamage = 1f;
        _globalMaxFireRate = 1f;
        _globalMaxAmmo = 1f;
        _globalMaxVestResistance = 1f;
        _globalMaxRadius = 1f;
        _globalMaxHeal = 1f;

        if (shopItemDatas == null || shopItemDatas.Count == 0) {
            shopItemDatas = new System.Collections.Generic.List<ShopItemDataSO>(
                Resources.LoadAll<ShopItemDataSO>("")
            );
        }

        if (shopItemDatas == null || shopItemDatas.Count == 0) {
            _globalMaxDamage = MAX_DAMAGE_WEAPON;
            _globalMaxFireRate = MAX_FIRE_RATE;
            _globalMaxAmmo = MAX_AMMO_WEAPON;
            _globalMaxVestResistance = 100f * (1f + 0.25f * 9f);
            _globalMaxRadius = MAX_GRENADE_RADIUS;
            _globalMaxHeal = MAX_HEAL;
            Debug.LogWarning("[WeaponStatsCalculator] No shop items found! Using fallback values. Stat bars may display incorrectly.");
        } else {
            foreach (var shopItem in shopItemDatas) {
                if (shopItem?.ItemData == null) continue;

                var itemData = shopItem.ItemData;
                int maxLevel = itemData.MaxUpgradeLevel;

                float[] maxLevelStats = itemData.GetStatValues(maxLevel);
                string[] statLabels = itemData.GetStatLabels();

                for (int i = 0; i < statLabels.Length && i < maxLevelStats.Length; i++) {
                    string label = statLabels[i].ToLower();
                    float value = maxLevelStats[i];

                    if (label.Contains("damage")) {
                        if (value > _globalMaxDamage) _globalMaxDamage = value;
                    }
                    else if (label.Contains("fire rate") || label.Contains("firerate")) {
                        if (value > _globalMaxFireRate) _globalMaxFireRate = value;
                    }
                    else if (label.Contains("ammo") || label.Contains("magazine")) {
                        if (value > _globalMaxAmmo) _globalMaxAmmo = value;
                    }
                    else if (label.Contains("resistance")) {
                        if (value > _globalMaxVestResistance) _globalMaxVestResistance = value;
                    }
                    else if (label.Contains("radius")) {
                        if (value > _globalMaxRadius) _globalMaxRadius = value;
                    }
                    else if (label.Contains("heal")) {
                        if (value > _globalMaxHeal) _globalMaxHeal = value;
                    }
                }
            }
        }

        _hasCalculatedGlobalMax = true;
    }

    /// <summary>
    /// Gets the global maximum damage value for normalization.
    /// </summary>
    public static float GetGlobalMaxDamage() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxDamage;
    }

    /// <summary>
    /// Gets the global maximum fire rate value for normalization.
    /// </summary>
    public static float GetGlobalMaxFireRate() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxFireRate;
    }

    /// <summary>
    /// Gets the global maximum ammo value for normalization.
    /// </summary>
    public static float GetGlobalMaxAmmo() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxAmmo;
    }

    /// <summary>
    /// Gets the global maximum resistance value for normalization.
    /// </summary>
    public static float GetGlobalMaxResistance() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxVestResistance;
    }

    /// <summary>
    /// Gets the global maximum radius value for normalization.
    /// </summary>
    public static float GetGlobalMaxRadius() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxRadius;
    }

    /// <summary>
    /// Gets the global maximum heal value for normalization.
    /// </summary>
    public static float GetGlobalMaxHeal() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxHeal;
    }

    /// <summary>
    /// Gets the maximum value for a specific stat name.
    /// </summary>
    public static float GetMaxValueForStat(string statName) {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();

        string lowerName = statName.ToLower().Replace(" ", "").Replace("/", "");
        float result = 100f;

        if (lowerName.Contains("damage")) {
            result = _globalMaxDamage;
        }
        else if (lowerName.Contains("firerate") || lowerName.Contains("fire rate")) {
            result = _globalMaxFireRate;
        }
        else if (lowerName.Contains("ammo")) {
            result = _globalMaxAmmo;
        }
        else if (lowerName.Contains("heal")) {
            result = _globalMaxHeal;
        }
        else if (lowerName.Contains("resistance")) {
            result = _globalMaxVestResistance;
        }
        else if (lowerName.Contains("radius")) {
            result = _globalMaxRadius;
        }

        return result;
    }

    /// <summary>
    /// Normalizes a value by its stat maximum, returning a 0-1 clamped result.
    /// </summary>
    public static float NormalizeByStatName(string statName, float value) {
        float maxValue = GetMaxValueForStat(statName);
        if (maxValue <= 0f) return 0f;
        return Mathf.Clamp01(value / maxValue);
    }

    /// <summary>
    /// Resets the global max values calculation flag, allowing recalculation at runtime.
    /// </summary>
    public static void ResetCalculation() {
        _hasCalculatedGlobalMax = false;
    }

    #endregion

    #region DEBUG

    /// <summary>
    /// Logs all current max values for debugging.
    /// </summary>
    public static void LogCurrentMaxValues() {
        Debug.Log($"[WeaponStatsCalculator] Current Max Values:\n" +
                  $"  Damage: {_globalMaxDamage}\n" +
                  $"  Fire Rate: {_globalMaxFireRate}\n" +
                  $"  Ammo: {_globalMaxAmmo}\n" +
                  $"  Healing: {_globalMaxHeal}\n" +
                  $"  Resistance: {_globalMaxVestResistance}\n" +
                  $"  Radius: {_globalMaxRadius}\n" +
                  $"  Calculated: {_hasCalculatedGlobalMax}");
    }

    #endregion
}
