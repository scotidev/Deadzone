using InfimaGames.LowPolyShooterPack;
using UnityEngine;

/// <summary>
/// Manages weapon upgrades and applies stat changes to weapons at runtime.
/// Reads WeaponDataSO to calculate upgraded stats and applies them to Weapon instances.
/// </summary>
public class UpgradeManager : MonoBehaviour {

    /// <summary>Global access point to the single UpgradeManager instance.</summary>
    public static UpgradeManager Instance { get; private set; }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Sets up the singleton pattern.
    /// </summary>
    private void Awake() {
        // Singleton pattern - ensure only one instance exists
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Calculates the cost to upgrade a weapon to the next level.
    /// Formula: baseUpgradeCost × currentLevel × 1.5 (exponential scaling)
    /// This makes higher levels progressively more expensive.
    /// </summary>
    /// <param name="baseUpgradeCost">The base cost for the first upgrade.</param>
    /// <param name="currentLevel">The weapon's current level (1-10).</param>
    /// <returns>The cost to upgrade to the next level.</returns>
    /// CONCEITO PEDAGÓGICO: Progressão Exponencial
    /// Em jogos, queremos que upgrades fiquem mais caros conforme o jogador avança
    /// Isso cria uma curva de progressão onde os primeiros upgrades são acessíveis,
    /// mas os últimos requerem muito esforço (balanceamento de economia)
    public int CalculateUpgradeCost(int baseUpgradeCost, int currentLevel) {
        // VALIDAÇÃO: Sempre validar dados de entrada antes de fazer cálculos
        // Isso previne bugs e comportamentos inesperados
        if (currentLevel < 1 || currentLevel >= PlayerProgress.MAX_UPGRADE_LEVEL) {
            Debug.LogWarning($"[UpgradeManager] Invalid level {currentLevel} for upgrade cost calculation.");
            return int.MaxValue; // Retorna custo muito alto para prevenir upgrade inválido
        }

        // FÓRMULA DE CUSTO EXPONENCIAL: base × level × 1.5
        // Exemplo prático com base=100:
        //   Nível 1→2:  100 × 1 × 1.5 = 150 moedas
        //   Nível 5→6:  100 × 5 × 1.5 = 750 moedas
        //   Nível 9→10: 100 × 9 × 1.5 = 1350 moedas
        // OBSERVE: O custo aumenta muito mais rápido do que linearmente!
        float cost = baseUpgradeCost * currentLevel * 1.5f;

        // CONVERSÃO: Mathf.RoundToInt() arredonda float para int
        // Necessário porque currency é número inteiro (não temos "meio moeda")
        return Mathf.RoundToInt(cost);
    }

    /// <summary>
    /// Attempts to upgrade a weapon using the currency system.
    /// Validates funds, level cap, and deducts currency if successful.
    /// </summary>
    /// <param name="weaponID">The unique identifier of the weapon to upgrade.</param>
    /// <param name="baseUpgradeCost">The base upgrade cost from ShopItemData.</param>
    /// <returns>True if upgrade succeeded, false if failed (insufficient funds or max level).</returns>
    public bool TryUpgradeWeapon(string weaponID, int baseUpgradeCost) {
        // Check if PlayerProgress exists
        if (PlayerProgress.Instance == null) {
            Debug.LogError("[UpgradeManager] PlayerProgress.Instance is null!");
            return false;
        }

        // Check if weapon is unlocked
        if (!PlayerProgress.Instance.IsWeaponUnlocked(weaponID)) {
            Debug.LogWarning($"[UpgradeManager] Cannot upgrade {weaponID} - weapon is not unlocked!");
            return false;
        }

        // Get current level
        int currentLevel = PlayerProgress.Instance.GetWeaponLevel(weaponID);

        // Check if already at max level
        if (currentLevel >= PlayerProgress.MAX_UPGRADE_LEVEL) {
            Debug.LogWarning($"[UpgradeManager] {weaponID} is already at max level {PlayerProgress.MAX_UPGRADE_LEVEL}!");
            return false;
        }

        // Calculate upgrade cost for next level
        int upgradeCost = CalculateUpgradeCost(baseUpgradeCost, currentLevel);

        // Check if EconomyManager exists
        if (EconomyManager.Instance == null) {
            Debug.LogError("[UpgradeManager] EconomyManager.Instance is null!");
            return false;
        }

        // Try to spend currency
        bool purchaseSuccess = EconomyManager.Instance.TrySpendCurrency(upgradeCost);

        if (!purchaseSuccess) {
            Debug.Log($"[UpgradeManager] Failed to upgrade {weaponID} - insufficient funds. Cost: {upgradeCost}");
            return false;
        }

        // Purchase succeeded - apply upgrade
        bool upgradeSuccess = PlayerProgress.Instance.UpgradeWeapon(weaponID);

        if (upgradeSuccess) {
            int newLevel = PlayerProgress.Instance.GetWeaponLevel(weaponID);
            Debug.Log($"[UpgradeManager] {weaponID} upgraded to level {newLevel} for {upgradeCost} currency.");

            // Check if reached max level (unlocked exclusive power)
            if (newLevel == PlayerProgress.MAX_UPGRADE_LEVEL) {
                Debug.Log($"[UpgradeManager] {weaponID} reached MAX LEVEL! Exclusive power unlocked!");
                ActivateExclusivePower(weaponID);
            }

            return true;
        }

        // If upgrade failed for some reason, refund the currency
        // This is a safety fallback and shouldn't normally happen
        EconomyManager.Instance.AddCurrency(upgradeCost);
        Debug.LogError($"[UpgradeManager] Upgrade failed after purchase. Currency refunded.");
        return false;
    }

    /// <summary>
    /// Applies upgraded stats to a weapon instance based on its current level and WeaponData.
    /// Call this when equipping a weapon or after upgrading it.
    /// </summary>
    /// <param name="weaponID">The weapon's unique identifier.</param>
    /// <param name="weaponData">The weapon's data asset with base stats and scaling.</param>
    /// <param name="weaponInstance">The actual weapon component to apply stats to.</param>
    public void ApplyUpgradedStats(string weaponID, WeaponDataSO weaponData, InfimaGames.LowPolyShooterPack.WeaponBehaviour weaponInstance) {
        // Check if all references are valid
        if (weaponData == null) {
            Debug.LogError($"[UpgradeManager] Cannot apply upgrades to {weaponID} - WeaponDataSO is null!");
            return;
        }

        if (weaponInstance == null) {
            Debug.LogError($"[UpgradeManager] Cannot apply upgrades to {weaponID} - WeaponBehaviour is null!");
            return;
        }

        if (PlayerProgress.Instance == null) {
            Debug.LogError("[UpgradeManager] PlayerProgress.Instance is null!");
            return;
        }

        // Get current upgrade level
        int currentLevel = PlayerProgress.Instance.GetWeaponLevel(weaponID);

        // Calculate upgraded stats
        float upgradedDamage = weaponData.GetDamageAtLevel(currentLevel);
        float upgradedFireRate = weaponData.GetFireRateAtLevel(currentLevel);
        int upgradedMagazine = weaponData.GetMagazineCapacityAtLevel(currentLevel);

        Debug.Log($"[UpgradeManager] Applying level {currentLevel} stats to {weaponID}:");
        Debug.Log($"  - Damage: {upgradedDamage:F1}");
        Debug.Log($"  - Fire Rate: {upgradedFireRate:F1} RPM");
        Debug.Log($"  - Magazine: {upgradedMagazine}");

        // TODO: Apply these stats to the weapon instance
        // This will require modifying the Weapon.cs script to expose setters
        // or storing these values in a way the weapon can read them
        // For now, we log them - implementation in Phase 5

        //// Check if exclusive power is unlocked and activate it
        //if (weaponData.HasExclusivePower(currentLevel)) {
        //    Debug.Log($"[UpgradeManager] {weaponID} has exclusive power: {weaponData.exclusivePowerDescription}");
        //    ActivateExclusivePowerOnWeapon(weaponInstance);
        //}
    }

    /// <summary>
    /// Activates the exclusive power for a weapon that reached level 10.
    /// Finds or adds the appropriate power component to the weapon.
    /// </summary>
    /// <param name="weaponID">The weapon's unique identifier.</param>
    private void ActivateExclusivePower(string weaponID) {
        // This method is called when upgrade happens but weapon might not be equipped
        // The power will be activated when the weapon is equipped and ApplyUpgradedStats is called
        Debug.Log($"[UpgradeManager] Exclusive power will activate for {weaponID} when equipped.");
    }

    /// <summary>
    /// Activates exclusive power on a weapon instance if it's at max level.
    /// Adds the appropriate power component if it doesn't exist.
    /// </summary>
    private void ActivateExclusivePowerOnWeapon(InfimaGames.LowPolyShooterPack.WeaponBehaviour weaponInstance) {
        if (weaponInstance == null) return;

        // Check if weapon already has an exclusive power component
        ExclusivePowerBehaviour existingPower = weaponInstance.GetComponent<ExclusivePowerBehaviour>();
        if (existingPower != null) {
            // Power already exists, just activate it
            if (!existingPower.IsActive()) {
                existingPower.ActivatePower();
            }
            return;
        }

        //// Determine which power to add based on weapon name
        //string weaponName = weaponInstance.gameObject.name.ToLower();
        ////System.Type powerType = GetPowerTypeForWeapon(weaponName);

        //if (powerType != null) {
        //    // Add the appropriate power component
        //    ExclusivePowerBehaviour newPower = (ExclusivePowerBehaviour)weaponInstance.gameObject.AddComponent(powerType);
        //    newPower.ActivatePower();
        //    Debug.Log($"[UpgradeManager] Added and activated {powerType.Name} on {weaponInstance.gameObject.name}");
        //}
    }

    /// <summary>
    /// Maps weapon names to their exclusive power types.
    /// </summary>
    //private System.Type GetPowerTypeForWeapon(string weaponName) {
    //    // Match weapon names (case insensitive) to power types
    //    if (weaponName.Contains("pistol")) return typeof(InfiniteAmmoPower);
    //    if (weaponName.Contains("smg") || weaponName.Contains("ak")) return typeof(BulletStormPower);
    //    if (weaponName.Contains("shotgun")) return typeof(ExplosiveShellsPower);
    //    if (weaponName.Contains("medkit") || weaponName.Contains("health")) return typeof(InstantHealPower);
    //    if (weaponName.Contains("grenade")) return typeof(ClusterGrenadePower);
    //    // Note: Buildables (Barricades, Explosive Barrels, Traps) are not weapon instances
    //    // Their powers would be applied when they are placed in the world
    //    if (weaponName.Contains("special")) return typeof(DevastationPower);

    //    return null;
    //}

    /// <summary>
    /// Gets the next upgrade cost for a weapon without performing the upgrade.
    /// Useful for UI to display the price.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <param name="baseUpgradeCost">The base upgrade cost.</param>
    /// <returns>The cost to upgrade to the next level, or -1 if at max level.</returns>
    public int GetNextUpgradeCost(string weaponID, int baseUpgradeCost) {
        if (PlayerProgress.Instance == null) return -1;

        int currentLevel = PlayerProgress.Instance.GetWeaponLevel(weaponID);

        // If at max level, return -1 to indicate no upgrade available
        if (currentLevel >= PlayerProgress.MAX_UPGRADE_LEVEL) {
            return -1;
        }

        return CalculateUpgradeCost(baseUpgradeCost, currentLevel);
    }

    /// <summary>
    /// Checks if a weapon can be upgraded (not at max level).
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>True if the weapon can be upgraded further.</returns>
    public bool CanUpgrade(string weaponID) {
        if (PlayerProgress.Instance == null) return false;

        return !PlayerProgress.Instance.IsWeaponMaxLevel(weaponID);
    }
}
