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

    /// <summary>
    /// Upgrades the item by spending the given cost, incrementing the level in PlayerProgress,
    /// and notifying listeners. Does NOT calculate cost — the caller is responsible for that.
    /// </summary>
    /// <param name="itemID">The unique identifier of the item to upgrade.</param>
    /// <param name="cost">The exact currency cost already calculated by the caller (ShopManager).</param>
    /// <param name="itemData">Optional item data for post-upgrade logic (e.g., Vest).</param>
    /// <returns>True if upgrade succeeded, false if failed (max level or insufficient funds).</returns>
    public bool TryUpgradeItem(string itemID, int cost, ItemDataSO itemData = null) {
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

        if (EconomyManager.Instance == null) {
            Debug.LogWarning("[UpgradeManager] TryUpgradeItem: EconomyManager.Instance is NULL!");
            return false;
        }

        bool purchaseSuccess = EconomyManager.Instance.TrySpendCurrency(cost);

        if (!purchaseSuccess) {
            Debug.Log($"[UpgradeManager] Failed to upgrade {itemID} - insufficient funds. Cost: {cost}");
            return false;
        }

        bool upgradeSuccess = PlayerProgress.Instance.UpgradeItem(itemID);

        if (upgradeSuccess) {
            int newLevel = PlayerProgress.Instance.GetItemLevel(itemID);
            Debug.Log($"[UpgradeManager] {itemID} upgraded to level {newLevel} for {cost} currency.");

            OnItemUpgraded?.Invoke(itemID, itemData);

            return true;
        }

        return false;
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

    #endregion
}
