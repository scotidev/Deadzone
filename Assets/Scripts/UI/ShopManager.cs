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
    public static event Action<bool> ShopClosed;
    public static event Action PlayerAFK;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

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

    /// <summary>
    /// Starts the coroutine that tracks AFK time in the shop.
    /// </summary>
    private void StartAFKTimer() {
        if (afkCoroutine != null) StopCoroutine(afkCoroutine);
        afkCoroutine = StartCoroutine(AFKTimerCoroutine());
    }

    /// <summary>
    /// Stops the AFK timer coroutine and resets the timer.
    /// </summary>
    private void StopAFKTimer() {
        if (afkCoroutine != null) {
            StopCoroutine(afkCoroutine);
            afkCoroutine = null;
        }
        afkTimer = 0f;
    }

    /// <summary>
    /// Coroutine that tracks how long the player has been idle in the shop.
    /// Invokes PlayerAFK event when the threshold is exceeded.
    /// </summary>
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

    /// <summary>
    /// Called when the player interacts with the shop, resetting the AFK timer.
    /// </summary>
    public void OnShopInteraction() {
        afkTimer = 0f;
    }

    /// <summary>
    /// Called when a purchase is made in the shop.
    /// </summary>
    public void OnPurchaseMade() {
        hasPurchasedSomething = true;
    }

    /// <summary>
    /// Returns the current state of the shop interface.
    /// </summary>
    public bool IsShopOpen() => isShopOpen;

    /// <summary>
    /// Notifies the specific item that was unlocked. Only notifies the item matching the unlocked ID.
    /// </summary>
    private void NotifyItemUnlocked(ShopItemDataSO unlockedItem) {
        if (playerCharacter == null || unlockedItem?.ItemData == null) {
            return;
        }

        var callbacks = playerCharacter.GetComponents<IShopItemCallback>();

        foreach (var callback in callbacks) {
            if (callback is MonoBehaviour mono && mono.GetComponent<InfimaGames.LowPolyShooterPack.ItemBehaviour>() is { } itemBehaviour) {
                if (itemBehaviour.GetItemID() == unlockedItem.ItemID) {
                    callback.OnShopUnlock();
                }
            } else if (callback is Vest vest && vest.GetItemID() == unlockedItem.ItemID) {
                callback.OnShopUnlock();
            }
        }
    }

    /// <summary>
    /// Notifies the specific item that was upgraded. Only notifies the item matching the upgraded ID.
    /// </summary>
    private void NotifyItemUpgraded(ShopItemDataSO upgradedItem) {
        if (playerCharacter == null || upgradedItem?.ItemData == null) return;

        var callbacks = playerCharacter.GetComponents<IShopItemCallback>();

        foreach (var callback in callbacks) {
            if (callback is MonoBehaviour mono && mono.GetComponent<InfimaGames.LowPolyShooterPack.ItemBehaviour>() is { } itemBehaviour) {
                if (itemBehaviour.GetItemID() == upgradedItem.ItemID) {
                    callback.OnShopUpgrade();
                }
            } else if (callback is Vest vest && vest.GetItemID() == upgradedItem.ItemID) {
                callback.OnShopUpgrade();
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
            Debug.LogWarning($"[ShopManager] Insufficient funds! Need {missingAmount} more coins.");
            return false;
        }

        PlayerProgress.Instance.UnlockItem(itemData);

        ItemUnlocked?.Invoke(itemData.ItemID);

        NotifyItemUnlocked(itemData);
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

        if (cost <= 0 || !EconomyManager.Instance.CanAfford(cost)) {
            int missingAmount = cost - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopManager] Insufficient funds! Need {missingAmount} more coins.");
            return false;
        }

        if (!UpgradeManager.Instance.TryUpgradeItem(itemData.ItemID, cost, itemData.ItemData)) {
            return false;
        }

        NotifyItemUpgraded(itemData);
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
    /// Delegates to AmmoManager which handles all item types.
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
    /// based on where that item type stores its data.
    /// </summary>
    public (int current, int max) GetAmmoStatus(ShopItemDataSO itemData) {
        if (itemData == null || PlayerProgress.Instance == null) return (0, 0);

        if (itemData.ItemData is VestDataSO) {
            Vest vest = Vest.GetFromPlayer(playerCharacter);
            if (vest != null) {
                return ((int)vest.GetCurrentArmor(), (int)vest.GetMaxArmor());
            }
            return (0, 0);
        }

        int current = PlayerProgress.Instance.GetCurrentAmmoForItem(itemData.ItemID);
        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);
        int max = PlayerProgress.Instance.GetMaxAmmoAtLevel(itemData.ItemID, currentLevel);
        return (current, max);
    }

    /// <summary>
    /// Determines why the action button should be disabled for a given item.
    /// </summary>
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

    /// <summary>
    /// Determines why the ammo button should be disabled for a given item.
    /// </summary>
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
