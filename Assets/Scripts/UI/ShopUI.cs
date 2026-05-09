using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Manages the shop UI including item cards and shop panel interactions.
/// </summary>
public class ShopUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Preview")]
    [SerializeField] private ItemPreviewHandler previewHandler;

    [Header("Shop Elements")]
    [SerializeField] private RectTransform itemsContainer;
    [SerializeField] private ShopItemCard shopItemCardPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private TextMeshProUGUI selectedItemNameText;
    [SerializeField] private TextMeshProUGUI selectedItemDescriptionText;

    [Header("Dynamic Stats")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statBarPrefab;

    [Header("Ammo Button")]
    [SerializeField] private Button ammoButton;
    [SerializeField] private TextMeshProUGUI ammoPriceText;

    [Header("Action Button")]
    [SerializeField] private Button selectedItemActionButton;
    [SerializeField] private TextMeshProUGUI selectedItemActionButtonText;
    [SerializeField] private TextMeshProUGUI selectedItemPriceText;

    [SerializeField] private List<ShopItemDataSO> shopItems = new List<ShopItemDataSO>();

    #endregion

    #region FIELDS

    private ShopItemDataSO selectedItemData;
    private List<StatBarDisplay> activeStatBars = new List<StatBarDisplay>();

    #endregion

    #region EVENTS

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();
        BindButtons();
        SubscribeToCurrencyEvents();
    }

    private void OnDestroy() {
        UnsubscribeFromCurrencyEvents();
    }

    #endregion

    #region METHODS

    private void BindButtons() {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClick);

        if (ammoButton != null)
            ammoButton.onClick.AddListener(OnAmmoButtonPressed);

        if (selectedItemActionButton != null)
            selectedItemActionButton.onClick.AddListener(OnActionButtonPressed);
    }

    public override void Show() {
        base.Show();
        PopulateShopItems();
        SelectInitialItem();
        UpdateCurrencyDisplay();
    }

    public override void Hide() {
        base.Hide();
        if (previewHandler != null) {
            previewHandler.DestroyPreview();
        }
    }

    private void SubscribeToCurrencyEvents() {
        ShopManager.CurrencyChanged += OnCurrencyChanged;
    }

    private void UnsubscribeFromCurrencyEvents() {
        ShopManager.CurrencyChanged -= OnCurrencyChanged;
    }

    private void OnCurrencyChanged() {
        UpdateCurrencyDisplay();

        if (selectedItemData != null) {
            UpdateActionButton(selectedItemData, 0);
        }
    }

    private void UpdateCurrencyDisplay() {
        if (currencyText == null || EconomyManager.Instance == null) return;

        int currentCurrency = EconomyManager.Instance.GetCurrentCurrency();
        currencyText.text = $"${currentCurrency:N0}";
    }

    private void PopulateShopItems() {
        if (itemsContainer == null || shopItemCardPrefab == null) {
            Debug.LogWarning($"{nameof(ShopUI)} has missing references for items container or card prefab.", this);
            return;
        }

        ClearShopItems();

        if (shopItems == null || shopItems.Count == 0) {
            Debug.LogWarning($"{nameof(ShopUI)} has no configured shop items.", this);
            return;
        }

        for (int index = 0; index < shopItems.Count; index++) {
            ShopItemDataSO itemData = shopItems[index];

            if (itemData == null) {
                Debug.LogWarning($"{nameof(ShopUI)} has a null item entry at index {index}.", this);
                continue;
            }

            ShopItemCard card = Instantiate(shopItemCardPrefab, itemsContainer);
            card.SetCallbacks(HandleCardSelected);
            card.Setup(itemData);
        }
    }

    private void ClearShopItems() {
        selectedItemData = null;

        foreach (Transform child in itemsContainer) {
            Destroy(child.gameObject);
        }
    }

    private void SelectInitialItem() {
        ShopItemDataSO fallbackItem = null;

        for (int index = 0; index < shopItems.Count; index++) {
            ShopItemDataSO itemData = shopItems[index];
            if (itemData == null) {
                continue;
            }

            if (fallbackItem == null) {
                fallbackItem = itemData;
            }

            if (itemData.ItemData is WeaponDataSO) {
                HandleCardSelected(itemData);
                return;
            }
        }

        if (fallbackItem != null) {
            HandleCardSelected(fallbackItem);
        } else {
            Debug.LogWarning($"{nameof(ShopUI)}.{nameof(SelectInitialItem)}: No valid shop items found.", this);
        }
    }

    private void HandleCardSelected(ShopItemDataSO itemData) {
        selectedItemData = itemData;
        UpdateSelectedItemInfo();

        if (previewHandler != null && itemData != null) {
            previewHandler.ShowItem(itemData);
        }

        ShopManager.Instance?.OnShopInteraction();
    }

    private void UpdateSelectedItemInfo() {
        if (selectedItemData == null) {
            SetSelectedInfoTexts(string.Empty, string.Empty);
            ClearStatBars();
            UpdateActionButton(null, 0);
            ClearAmmoDisplay();
            return;
        }

        string itemName = selectedItemData.ItemName;
        string description = selectedItemData.Description;

        SetSelectedInfoTexts(itemName, description);
        BuildDynamicStats();
        UpdateAmmoDisplay(selectedItemData.ItemID);
        UpdateActionButton(selectedItemData, 0);
    }

    private void SetSelectedInfoTexts(string itemName, string description) {
        if (selectedItemNameText != null) selectedItemNameText.text = itemName;
        if (selectedItemDescriptionText != null) selectedItemDescriptionText.text = description;
    }

    private void BuildDynamicStats() {
        foreach (Transform child in statsContainer) {
            Destroy(child.gameObject);
        }

        activeStatBars.Clear();

        if (selectedItemData?.ItemData == null || statsContainer == null || statBarPrefab == null) {
            return;
        }

        ItemDataSO itemData = selectedItemData.ItemData;
        string[] labels = itemData.GetStatLabels();

        int currentLevel = PlayerProgress.Instance != null
            ? PlayerProgress.Instance.GetItemLevel(selectedItemData.ItemID)
            : 1;

        int maxLevel = PlayerProgress.Instance != null
            ? PlayerProgress.Instance.GetItemMaxLevel(selectedItemData.ItemID)
            : 10;

        bool isUnlocked = PlayerProgress.Instance != null && PlayerProgress.Instance.IsItemUnlocked(selectedItemData.ItemID);

        int nextLevel = (currentLevel >= maxLevel) ? currentLevel : currentLevel + 1;

        float[] currentValues = itemData.GetStatValues(currentLevel);
        float[] nextValues = isUnlocked ? itemData.GetStatValues(nextLevel) : new float[currentValues.Length]; // If locked, upgrade shows 0 progress

        // Force calculation of global max values to ensure they're ready
        WeaponStatsCalculator.CalculateGlobalMaxValues();

        for (int i = 0; i < labels.Length && i < 3; i++) {
            GameObject barObj = Instantiate(statBarPrefab, statsContainer);
            RectTransform rt = barObj.GetComponent<RectTransform>();
            StatBarDisplay bar = barObj.GetComponent<StatBarDisplay>();

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 35f);

            float maxValue = WeaponStatsCalculator.GetMaxValueForStat(labels[i]);

            bar.Setup(labels[i], maxValue, i);
            // Show upgrade bar only if item is unlocked, otherwise hide it
            bar.SetValues(currentValues[i], nextValues[i], isUnlocked);

            activeStatBars.Add(bar);
        }

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(statsContainer as RectTransform);
    }

    private void ClearStatBars() {
        foreach (var bar in activeStatBars) {
            if (bar != null) Destroy(bar.gameObject);
        }
        activeStatBars.Clear();
    }

    private void UpdateAmmoDisplay(string itemID) {
        if (string.IsNullOrEmpty(itemID) || selectedItemData == null || ShopManager.Instance == null) {
            ClearAmmoDisplay();
            return;
        }

        var (current, max) = ShopManager.Instance.GetAmmoStatus(selectedItemData);

        if (current >= max) {
            if (ammoPriceText != null) ammoPriceText.text = "FULL";
            if (ammoButton != null) ammoButton.interactable = false;
        } else {
            int cost = selectedItemData.CostPerPurchase;
            if (ammoPriceText != null) ammoPriceText.text = $"${cost:N0}";
            if (ammoButton != null) ammoButton.interactable = ShopManager.Instance.CanAffordAmmo(selectedItemData);
        }
    }

    private void ClearAmmoDisplay() {
        if (ammoPriceText != null) ammoPriceText.text = string.Empty;
        if (ammoButton != null) ammoButton.interactable = false;
    }

    private void UpdateActionButton(ShopItemDataSO itemData, int dummy) {
        if (selectedItemActionButton == null || itemData == null) {
            return;
        }

        if (PlayerProgress.Instance == null || ShopManager.Instance == null) {
            selectedItemActionButton.interactable = false;
            return;
        }

        string itemID = itemData.ItemID;
        bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(itemID);
        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemID);
        int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemID);
        bool isMaxLevel = currentLevel >= maxLevel;

        if (!isUnlocked) {
            int cost = itemData.UnlockCost;
            if (selectedItemActionButtonText != null)
                selectedItemActionButtonText.text = "Unlock";

            if (selectedItemPriceText != null)
                selectedItemPriceText.text = $"${cost:N0}";

            selectedItemActionButton.interactable = ShopManager.Instance.CanAffordUnlock(itemData);
        } else if (isMaxLevel) {
            if (selectedItemActionButtonText != null)
                selectedItemActionButtonText.text = "Maxed Out";

            selectedItemActionButton.interactable = false;

            if (selectedItemPriceText != null)
                selectedItemPriceText.text = string.Empty;
        } else {
            int cost = ShopManager.Instance.GetUpgradeCost(itemData, currentLevel);
            if (selectedItemActionButtonText != null)
                selectedItemActionButtonText.text = "Upgrade";

            if (selectedItemPriceText != null)
                selectedItemPriceText.text = cost > 0 ? $"${cost:N0}" : string.Empty;

            selectedItemActionButton.interactable = cost > 0 && ShopManager.Instance.CanAffordUpgrade(itemData);
        }

        UpdateAmmoDisplay(itemID);
    }

    private void OnActionButtonPressed() {
        if (selectedItemData == null || ShopManager.Instance == null) {
            return;
        }

        string itemID = selectedItemData.ItemID;
        bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(itemID);
        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemID);
        int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemID);

        if (!isUnlocked) {
            OnRightPanelUnlock(selectedItemData);
        } else if (currentLevel < maxLevel) {
            OnRightPanelUpgrade(selectedItemData);
        }
    }

    private void OnRightPanelUnlock(ShopItemDataSO itemData) {
        if (itemData == null || ShopManager.Instance == null) {
            return;
        }

        if (ShopManager.Instance.TryUnlockItem(itemData)) {
            selectedItemActionButton?.GetComponent<UIButtonFeedback>()?.PlayUnlockSound();
            ShopManager.Instance.OnPurchaseMade();
            ShopManager.Instance.OnShopInteraction();
            RefreshAllCards();
        }
    }

    private void OnRightPanelUpgrade(ShopItemDataSO itemData) {
        if (itemData == null || ShopManager.Instance == null) {
            return;
        }

        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);
        int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemData.ItemID);
        bool reachedMax = currentLevel >= maxLevel - 1;

        if (ShopManager.Instance.TryUpgradeItem(itemData)) {
            UIButtonFeedback feedback = selectedItemActionButton?.GetComponent<UIButtonFeedback>();
            if (reachedMax) {
                feedback?.PlayMaxedOutSound();
            } else {
                feedback?.PlayUpgradeSound();
            }
            ShopManager.Instance.OnPurchaseMade();
            ShopManager.Instance.OnShopInteraction();
            RefreshAllCards();
        }
    }

    private void OnAmmoButtonPressed() {
        if (selectedItemData == null || ShopManager.Instance == null) return;

        if (ShopManager.Instance.TryBuyAmmo(selectedItemData)) {
            PlayAmmoSound(selectedItemData);
            ShopManager.Instance.OnPurchaseMade();
            ShopManager.Instance.OnShopInteraction();
            UpdateSelectedItemInfo();
        }
    }

    private void PlayAmmoSound(ShopItemDataSO itemData) {
        if (itemData?.ItemData == null) return;

        UIButtonFeedback feedback = ammoButton?.GetComponent<UIButtonFeedback>();
        if (feedback == null) return;

        if (itemData.ItemData is VestDataSO) {
            feedback.PlayVestClickSound();
        }
        else if (itemData.ItemData is WeaponDataSO) {
            feedback.PlayAmmoClickSound();
        }
        else {
            feedback.PlaySuppliesClickSound();
        }
    }

    private void RefreshAllCards() {
        foreach (Transform child in itemsContainer) {
            ShopItemCard card = child.GetComponent<ShopItemCard>();
            if (card != null) {
                card.RefreshCardState();
            }
        }

        UpdateSelectedItemInfo();
    }

    private void OnCloseClick() {
        if (ShopManager.Instance != null)
            ShopManager.Instance.CloseShop();
    }

    #endregion
}