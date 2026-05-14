using UnityEngine;
using InfimaGames.LowPolyShooterPack;

//REFATORAÇÃO: esse  script está fazendo o trabalho que o PlayerProgress deveria estar fazendo? Analise profunda necessaria. 
// Refatoração: pq esse script lê o WeaponDataSO? O weaponLevels está no playerProgress, ele é o responsavel por atualizar a arma  e seu nivel, entendo que os stats a serem atualizados devem ter sua base lida no weaponDataSO, ai é que mora a questão, deviamos ler o player progress, guardar os stats no playerprogress tambem alem do nivel da arma? ou somente o nivel da arma no player progress e atualizar os stats advindos do weaponDataSO dentro desse script? Ou mover a logica desse script para o player progress? Analise profunda necessaria.

/// <summary>
/// Manages weapon upgrades and applies stat changes to weapons at runtime.
/// Reads WeaponDataSO to calculate upgraded stats and applies them to Weapon instances.
/// </summary>
public class UpgradeManager : MonoBehaviour {

    #region STATIC

    public static UpgradeManager Instance { get; private set; }
    
    // Evento para notificar quando um item é feito upgrade
    public static event System.Action<string, ItemDataSO> OnItemUpgraded;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    #endregion

    #region METHODS

    // REFATORAÇÃO: aqui vale lembrar do playerprogress que: os items tem um nivel máximo diferente cada um. Ou seja barricadas podem ser upgradadas até o nivel 5, enquanto as armas podem ser upgradadas até o nivel 10 por exemplo, mas ainda existem os niveis de medkit, grenades... entao precisamos de juma logica generalista escalável.

    /// <summary>
    /// Calculates the cost to upgrade an item to the next level.
    /// Formula: baseUpgradeCost × currentLevel × 1.5 (exponential scaling)
    /// </summary>
    private int CalculateUpgradeCost(int baseUpgradeCost, int currentLevel) {
        // Calcula o custo linear e arredonda para baixo na centena
        int rawCost = Mathf.RoundToInt(baseUpgradeCost * currentLevel * 1.5f);
        return (rawCost / 100) * 100;
    }

/// <summary>
    /// Replaces TryUpgradeWeapon for items other than weapons.
    /// </summary>
    /// <param name="itemID">The unique identifier of the item to upgrade.</param>
    /// <param name="baseUpgradeCost">The base upgrade cost from ShopItemData.</param>
    /// <param name="itemData">Optional item data for post-upgrade logic (e.g., Vest).</param>
    /// <returns>True if upgrade succeeded, false if failed (insufficient funds or max level).</returns>
    public bool TryUpgradeItem(string itemID, int baseUpgradeCost, ItemDataSO itemData = null) {
        if (PlayerProgress.Instance == null) {
            Debug.LogWarning("[UpgradeManager] TryUpgradeItem: PlayerProgress.Instance is NULL!");
            return false;
        }

        if (!PlayerProgress.Instance.IsItemUnlocked(itemID)) {
            Debug.LogWarning($"[UpgradeManager] TryUpgradeItem: {itemID} is not unlocked!");
            return false;
        }

        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemID);
        int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemID);

        if (currentLevel >= maxLevel) {
            Debug.LogWarning($"[UpgradeManager] {itemID} is already at max level ({maxLevel})!");
            return false;
        }

        int upgradeCost = CalculateUpgradeCost(baseUpgradeCost, currentLevel);

        if (EconomyManager.Instance == null) {
            Debug.LogWarning("[UpgradeManager] TryUpgradeItem: EconomyManager.Instance is NULL!");
            return false;
        }

        bool purchaseSuccess = EconomyManager.Instance.TrySpendCurrency(upgradeCost);

        if (!purchaseSuccess) {
            Debug.Log($"[UpgradeManager] Failed to upgrade {itemID} - insufficient funds. Cost: {upgradeCost}");
            return false;
        }

        bool upgradeSuccess = PlayerProgress.Instance.UpgradeItem(itemID);

if (upgradeSuccess) {
            int newLevel = PlayerProgress.Instance.GetItemLevel(itemID);
            Debug.Log($"[UpgradeManager] {itemID} upgraded to level {newLevel} for {upgradeCost} currency.");

            // Dispara evento para que itens interessados saibam que foram atualizados
            OnItemUpgraded?.Invoke(itemID, itemData);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the next upgrade cost for an item without performing the upgrade.
    /// Useful for UI to display the price.
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <param name="baseUpgradeCost">The base upgrade cost.</param>
    /// <returns>The cost to upgrade to the next level, or -1 if at max level.</returns>
    public int GetNextUpgradeCost(string itemID, int baseUpgradeCost) {
        if (PlayerProgress.Instance == null) return -1;

        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemID);
        int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemID);

        if (currentLevel >= maxLevel) {
            return -1;
        }

        return CalculateUpgradeCost(baseUpgradeCost, currentLevel);
    }

    /// <summary>
    /// Checks if an item can be upgraded (not at max level).
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <returns>True if the item can be upgraded further.</returns>
    public bool CanUpgradeItem(string itemID) {
        if (PlayerProgress.Instance == null) return false;

        return !PlayerProgress.Instance.IsItemMaxLevel(itemID);
    }

    /// <summary>
    /// Applies upgraded stats to a weapon instance based on its current level and WeaponData.
    /// Call this after upgrading it.
    /// </summary>
    /// <param name="weaponID">The weapon's unique identifier.</param>
    /// <param name="weaponData">The weapon's data asset with base stats and scaling.</param>
    /// <param name="weaponInstance">The actual weapon component to apply stats to.</param>
    public void ApplyUpgradedStats(string weaponID, WeaponDataSO weaponData, InfimaGames.LowPolyShooterPack.WeaponBehaviour weaponInstance) {
        if (weaponData == null) {
            return;
        }

        if (weaponInstance == null) {
            return;
        }

        if (PlayerProgress.Instance == null) {
            return;
        }

        int currentLevel = PlayerProgress.Instance.GetWeaponLevel(weaponID);

        float upgradedDamage = weaponData.GetDamageAtLevel(currentLevel);
        float upgradedFireRate = weaponData.GetFireRateAtLevel(currentLevel);
        int upgradedMagazine = weaponData.GetMagazineCapacityAtLevel(currentLevel);

        Debug.Log($"[UpgradeManager] Applying level {currentLevel} stats to {weaponID}:");

        // TODO: Apply these stats to the weapon instance
        // This will require modifying the Weapon.cs script to expose setters
        // or storing these values in a way the weapon can read them, maybe use the weapondata as a source of truth and the weapon instance reads from it every time it needs to calculate damage, fire rate, etc, based on the current level stored in player progress. This way we avoid having to set these values directly on the weapon instance and keep the logic centralized in the data and player progress. It is something to think about.
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

    #endregion
}
