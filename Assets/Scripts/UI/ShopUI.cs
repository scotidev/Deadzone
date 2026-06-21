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

    #region STATIC

    public static ShopUI Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Preview")]
    [SerializeField] private ItemPreviewHandler previewHandler;

    [Header("Shop Elements")]
    [SerializeField] private RectTransform itemsContainer;
    [SerializeField] private ShopItemCard shopItemCardPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private TextMeshProUGUI selectedItemNameText;

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
    private ShopItemCard currentSelectedCard;

    #endregion

    #region PROPERTIES

    public ShopItemDataSO SelectedItemData => selectedItemData;

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();
        Instance = this;
        BindButtons();
    }

    protected override void Start() {
        base.Start();
        SubscribeToCurrencyEvents();
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
        UnsubscribeFromCurrencyEvents();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Binds all button click events to their respective handlers.
    /// </summary>
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
        currentSelectedCard = null;
        PopulateShopItems();
        SelectInitialItem();
        UpdateCurrencyDisplay();
    }

    public override void Hide() {
        base.Hide();
        currentSelectedCard = null;
        if (previewHandler != null) {
            previewHandler.DestroyPreview();
        }
    }

    private void SubscribeToCurrencyEvents() {
        EconomyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
    }

    private void UnsubscribeFromCurrencyEvents() {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
    }

    /// <summary>
    /// Updates currency display and action button state when currency changes.
    /// </summary>
    private void OnCurrencyChanged(int newAmount) {
        UpdateCurrencyDisplay();

        if (selectedItemData != null) {
            UpdateActionButton(selectedItemData, 0);
        }
    }

    /// <summary>
    /// Updates the currency display text with current amount.
    /// </summary>
    private void UpdateCurrencyDisplay() {
        if (currencyText == null || EconomyManager.Instance == null) return;

        int currentCurrency = EconomyManager.Instance.GetCurrentCurrency();
        currencyText.text = $"${currentCurrency:N0}";

        currencyText.GetComponent<TextScalePulse>()?.Pulse();
    }

    /// <summary>
    /// Populates the shop items container with cards for each configured item.
    /// </summary>
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

    /// <summary>
    /// Clears all shop item cards from the container.
    /// </summary>
    private void ClearShopItems() {
        selectedItemData = null;

        foreach (Transform child in itemsContainer) {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Selects the first weapon item, or falls back to the first valid item.
    /// </summary>
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

    /// <summary>
    /// Handles card selection, updating the selected item and preview.
    /// </summary>
    private void HandleCardSelected(ShopItemDataSO itemData) {
        if (currentSelectedCard != null) {
            currentSelectedCard.SetSelected(false);
        }

        selectedItemData = itemData;
        UpdateSelectedItemInfo();

        ShopItemCard newCard = itemsContainer.GetChild(
            shopItems.IndexOf(itemData)
        ).GetComponent<ShopItemCard>();

        if (newCard != null) {
            currentSelectedCard = newCard;
            currentSelectedCard.SetSelected(true);
        }

        if (previewHandler != null && itemData != null) {
            previewHandler.ShowItem(itemData);
        }

        ShopManager.Instance?.OnShopInteraction();
    }

    /// <summary>
    /// Updates the selected item info panel with name, stats, and buttons.
    /// </summary>
    private void UpdateSelectedItemInfo() {
        if (selectedItemData == null) {
            SetSelectedInfoTexts(string.Empty);
            ClearStatBars();
            UpdateActionButton(null, 0);
            ClearAmmoDisplay();
            return;
        }

        string itemName = selectedItemData.ItemName;

        SetSelectedInfoTexts(itemName);
        BuildDynamicStats();
        UpdateAmmoDisplay(selectedItemData.ItemID);
        UpdateActionButton(selectedItemData, 0);
    }

    /// <summary>
    /// Sets the selected item name text.
    /// </summary>
    private void SetSelectedInfoTexts(string itemName) {
        if (selectedItemNameText != null) selectedItemNameText.text = itemName;
    }

    /// <summary>
    /// Builds stat bars for the selected item's current and next level values.
    /// </summary>
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
        float[] nextValues = isUnlocked ? itemData.GetStatValues(nextLevel) : new float[currentValues.Length];

        WeaponStatsCalculator.CalculateGlobalMaxValues(shopItems);

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
            bar.SetValues(currentValues[i], nextValues[i], isUnlocked);

            activeStatBars.Add(bar);
        }

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(statsContainer as RectTransform);
    }

    /// <summary>
    /// Clears all active stat bars.
    /// </summary>
    private void ClearStatBars() {
        foreach (var bar in activeStatBars) {
            if (bar != null) Destroy(bar.gameObject);
        }
        activeStatBars.Clear();
    }

    /// <summary>
    /// Updates the ammo display for the selected item.
    /// </summary>
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

    /// <summary>
    /// Clears the ammo display.
    /// </summary>
    private void ClearAmmoDisplay() {
        if (ammoPriceText != null) ammoPriceText.text = string.Empty;
        if (ammoButton != null) ammoButton.interactable = false;
    }

    /// <summary>
    /// Updates the action button label, price, and interactability based on item state.
    /// </summary>
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

    /// <summary>
    /// Handles the action button press, dispatching unlock or upgrade.
    /// </summary>
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

    /// <summary>
    /// Handles the unlock operation from the right panel action button.
    /// </summary>
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

    /// <summary>
    /// Handles the upgrade operation from the right panel action button.
    /// </summary>
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

    /// <summary>
    /// Handles the ammo purchase button press.
    /// </summary>
    private void OnAmmoButtonPressed() {
        if (selectedItemData == null || ShopManager.Instance == null) return;

        if (ShopManager.Instance.TryBuyAmmo(selectedItemData)) {
            PlayAmmoSound(selectedItemData);
            ShopManager.Instance.OnPurchaseMade();
            ShopManager.Instance.OnShopInteraction();
            UpdateSelectedItemInfo();
        }
    }

    /// <summary>
    /// Plays the appropriate sound for the ammo type being purchased.
    /// </summary>
    private void PlayAmmoSound(ShopItemDataSO itemData) {
        if (itemData?.ItemData == null) return;

        UIButtonFeedback feedback = ammoButton?.GetComponent<UIButtonFeedback>();
        if (feedback == null) return;

        if (itemData.ItemData is VestDataSO) {
            feedback.PlayVestClickSound();
        } else if (itemData.ItemData is WeaponDataSO) {
            feedback.PlayAmmoClickSound();
        } else {
            feedback.PlaySuppliesClickSound();
        }
    }

    /// <summary>
    /// Refreshes all card visuals and updates the selected item info.
    /// </summary>
    private void RefreshAllCards() {
        foreach (Transform child in itemsContainer) {
            ShopItemCard card = child.GetComponent<ShopItemCard>();
            if (card != null) {
                card.RefreshCardState();
                card.SetSelected(card == currentSelectedCard);
            }
        }

        UpdateSelectedItemInfo();
    }

    /// <summary>
    /// Handles the close button click, closing the shop.
    /// </summary>
    private void OnCloseClick() {
        if (ShopManager.Instance != null)
            ShopManager.Instance.CloseShop();
    }

    #endregion
}
