using UnityEngine;
using System.Collections.Generic;

public static class WeaponStatsCalculator {
    
    #region GLOBAL MAX VALUES (calculated once at startup)
    
    // These will be set by CalculateGlobalMaxValues() and used for normalization
    private static float _globalMaxDamage = 1f;
    private static float _globalMaxFireRate = 1f;
    private static float _globalMaxAmmo = 1f;
    private static float _globalMaxVestResistance = 1f;
    
    // Flag to track if calculation has been done
    private static bool _hasCalculatedGlobalMax = false;
    
    #endregion
    
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
    
    #region METHODS
    
    /// <summary>
    /// Calculates the global maximum values for weapon stats by scanning all relevant assets.
    /// This should be called once at game startup (e.g., in ShopManager.Awake()).
    /// </summary>
    public static void CalculateGlobalMaxValues() {
        // Prevent recalculation
        if (_hasCalculatedGlobalMax) return;
        
        // Reset values
        _globalMaxDamage = 1f;
        _globalMaxFireRate = 1f;
        _globalMaxAmmo = 1f;
        _globalMaxVestResistance = 1f;
        
        // APPROACH: Load ShopItemDataSO assets (which are likely in Resources)
        // Then extract their ItemData references to get WeaponDataSO, VestDataSO, etc.
        var shopItemDatas = Resources.LoadAll<ShopItemDataSO>("");
        
        if (shopItemDatas == null || shopItemDatas.Length == 0) {
            _globalMaxDamage = MAX_DAMAGE_WEAPON;
            _globalMaxFireRate = MAX_FIRE_RATE;
            _globalMaxAmmo = MAX_AMMO_WEAPON;
            _globalMaxVestResistance = 100f * (1f + 0.25f * 9f); // Vest max at level 10
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
                }
            }
        }
        
        _hasCalculatedGlobalMax = true;
    }
    
    /// <summary>
    /// Gets the global maximum damage value (for normalization 0-1)
    /// </summary>
    public static float GetGlobalMaxDamage() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxDamage;
    }
    
    /// <summary>
    /// Gets the global maximum fire rate value (for normalization 0-1)
    /// </summary>
    public static float GetGlobalMaxFireRate() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxFireRate;
    }
    
    /// <summary>
    /// Gets the global maximum ammo value (for normalization 0-1)
    /// </summary>
    public static float GetGlobalMaxAmmo() {
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        return _globalMaxAmmo;
    }
    
    public static float GetMaxValueForStat(string statName) {
        // Calculate global max values if not done yet
        if (!_hasCalculatedGlobalMax) CalculateGlobalMaxValues();
        
        string lowerName = statName.ToLower().Replace(" ", "").Replace("/", "");
        float result = 100f; // default
        
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
            result = MAX_HEAL; // Medkit heal doesn't scale globally for now
        }
        else if (lowerName.Contains("resistance")) {
            result = _globalMaxVestResistance;
        }
        else if (lowerName.Contains("radius")) {
            result = MAX_GRENADE_RADIUS; // Grenade radius doesn't scale globally for now
        }
        
        return result;
    }
    
    public static float NormalizeByStatName(string statName, float value) {
        float maxValue = GetMaxValueForStat(statName);
        if (maxValue <= 0f) return 0f;
        return Mathf.Clamp01(value / maxValue);
    }
    
    #endregion
}
