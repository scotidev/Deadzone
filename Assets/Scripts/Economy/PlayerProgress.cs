using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Tracks the player's progression through the game.
/// Stores weapon unlocks, upgrade levels, ammo reserves, and buildable quantities.
/// This is runtime-only data (not saved between sessions for web game).
/// </summary>
public class PlayerProgress : MonoBehaviour {

    #region STATIC

    public static PlayerProgress Instance { get; private set; }

    #endregion

    #region FIELDS

    private Dictionary<string, bool> unlockedWeapons = new Dictionary<string, bool>();
    private Dictionary<string, bool> unlockedBuildables = new Dictionary<string, bool>();
    private Dictionary<string, bool> unlockedConsumables = new Dictionary<string, bool>();
    private Dictionary<string, int> weaponLevels = new Dictionary<string, int>();
    private Dictionary<string, int> itemLevels = new Dictionary<string, int>();
    private Dictionary<string, int> weaponReserveAmmo = new Dictionary<string, int>();
    private HashSet<string> ammoInitialized = new HashSet<string>();
    private Dictionary<string, int> buildableQuantities = new Dictionary<string, int>();
    private Dictionary<string, int> itemCurrentAmmo = new Dictionary<string, int>();
    private Dictionary<string, int> itemTotalAmmo = new Dictionary<string, int>();

    #endregion

    #region CONSTANTS

    public const int MAX_UPGRADE_LEVEL = 10;
    public const int MAX_BUILDABLE_QUANTITY = 5;
    public const int MAX_CONSUMABLE_QUANTITY = 10;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }

        InitializeDefaults();
    }

    #endregion

    #region METHODS

    #region INITIALIZATION

    private void InitializeDefaults() {
    }

    /// <summary>
    /// Resets all progression data to default values.
    /// </summary>
    public void ResetProgress() {
        unlockedWeapons.Clear();
        unlockedBuildables.Clear();
        unlockedConsumables.Clear();
        weaponLevels.Clear();
        itemLevels.Clear();
        weaponReserveAmmo.Clear();
        ammoInitialized.Clear();
        buildableQuantities.Clear();
        itemCurrentAmmo.Clear();
        itemTotalAmmo.Clear();

        InitializeDefaults();
    }

    #endregion

    #region UNLOCKS

    /// <summary>
    /// Unlocks an item (weapon or buildable) based on its type.
    /// </summary>
    public void UnlockItem(ShopItemDataSO itemData, int quantity = 1) {
        if (itemData == null || itemData.ItemData == null) {
            Debug.LogWarning($"[PlayerProgress] UnlockItem: itemData or itemData.ItemData is NULL!");
            return;
        }

        if (itemData.ItemData is WeaponDataSO) {
            UnlockWeaponInternal(itemData.ItemID);
        }
        else if (itemData.ItemData is BuildableDataSO) {
            UnlockBuildableInternal(itemData.ItemID, quantity);
        } else if (itemData.ItemData is MedkitDataSO) {
            UnlockConsumableInternal(itemData.ItemID, quantity);
        } else if (itemData.ItemData is GrenadeDataSO) {
            UnlockConsumableInternal(itemData.ItemID, quantity);
        } else if (itemData.ItemData is VestDataSO) {
            UnlockConsumableInternal(itemData.ItemID, 1);
        } else {
            Debug.LogWarning($"[PlayerProgress] Unsupported item type for unlocking: {itemData.ItemData.GetType().Name} (ID: {itemData.ItemID})");
        }
    }

    /// <summary>
    /// Unlocks a weapon, making it available for use.
    /// </summary>
    private void UnlockWeaponInternal(string weaponID) {
        if (!unlockedWeapons.ContainsKey(weaponID)) {
            unlockedWeapons[weaponID] = true;
        } else {
            unlockedWeapons[weaponID] = true;
        }

        if (!weaponLevels.ContainsKey(weaponID)) {
            weaponLevels[weaponID] = 1;
        }

        if (!itemLevels.ContainsKey(weaponID)) {
            itemLevels[weaponID] = 1;
        }

        if (!weaponReserveAmmo.ContainsKey(weaponID)) {
            weaponReserveAmmo[weaponID] = 0;
        }

        FillItemToMax(weaponID);
    }

    /// <summary>
    /// Unlocks a buildable item (Barricade, ExplosiveBarrel, BearTrap).
    /// </summary>
    private void UnlockBuildableInternal(string buildableID, int initialQuantity = 1) {
        if (!unlockedBuildables.ContainsKey(buildableID)) {
            unlockedBuildables[buildableID] = true;
        } else {
            unlockedBuildables[buildableID] = true;
        }

        if (!itemLevels.ContainsKey(buildableID)) {
            itemLevels[buildableID] = 1;
        }

        FillItemToMax(buildableID);
    }

    /// <summary>
    /// Unlocks a consumable item (Medkit, Grenade, Vest).
    /// </summary>
    private void UnlockConsumableInternal(string consumableID, int initialQuantity = 1) {
        if (!unlockedConsumables.ContainsKey(consumableID)) {
            unlockedConsumables[consumableID] = true;
        } else {
            unlockedConsumables[consumableID] = true;
        }

        if (!itemLevels.ContainsKey(consumableID)) {
            itemLevels[consumableID] = 1;
        }

        FillItemToMax(consumableID);
    }

    /// <summary>
    /// Checks if a weapon is unlocked.
    /// </summary>
    public bool IsWeaponUnlocked(string weaponID) {
        bool isUnlocked = unlockedWeapons.TryGetValue(weaponID, out bool unlocked) && unlocked;
        return isUnlocked;
    }

    /// <summary>
    /// Generic method to check if any item (weapon, buildable, consumable) is unlocked.
    /// </summary>
    public bool IsItemUnlocked(string itemID) {
        if (unlockedWeapons.TryGetValue(itemID, out bool weaponUnlocked) && weaponUnlocked) {
            return true;
        }

        if (unlockedBuildables.TryGetValue(itemID, out bool buildableUnlocked) && buildableUnlocked) {
            return true;
        }

        if (unlockedConsumables.TryGetValue(itemID, out bool consumableUnlocked) && consumableUnlocked) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the current quantity of a buildable item in inventory.
    /// </summary>
    public int GetBuildableQuantity(string buildableID) {
        return GetItemTotal(buildableID);
    }

    /// <summary>
    /// Consumes a buildable item (when placing it in the world).
    /// </summary>
    public bool ConsumeBuildable(string buildableID) {
        return UseItem(buildableID, 1);
    }

    #endregion

    #region UPGRADES

    /// <summary>
    /// Upgrades a weapon to the next level (up to max level 10).
    /// </summary>
    public bool UpgradeWeapon(string weaponID) {
        int currentLevel = GetWeaponLevel(weaponID);

        if (currentLevel >= MAX_UPGRADE_LEVEL) {
            return false;
        }

        weaponLevels[weaponID] = currentLevel + 1;
        itemLevels[weaponID] = weaponLevels[weaponID];

        return true;
    }

    /// <summary>
    /// Generic method to upgrade any item (weapon, buildable, consumable).
    /// </summary>
    public bool UpgradeItem(string itemID) {
        int currentLevel = GetItemLevel(itemID);

        if (currentLevel >= MAX_UPGRADE_LEVEL) {
            return false;
        }

        itemLevels[itemID] = currentLevel + 1;

        if (unlockedWeapons.ContainsKey(itemID)) {
            weaponLevels[itemID] = itemLevels[itemID];
        }

        FillItemToMax(itemID);

        return true;
    }

    /// <summary>
    /// Fills the item's ammo/quantity to the maximum allowed at its current level.
    /// </summary>
    public void FillItemToMax(string itemID) {
        int level = GetItemLevel(itemID);
        
        int maxCurrent = GetItemMaxCurrent(itemID, level);
        itemCurrentAmmo[itemID] = maxCurrent;
        
        int maxTotal = GetItemMaxTotal(itemID, level);
        itemTotalAmmo[itemID] = maxTotal;

        if (unlockedWeapons.ContainsKey(itemID)) {
            weaponReserveAmmo[itemID] = maxTotal;
        }
    }

    /// <summary>
    /// Gets the current upgrade level of a weapon.
    /// </summary>
    public int GetWeaponLevel(string weaponID) {
        return weaponLevels.TryGetValue(weaponID, out int level) ? level : 1;
    }

    /// <summary>
    /// Generic method to get upgrade level of any item (weapon, buildable, consumable).
    /// </summary>
    public int GetItemLevel(string itemID) {
        if (itemLevels.TryGetValue(itemID, out int level)) {
            return level;
        }

        return GetWeaponLevel(itemID);
    }

    /// <summary>
    /// Checks if a weapon is at maximum level.
    /// </summary>
    public bool IsWeaponMaxLevel(string weaponID) {
        return GetWeaponLevel(weaponID) >= MAX_UPGRADE_LEVEL;
    }

    /// <summary>
    /// Gets the maximum upgrade level for any item by reading from its ScriptableObject.
    /// </summary>
    public int GetItemMaxLevel(string itemID) {
        var shopItemData = GetShopItemData(itemID);

        if (shopItemData != null && shopItemData.ItemData != null) {
            if (shopItemData.ItemData != null) {
                return shopItemData.ItemData.MaxUpgradeLevel;
            }
        }

        return MAX_UPGRADE_LEVEL;
    }

    /// <summary>
    /// Checks if an item is at its maximum level using dynamic max level.
    /// </summary>
    public bool IsItemMaxLevel(string itemID) {
        return GetItemLevel(itemID) >= GetItemMaxLevel(itemID);
    }

    /// <summary>
    /// Gets the maximum ammo/quantity for an item at a specific level.
    /// </summary>
    public int GetMaxAmmoAtLevel(string itemID, int level) {
        var shopItemData = GetShopItemData(itemID);
        
        if (shopItemData?.ItemData == null) {
            Debug.LogWarning($"[PlayerProgress] GetMaxAmmoAtLevel: Could not find item data for {itemID}. Returning default 10.");
            return 10;
        }
        
        return shopItemData.ItemData.GetMaxAmmoAtLevel(level);
    }

    /// <summary>
    /// Gets the current ammo/quantity for ANY item type (weapons, buildables, consumables).
    /// </summary>
    public int GetCurrentAmmoForItem(string itemID) {
        var shopItemData = GetShopItemData(itemID);
        if (shopItemData?.ItemData == null) {
            return 0;
        }
        
        if (shopItemData.ItemData is WeaponDataSO) {
            return GetWeaponReserveAmmo(itemID);
        }
        
        if (shopItemData.ItemData is BuildableDataSO ||
            shopItemData.ItemData is MedkitDataSO ||
            shopItemData.ItemData is GrenadeDataSO) {
            return GetConsumableQuantity(itemID);
        }
        
        if (shopItemData.ItemData is VestDataSO) {
            return 0;
        }
        
        return 0;
    }

    /// <summary>
    /// Helper to find ShopItemDataSO by item ID.
    /// </summary>
    private ShopItemDataSO GetShopItemData(string itemID) {
        var allShopItems = UnityEngine.Resources.FindObjectsOfTypeAll<ShopItemDataSO>();
        foreach (var shopItem in allShopItems) {
            if (shopItem.ItemID == itemID) {
                return shopItem;
            }
        }
        return null;
    }

    #endregion

    #region UNIFIED AMMO/QUANTITY SYSTEM

    /// <summary>
    /// Initializes ammo/quantity for an item when it's unlocked.
    /// </summary>
    public void InitializeItemAmmo(string itemID, int level = 1) {
        var shopItemData = GetShopItemData(itemID);
        if (shopItemData?.ItemData == null) {
            Debug.LogWarning($"[PlayerProgress] InitializeItemAmmo: Could not find item data for {itemID}");
            return;
        }

        int maxCurrent = shopItemData.ItemData.GetMaxCurrentCapacityAtLevel(level);
        
        itemCurrentAmmo[itemID] = 0;
        
        itemTotalAmmo[itemID] = 0;
    }

    /// <summary>
    /// Gets the current quantity in hand (magazine for weapons, 1 for consumables when selected).
    /// </summary>
    public int GetItemCurrent(string itemID) {
        return itemCurrentAmmo.TryGetValue(itemID, out int current) ? current : 0;
    }

    /// <summary>
    /// Gets the total quantity in inventory (reserve).
    /// </summary>
    public int GetItemTotal(string itemID) {
        return itemTotalAmmo.TryGetValue(itemID, out int total) ? total : 0;
    }

    /// <summary>
    /// Gets the maximum capacity for current (magazine/hand).
    /// </summary>
    public int GetItemMaxCurrent(string itemID, int level = -1) {
        if (level == -1) {
            level = GetItemLevel(itemID);
        }

        var shopItemData = GetShopItemData(itemID);
        if (shopItemData?.ItemData == null) {
            return 1;
        }

        return shopItemData.ItemData.GetMaxCurrentCapacityAtLevel(level);
    }

    /// <summary>
    /// Gets the maximum total capacity (total ammo/quantity allowed).
    /// </summary>
    public int GetItemMaxTotal(string itemID, int level = -1) {
        if (level == -1) {
            level = GetItemLevel(itemID);
        }

        return GetMaxAmmoAtLevel(itemID, level);
    }

    /// <summary>
    /// Adds ammo/quantity to the inventory (total).
    /// </summary>
    public bool AddItemAmmo(string itemID, int amount) {
        int level = GetItemLevel(itemID);
        int currentTotal = GetItemTotal(itemID);
        int maxTotal = GetItemMaxTotal(itemID, level);

        if (currentTotal >= maxTotal) {
            Debug.LogWarning($"[PlayerProgress] {itemID} inventory is already at max ({maxTotal}).");
            return false;
        }

        int newTotal = Mathf.Min(currentTotal + amount, maxTotal);
        itemTotalAmmo[itemID] = newTotal;

        return true;
    }

    /// <summary>
    /// Uses an item (reduces total).
    /// </summary>
    public bool UseItem(string itemID, int amount = 1) {
        int currentTotal = GetItemTotal(itemID);

        if (currentTotal < amount) {
            Debug.LogWarning($"[PlayerProgress] Not enough {itemID} to use (have {currentTotal}, need {amount}).");
            return false;
        }

        itemTotalAmmo[itemID] = currentTotal - amount;
        return true;
    }

    /// <summary>
    /// Sets the current ammo/quantity in hand.
    /// </summary>
    public void SetItemCurrent(string itemID, int amount) {
        int level = GetItemLevel(itemID);
        int maxCurrent = GetItemMaxCurrent(itemID, level);
        
        int clamped = Mathf.Clamp(amount, 0, maxCurrent);
        itemCurrentAmmo[itemID] = clamped;
    }

    /// <summary>
    /// Checks if starting ammo has been granted to this weapon type.
    /// </summary>
    public bool IsAmmoInitialized(string itemID) {
        return ammoInitialized.Contains(itemID);
    }

    /// <summary>
    /// Marks a weapon type as having received its starting ammo.
    /// </summary>
    public void MarkAmmoInitialized(string itemID) {
        if (!ammoInitialized.Contains(itemID)) {
            ammoInitialized.Add(itemID);
        }
    }

    /// <summary>
    /// Sets the total ammo/quantity in inventory.
    /// </summary>
    public void SetItemTotal(string itemID, int amount) {
        int clamped = Mathf.Max(0, amount);
        itemTotalAmmo[itemID] = clamped;
    }

    /// <summary>
    /// Transfers ammo from total (inventory) to current (magazine/hand).
    /// </summary>
    public int ReloadItem(string itemID) {
        int level = GetItemLevel(itemID);
        int maxCurrent = GetItemMaxCurrent(itemID, level);
        int currentCurrent = GetItemCurrent(itemID);
        int currentTotal = GetItemTotal(itemID);

        int ammoNeeded = maxCurrent - currentCurrent;
        int ammoAvailable = currentTotal;
        int ammoToTransfer = Mathf.Min(ammoNeeded, ammoAvailable);

        if (ammoToTransfer > 0) {
            itemTotalAmmo[itemID] = currentTotal - ammoToTransfer;
            itemCurrentAmmo[itemID] = currentCurrent + ammoToTransfer;
        } else {
            Debug.LogWarning($"[PlayerProgress] Cannot reload {itemID} - no ammo available in total or magazine full.");
        }

        return ammoToTransfer;
    }

    #endregion

    #region AMMO AND QUANTITY

    /// <summary>
    /// Adds reserve ammo for a weapon, respecting the maximum limit.
    /// </summary>
    public bool AddReserveAmmo(string weaponID, int amount, int maxReserve) {
        bool added = AddItemAmmo(weaponID, amount);
        
        if (added) {
            weaponReserveAmmo[weaponID] = GetItemTotal(weaponID);
        }
        
        return added;
    }

    /// <summary>
    /// Spends reserve ammo (when reloading).
    /// </summary>
    public bool SpendReserveAmmo(string weaponID, int amount) {
        return UseItem(weaponID, amount);
    }

    /// <summary>
    /// Gets the current reserve ammo for a weapon.
    /// </summary>
    public int GetReserveAmmo(string weaponID) {
        return GetItemTotal(weaponID);
    }

    /// <summary>
    /// Public wrapper for GetReserveAmmo.
    /// </summary>
    public int GetWeaponReserveAmmo(string weaponID) {
        return GetReserveAmmo(weaponID);
    }

    /// <summary>
    /// Adds reserve ammo for a weapon (used by Shop UI).
    /// </summary>
    public void AddWeaponReserveAmmo(string weaponID, int amount) {
        AddItemAmmo(weaponID, amount);
    }

    #endregion

    #region CONSUMABLES

    /// <summary>
    /// Gets the current quantity of a consumable or buildable item.
    /// </summary>
    public int GetConsumableQuantity(string itemID) {
        return GetItemTotal(itemID);
    }

    /// <summary>
    /// Adds quantity to a consumable or buildable item.
    /// </summary>
    public bool AddConsumable(string itemID, int amount, int maxAmount = MAX_CONSUMABLE_QUANTITY) {
        return AddItemAmmo(itemID, amount);
    }

    /// <summary>
    /// Consumes (decrements) a consumable or buildable item.
    /// </summary>
    public bool ConsumeItem(string itemID, int amount) {
        return UseItem(itemID, amount);
    }

    #endregion

    #endregion
}
