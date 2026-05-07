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

    #region PUBLIC METHODS

    /// <summary>
    /// Attempts to add item (ammo/quantity) to player's inventory.
    /// Returns true if purchase was successful.
    /// </summary>
    public bool TryAddItem(ShopItemDataSO shopItem) {
        if (shopItem == null) {
            Debug.LogWarning("[AmmoManager] shopItem is null!");
            return false;
        }

        string itemID = shopItem.ItemID;
        ItemDataSO itemData = shopItem.ItemData;
        int cost = shopItem.CostPerPurchase;
        int quantity = shopItem.QuantityPerPurchase;
        int maxAmount = shopItem.MaxReserveQuantity;

        if (itemData == null) {
            Debug.LogWarning($"[AmmoManager] ItemData is null for {itemID}!");
            return false;
        }

        // Check if player can afford
        if (!CanAfford(cost)) {
            Debug.LogWarning($"[AmmoManager] Insufficient funds! Need {cost - GetCurrentCurrency()} more.");
            return false;
        }

        // Handle based on item type
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

    #endregion

    #region PRIVATE METHODS - ITEM TYPE HANDLERS

    /// <summary>
    /// Adds vest (repairs to 100% armor).
    /// </summary>
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

        // Check if already at full armor
        if (vest.GetArmorFraction() >= 1f) {
            Debug.Log("[AmmoManager] Vest already at full armor!");
            return false;
        }

        // Spend currency and fill armor to 100%
        if (TrySpendCurrency(cost)) {
            vest.AddArmor(vest.GetMaxArmorFromCurrentLevel());
            Debug.Log($"[AmmoManager] Vest repaired for ${cost}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds weapon reserve ammo.
    /// </summary>
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

        // Calculate proportional cost if not full quantity added
        float quantityProportion = (float)actualQuantityAdded / quantity;
        int actualCost = Mathf.RoundToInt(cost * quantityProportion);

        if (TrySpendCurrency(actualCost)) {
            PlayerProgress.Instance.AddWeaponReserveAmmo(weaponID, actualQuantityAdded);
            Debug.Log($"[AmmoManager] Added {actualQuantityAdded} ammo for {weaponID}. Cost: ${actualCost}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds inventory item (medkit, grenade, barricade, etc).
    /// </summary>
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

        // Calculate proportional cost if not full quantity added
        float quantityProportion = (float)actualQuantityAdded / quantity;
        int actualCost = Mathf.RoundToInt(cost * quantityProportion);

        if (TrySpendCurrency(actualCost)) {
            PlayerProgress.Instance.AddConsumable(itemID, actualQuantityAdded, maxAmount);
            Debug.Log($"[AmmoManager] Added {actualQuantityAdded} of {itemID}. Cost: ${actualCost}");
            return true;
        }

        return false;
    }

    #endregion

    #region HELPER METHODS

    private bool CanAfford(int cost) {
        return EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost);
    }

    private bool TrySpendCurrency(int amount) {
        return EconomyManager.Instance != null && EconomyManager.Instance.TrySpendCurrency(amount);
    }

    private int GetCurrentCurrency() {
        return EconomyManager.Instance != null ? EconomyManager.Instance.GetCurrentCurrency() : 0;
    }

    #endregion
}

}