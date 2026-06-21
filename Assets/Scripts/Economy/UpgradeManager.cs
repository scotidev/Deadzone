using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Manages weapon upgrades and applies stat changes to weapons at runtime.
/// Reads WeaponDataSO to calculate upgraded stats and applies them to Weapon instances.
/// </summary>
public class UpgradeManager : MonoBehaviour {

    #region STATIC

    public static UpgradeManager Instance { get; private set; }

    #endregion

    #region EVENTS

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
            return false;
        }

        bool upgradeSuccess = PlayerProgress.Instance.UpgradeItem(itemID);

        if (upgradeSuccess) {
            int newLevel = PlayerProgress.Instance.GetItemLevel(itemID);

            OnItemUpgraded?.Invoke(itemID, itemData);

            return true;
        }

        return false;
    }

    public bool CanUpgradeItem(string itemID) {
        if (PlayerProgress.Instance == null) return false;

        return !PlayerProgress.Instance.IsItemMaxLevel(itemID);
    }

    #endregion
}
