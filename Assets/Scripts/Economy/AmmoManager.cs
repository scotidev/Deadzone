using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack {

/// <summary>
/// Manages ammo/quantity purchases for all item types in the shop.
/// Handles the "+ammo" button logic in a scalable way.
/// </summary>
public class AmmoManager : MonoBehaviour {

    #region STATIC

    public static AmmoManager Instance { get; private set; }

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

    public bool TryAddItem(ShopItemDataSO shopItem) {
        if (shopItem == null) {
            Debug.LogWarning("[AmmoManager] shopItem is null!");
            return false;
        }

        string itemID = shopItem.ItemID;
        ItemDataSO itemData = shopItem.ItemData;
        int cost = shopItem.CostPerPurchase;
        int quantity = shopItem.QuantityPerPurchase;

        if (itemData == null) {
            Debug.LogWarning($"[AmmoManager] ItemData is null for {itemID}!");
            return false;
        }

        if (!CanAfford(cost)) {
            Debug.LogWarning($"[AmmoManager] Insufficient funds! Need {cost - GetCurrentCurrency()} more.");
            return false;
        }

        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemID);
        int maxAmount = PlayerProgress.Instance.GetMaxAmmoAtLevel(itemID, currentLevel);

        bool success = false;

        if (itemData is VestDataSO) {
            success = TryAddVest(itemID, cost);
        }
        else if (itemData is WeaponDataSO) {
            success = TryAddWeaponAmmo(itemID, quantity, maxAmount, cost);
        }
        else if (itemData is MedkitDataSO) {
            success = TryAddInventoryItem(itemID, quantity, maxAmount, cost);
        }
        else if (itemData is GrenadeDataSO) {
            success = TryAddInventoryItem(itemID, quantity, maxAmount, cost);
        }
        else if (itemData is BuildableDataSO) {
            success = TryAddInventoryItem(itemID, quantity, maxAmount, cost);
        }
        else {
            Debug.LogWarning($"[AmmoManager] Unknown item type: {itemData.GetType()}");
        }

        return success;
    }

    private bool TryAddVest(string itemID, int cost) {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) {
            Debug.LogWarning("[AmmoManager] Player not found!");
            return false;
        }

        var vest = player.GetComponent<Vest>();
        if (vest == null) {
            vest = player.GetComponentInChildren<Vest>();
        }

        if (vest == null) {
            Debug.LogWarning("[AmmoManager] Vest component not found!");
            return false;
        }

        if (vest.GetArmorFraction() >= 1f) {
            Debug.Log("[AmmoManager] Vest already at full armor!");
            return false;
        }

        if (TrySpendCurrency(cost)) {
            vest.AddArmor(vest.GetMaxArmorFromCurrentLevel());
            return true;
        }

        return false;
    }

    private bool TryAddWeaponAmmo(string weaponID, int quantity, int maxAmount, int cost) {
        if (PlayerProgress.Instance == null) {
            Debug.LogWarning("[AmmoManager] PlayerProgress is null!");
            return false;
        }

        int currentQuantity = PlayerProgress.Instance.GetWeaponReserveAmmo(weaponID);
        int newQuantity = currentQuantity + quantity;
        newQuantity = Mathf.Clamp(newQuantity, 0, maxAmount);

        int actualQuantityAdded = newQuantity - currentQuantity;
        if (actualQuantityAdded <= 0) {
            Debug.LogWarning($"[AmmoManager] {weaponID} already at max quantity ({maxAmount})!");
            return false;
        }

        float quantityProportion = (float)actualQuantityAdded / quantity;
        int actualCost = Mathf.RoundToInt(cost * quantityProportion);

        if (TrySpendCurrency(actualCost)) {
            PlayerProgress.Instance.AddWeaponReserveAmmo(weaponID, actualQuantityAdded);
            return true;
        }

        return false;
    }

    private bool TryAddInventoryItem(string itemID, int quantity, int maxAmount, int cost) {
        if (PlayerProgress.Instance == null) {
            Debug.LogWarning("[AmmoManager] PlayerProgress is null!");
            return false;
        }

        int currentQuantity = PlayerProgress.Instance.GetConsumableQuantity(itemID);
        int newQuantity = currentQuantity + quantity;
        newQuantity = Mathf.Clamp(newQuantity, 0, maxAmount);

        int actualQuantityAdded = newQuantity - currentQuantity;
        if (actualQuantityAdded <= 0) {
            Debug.LogWarning($"[AmmoManager] {itemID} already at max quantity ({maxAmount})!");
            return false;
        }

        float quantityProportion = (float)actualQuantityAdded / quantity;
        int actualCost = Mathf.RoundToInt(cost * quantityProportion);

        if (TrySpendCurrency(actualCost)) {
            PlayerProgress.Instance.AddConsumable(itemID, actualQuantityAdded, maxAmount);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the player can afford the given cost.
    /// </summary>
    private bool CanAfford(int cost) {
        return EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost);
    }

    /// <summary>
    /// Attempts to spend currency through the EconomyManager.
    /// </summary>
    private bool TrySpendCurrency(int amount) {
        return EconomyManager.Instance != null && EconomyManager.Instance.TrySpendCurrency(amount);
    }

    /// <summary>
    /// Gets the player's current currency from EconomyManager.
    /// </summary>
    private int GetCurrentCurrency() {
        return EconomyManager.Instance != null ? EconomyManager.Instance.GetCurrentCurrency() : 0;
    }

    #endregion
}

}
