using UnityEngine;

public static class WeaponStatsCalculator {

    #region WEAPON CONSTANTS

    public const float MAX_DAMAGE_WEAPON = 44f;
    public const float MAX_FIRE_RATE = 500f;
    public const float MAX_AMMO_WEAPON = 33f;
    public const float MAX_CRIT = 25f;

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

    #region VEST CONSTANTS

    public const float MAX_VEST_RESISTANCE = 130f;

    #endregion

    #region BUILDABLE CONSTANTS

    public const float MAX_BUILDABLE_DAMAGE = 110f;
    public const float MAX_BUILDABLE_RESISTANCE = 110f;
    public const float MAX_BUILDABLE_RADIUS = 55f;
    public const float MAX_AMMO_BUILDABLE = 5f;

    #endregion

    #region METHODS

    public static float Normalize(float value, float maxValue) {
        if (maxValue <= 0f) return 0f;
        return Mathf.Clamp01(value / maxValue);
    }

    public static float GetMaxValueForStat(string statName) {
        string lowerName = statName.ToLower().Replace(" ", "").Replace("/", "");

        if (lowerName.Contains("damage")) return MAX_DAMAGE_WEAPON;
        if (lowerName.Contains("firerate") || lowerName.Contains("fire rate")) return MAX_FIRE_RATE;
        if (lowerName.Contains("ammo")) return MAX_AMMO_WEAPON;
        if (lowerName.Contains("crit")) return MAX_CRIT;
        if (lowerName.Contains("heal")) return MAX_HEAL;
        if (lowerName.Contains("resistance")) return MAX_VEST_RESISTANCE;
        if (lowerName.Contains("radius")) return MAX_GRENADE_RADIUS;

        return 100f;
    }

    public static float NormalizeByStatName(string statName, float value) {
        float maxValue = GetMaxValueForStat(statName);
        return Normalize(value, maxValue);
    }

    #endregion
}
