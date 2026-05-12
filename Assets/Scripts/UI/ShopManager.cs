using System;
using System.Collections;
using System.Linq;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using Deadzone.Interfaces;

public enum ShopButtonDisabledReason {
    None,
    InsufficientFunds,
    MaxLevel,
    NotUnlocked,
    FullAmmo,
    FullArmor
}

/// <summary>
/// Manages the shop interface system in the game.
/// </summary>
public class ShopManager : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="ShopManager"/> instance.</summary>
    public static ShopManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [SerializeField] private Character playerCharacter;

    [Header("AFK Settings")]
    [Tooltip("Time in seconds before AFK dialogue is triggered")]
    [SerializeField] private float afkTimeThreshold = 10f;

    #endregion

    #region FIELDS

    private bool isShopOpen = false;
    private bool hasPurchasedSomething = false;
    private float afkTimer = 0f;
    private Coroutine afkCoroutine;

    #endregion

    #region EVENTS

    public static event Action<string> ItemUnlocked;
    public static event Action<string, int> AmmoPurchased;
    public static event Action CurrencyChanged;
    public static event Action ItemStateChanged;
    public static event Action<bool> ShopClosed;
    public static event Action PlayerAFK;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Calculate global max values for weapon stats normalization
        WeaponStatsCalculator.CalculateGlobalMaxValues();
        
        ResolvePlayerCharacter();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Ensures a valid reference to the player's character component.
    /// </summary>
    private void ResolvePlayerCharacter() {
        if (playerCharacter != null)
            return;

        playerCharacter = FindFirstObjectByType<Character>();
    }

    /// <summary>
    /// Opens the shop interface.
    /// Hides the interaction prompt and puts the player character in interface mode.
    /// </summary>
    public void OpenShop() {
        ResolvePlayerCharacter();
        isShopOpen = true;
        hasPurchasedSomething = false;
        GameManager.Instance?.SetState(GameState.Shopping);

        if (UIManager.Instance != null) {
            UIManager.Instance.ShowShop();
            UIManager.Instance.HideInteractionPrompt();
        }

        if (playerCharacter != null)
            playerCharacter.SetInterfaceMode(true);

        SetCursorState(true);
        StartAFKTimer();
    }

    /// <summary>
    /// Closes the shop interface and returns to gameplay mode.
    /// Restores normal player character controls.
    /// </summary>
    public void CloseShop() {
        ResolvePlayerCharacter();
        StopAFKTimer();
        isShopOpen = false;
        GameManager.Instance?.SetState(GameState.Playing);

        if (UIManager.Instance != null)
            UIManager.Instance.HideAllPanels();

        if (playerCharacter != null)
            playerCharacter.SetInterfaceMode(false);

        ShopClosed?.Invoke(hasPurchasedSomething);

        SetCursorState(false);
    }

    /// <summary>
    /// Sets the cursor lock state and visibility.
    /// </summary>
    /// <param name="visible">True to show cursor, false to hide and lock.</param>
    private void SetCursorState(bool visible) {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void StartAFKTimer() {
        if (afkCoroutine != null) StopCoroutine(afkCoroutine);
        afkCoroutine = StartCoroutine(AFKTimerCoroutine());
    }

    private void StopAFKTimer() {
        if (afkCoroutine != null) {
            StopCoroutine(afkCoroutine);
            afkCoroutine = null;
        }
        afkTimer = 0f;
    }

    private IEnumerator AFKTimerCoroutine() {
        afkTimer = 0f;
        while (isShopOpen) {
            yield return null;
            afkTimer += Time.deltaTime;

            if (afkTimer >= afkTimeThreshold) {
                if (isShopOpen) {
                    PlayerAFK?.Invoke();
                }
                afkTimer = 0f;
            }
        }
    }

    public void OnShopInteraction() {
        afkTimer = 0f;
    }

    public void OnPurchaseMade() {
        hasPurchasedSomething = true;
    }

    /// <summary>
    /// Returns the current state of the shop interface.
    /// </summary>
    /// <returns>True if the shop is currently open, false otherwise.</returns>
    public bool IsShopOpen() => isShopOpen;

    /// <summary>
    /// Notifies the SPECIFIC item that was unlocked.
    /// BUG FIX: Only notify the item that was actually unlocked, not ALL items!
    /// </summary>
    private void NotifyItemUnlocked(ShopItemDataSO unlockedItem) {
        Debug.Log($"[ShopManager] NOTIFY ITEM UNLOCKED CALLED! Item: {unlockedItem?.ItemName ?? "NULL"}");
        
        if (playerCharacter == null || unlockedItem?.ItemData == null) {
            Debug.Log($"[ShopManager] Early return - playerCharacter: {playerCharacter}, unlockedItem: {unlockedItem}");
            return;
        }
        
        var callbacks = playerCharacter.GetComponents<IShopItemCallback>();
        Debug.Log($"[ShopManager] NotifyItemUnlocked called for: {unlockedItem.ItemName} (ID: {unlockedItem.ItemID}), found {callbacks?.Length ?? 0} callbacks");
        
        foreach (var callback in callbacks) {
            // Only notify the SPECIFIC item that was unlocked!
            // Check if this callback belongs to the unlocked item
            if (callback is MonoBehaviour mono && mono.GetComponent<InfimaGames.LowPolyShooterPack.ItemBehaviour>() is { } itemBehaviour) {
                // This is a weapon/grenade/etc. - check if it's the unlocked one
                if (itemBehaviour.GetItemID() == unlockedItem.ItemID) {
                    Debug.Log($"[ShopManager] ✓ Calling OnShopUnlock() ONLY for: {unlockedItem.ItemName}");
                    callback.OnShopUnlock();
                }
            } else if (callback is Vest vest && vest.GetItemID() == unlockedItem.ItemID) {
                Debug.Log($"[ShopManager] ✓ Calling OnShopUnlock() ONLY for: {unlockedItem.ItemName} (Vest)");
                callback.OnShopUnlock();
            } else {
                Debug.Log($"[ShopManager] ✗ SKIPPING callback for different item");
            }
        }
    }

    /// <summary>
    /// Notifies the SPECIFIC item that was upgraded.
    /// BUG FIX: Only notify the item that was actually upgraded, not ALL items!
    /// </summary>
    private void NotifyItemUpgraded(ShopItemDataSO upgradedItem) {
        if (playerCharacter == null || upgradedItem?.ItemData == null) return;
        
        var callbacks = playerCharacter.GetComponents<IShopItemCallback>();
        Debug.Log($"[ShopManager] NotifyItemUpgraded called for: {upgradedItem.ItemName} (ID: {upgradedItem.ItemID}), found {callbacks?.Length ?? 0} callbacks");
        
        foreach (var callback in callbacks) {
            // Only notify the SPECIFIC item that was upgraded!
            // Check if this callback belongs to the upgraded item
            if (callback is MonoBehaviour mono && mono.GetComponent<InfimaGames.LowPolyShooterPack.ItemBehaviour>() is { } itemBehaviour) {
                // This is a weapon/grenade/etc. - check if it's the upgraded one
                if (itemBehaviour.GetItemID() == upgradedItem.ItemID) {
                    Debug.Log($"[ShopManager] ✓ Calling OnShopUpgrade() ONLY for: {upgradedItem.ItemName}");
                    callback.OnShopUpgrade();
                }
            } else if (callback is Vest vest && vest.GetItemID() == upgradedItem.ItemID) {
                Debug.Log($"[ShopManager] ✓ Calling OnShopUpgrade() ONLY for: {upgradedItem.ItemName} (Vest)");
                callback.OnShopUpgrade();
            } else {
                Debug.Log($"[ShopManager] ✗ SKIPPING callback for different item");
            }
        }
    }

    /// <summary>
    /// Attempts to unlock an item in the shop.
    /// </summary>
    /// <param name="itemData">The shop item data to unlock.</param>
    /// <returns>True if unlock was successful, false otherwise.</returns>
    public bool TryUnlockItem(ShopItemDataSO itemData) {
        if (itemData == null || EconomyManager.Instance == null || PlayerProgress.Instance == null) {
            Debug.LogWarning("[ShopManager] TryUnlockItem: null reference detected!");
            return false;
        }

        int cost = itemData.UnlockCost;

        if (!EconomyManager.Instance.TrySpendCurrency(cost)) {
            int missingAmount = cost - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopManager] Insufficient funds! Need {missingAmount} more coins."); // Adicionar feedback de som
            return false;
        }

        PlayerProgress.Instance.UnlockItem(itemData);
        Debug.Log($"[ShopManager] Unlocked {itemData.ItemName}!");

        ItemUnlocked?.Invoke(itemData.ItemID);

        NotifyItemUnlocked(itemData);
        ItemStateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Attempts to upgrade an item in the shop.
    /// </summary>
    /// <param name="itemData">The shop item data to upgrade.</param>
    /// <returns>True if upgrade was successful, false otherwise.</returns>
    public bool TryUpgradeItem(ShopItemDataSO itemData) {
        if (itemData == null || UpgradeManager.Instance == null || PlayerProgress.Instance == null) {
            return false;
        }

        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);
        int cost = GetUpgradeCost(itemData, currentLevel);

        Debug.Log($"[ShopManager] TryUpgradeItem called for: {itemData.ItemName} (ID: {itemData.ItemID}), type: {itemData.ItemData?.GetType().Name}");

        if (cost <= 0 || !EconomyManager.Instance.CanAfford(cost)) {
            int missingAmount = cost - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopManager] Insufficient funds! Need {missingAmount} more coins.");
            return false;
        }

        if (!UpgradeManager.Instance.TryUpgradeItem(itemData.ItemID, itemData.BaseUpgradeCost, itemData.ItemData)) {
            return false;
        }

        Debug.Log($"[ShopManager] Upgraded {itemData.ItemName} to level {PlayerProgress.Instance.GetItemLevel(itemData.ItemID)}!");

        NotifyItemUpgraded(itemData);
        ItemStateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Gets the cost for the next upgrade of an item.
    /// Formula: unlockCost + (baseUpgradeCost * multiplier * currentLevel)
    /// </summary>
    /// <param name="itemData">The shop item data.</param>
    /// <param name="currentLevel">Current upgrade level.</param>
    /// <returns>The cost for the next level, or 0 if max level.</returns>
    public int GetUpgradeCost(ShopItemDataSO itemData, int currentLevel) {
        if (itemData == null) return 0;

        int maxLevel = PlayerProgress.Instance != null
            ? PlayerProgress.Instance.GetItemMaxLevel(itemData.ItemID)
            : 10;

        if (currentLevel >= maxLevel) return 0;

        return itemData.GetUpgradeCost(currentLevel);
    }

    /// <summary>
    /// Attempts to purchase ammo/supplies for an item.
    /// Delegates to AmmoManager which handles all item types (Weapon, Vest, Medkit, Grenade, Buildable).
    /// </summary>
    /// <param name="itemData">The shop item data to purchase.</param>
    /// <returns>True if purchase was successful, false otherwise.</returns>
    public bool TryBuyAmmo(ShopItemDataSO itemData) {
        if (itemData == null || AmmoManager.Instance == null) {
            Debug.LogWarning("[ShopManager] TryBuyAmmo: null reference detected!");
            return false;
        }

        bool success = AmmoManager.Instance.TryAddItem(itemData);

        if (success) {
            AmmoPurchased?.Invoke(itemData.ItemID, itemData.QuantityPerPurchase);
            CurrencyChanged?.Invoke();
            ItemStateChanged?.Invoke();
        }

        return success;
    }

    /// <summary>
    /// Checks if player can afford an item unlock.
    /// </summary>
    public bool CanAffordUnlock(ShopItemDataSO itemData) {
        if (itemData == null || EconomyManager.Instance == null) return false;
        return EconomyManager.Instance.CanAfford(itemData.UnlockCost);
    }

    /// <summary>
    /// Checks if player can afford an item upgrade.
    /// </summary>
    public bool CanAffordUpgrade(ShopItemDataSO itemData) {
        if (itemData == null || PlayerProgress.Instance == null || EconomyManager.Instance == null) return false;

        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);
        int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemData.ItemID);

        if (currentLevel >= maxLevel) return false;

        int cost = GetUpgradeCost(itemData, currentLevel);
        return cost > 0 && EconomyManager.Instance.CanAfford(cost);
    }

    /// <summary>
    /// Checks if player can afford ammo purchase.
    /// </summary>
    public bool CanAffordAmmo(ShopItemDataSO itemData) {
        if (itemData == null || PlayerProgress.Instance == null || EconomyManager.Instance == null) return false;

        if (itemData.ItemData is VestDataSO) {
            Vest vest = Vest.GetFromPlayer(playerCharacter);
            if (vest == null) return false;
            return vest.GetCurrentArmor() < vest.GetMaxArmor();
        }

        int currentAmount = PlayerProgress.Instance.GetWeaponReserveAmmo(itemData.ItemID);
        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);
        int maxAmount = PlayerProgress.Instance.GetMaxAmmoAtLevel(itemData.ItemID, currentLevel);
        int cost = itemData.CostPerPurchase;

        return currentAmount < maxAmount &&
               PlayerProgress.Instance.IsItemUnlocked(itemData.ItemID) &&
               EconomyManager.Instance.CanAfford(cost);
    }

    /// <summary>
    /// Gets current ammo status for an item.
    /// Uses smart dispatcher GetCurrentAmmoForItem() to return the correct current amount
    /// based on where that item type stores its data (weapons dict vs consumables dict).
    /// </summary>
    public (int current, int max) GetAmmoStatus(ShopItemDataSO itemData) {
        if (itemData == null || PlayerProgress.Instance == null) return (0, 0);

        // Vest: special case - uses armor system (percentage-based), not quantity-based
        if (itemData.ItemData is VestDataSO) {
            Vest vest = Vest.GetFromPlayer(playerCharacter);
            if (vest != null) {
                return ((int)vest.GetCurrentArmor(), (int)vest.GetMaxArmor());
            }
            return (0, 0);
        }

        // All other items: use smart dispatcher to route to correct storage location
        // Weapons → GetWeaponReserveAmmo()
        // Buildables/Consumables → GetConsumableQuantity()
        int current = PlayerProgress.Instance.GetCurrentAmmoForItem(itemData.ItemID);
        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);
        int max = PlayerProgress.Instance.GetMaxAmmoAtLevel(itemData.ItemID, currentLevel);
        return (current, max);
    }

    public ShopButtonDisabledReason GetActionButtonDisabledReason(ShopItemDataSO itemData) {
        if (itemData == null || PlayerProgress.Instance == null || EconomyManager.Instance == null) {
            return ShopButtonDisabledReason.None;
        }

        string itemID = itemData.ItemID;
        bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(itemID);
        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemID);
        int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemID);

        if (!isUnlocked) {
            return EconomyManager.Instance.CanAfford(itemData.UnlockCost) 
                ? ShopButtonDisabledReason.None 
                : ShopButtonDisabledReason.InsufficientFunds;
        }

        if (currentLevel >= maxLevel) {
            return ShopButtonDisabledReason.MaxLevel;
        }

        int cost = GetUpgradeCost(itemData, currentLevel);
        if (cost <= 0 || !EconomyManager.Instance.CanAfford(cost)) {
            return ShopButtonDisabledReason.InsufficientFunds;
        }

        return ShopButtonDisabledReason.None;
    }

    public ShopButtonDisabledReason GetAmmoButtonDisabledReason(ShopItemDataSO itemData) {
        if (itemData == null || PlayerProgress.Instance == null || EconomyManager.Instance == null) {
            return ShopButtonDisabledReason.None;
        }

        if (!PlayerProgress.Instance.IsItemUnlocked(itemData.ItemID)) {
            return ShopButtonDisabledReason.NotUnlocked;
        }

        if (itemData.ItemData is VestDataSO) {
            Vest vest = Vest.GetFromPlayer(playerCharacter);
            if (vest != null && vest.GetCurrentArmor() >= vest.GetMaxArmor()) {
                return ShopButtonDisabledReason.FullArmor;
            }
            return EconomyManager.Instance.CanAfford(itemData.CostPerPurchase)
                ? ShopButtonDisabledReason.None
                : ShopButtonDisabledReason.InsufficientFunds;
        }

        int currentAmount = PlayerProgress.Instance.GetWeaponReserveAmmo(itemData.ItemID);
        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);
        int maxAmount = PlayerProgress.Instance.GetMaxAmmoAtLevel(itemData.ItemID, currentLevel);

        if (currentAmount >= maxAmount) {
            return ShopButtonDisabledReason.FullAmmo;
        }

        return EconomyManager.Instance.CanAfford(itemData.CostPerPurchase)
            ? ShopButtonDisabledReason.None
            : ShopButtonDisabledReason.InsufficientFunds;
    }

    #endregion
}
