using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Manages the shop UI including item cards and shop panel interactions.
/// </summary>
public class ShopUI : BaseUI {
    [Header("Shop Elements")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemCard shopItemCardPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("Currency Display")]
    [Tooltip("Text component to display player's current currency in the shop.")]
    [SerializeField] private TextMeshProUGUI currencyText;

    protected override void Awake() {
        base.Awake();
        BindButtons();
        SubscribeToCurrencyEvents();
    }

    /// <summary>
    /// Binds button click events to their handlers.
    /// </summary>
    private void BindButtons() {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClick);
    }

    /// <summary>
    /// Shows the shop panel and populates items.
    /// </summary>
    public override void Show() {
        base.Show();
        PopulateShopItems();
        UpdateCurrencyDisplay();
    }

    /// <summary>
    /// Subscribes to EconomyManager events to update currency display.
    /// </summary>
    private void SubscribeToCurrencyEvents() {
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }
    }

    /// <summary>
    /// Called when the component is destroyed.
    /// Unsubscribes from events to prevent memory leaks.
    /// </summary>
    private void OnDestroy() {
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }
    }

    /// <summary>
    /// Event handler for currency changes.
    /// Updates the currency display text.
    /// </summary>
    /// <param name="newAmount">The new currency amount.</param>
    private void OnCurrencyChanged(int newAmount) {
        UpdateCurrencyDisplay();
    }

    /// <summary>
    /// Updates the currency display text in the shop UI.
    /// </summary>
    private void UpdateCurrencyDisplay() {
        if (currencyText == null || EconomyManager.Instance == null) return;

        int currentCurrency = EconomyManager.Instance.GetCurrentCurrency();
        currencyText.text = $"${currentCurrency:N0}";
    }

    /// <summary>
    /// Populates the shop with item cards based on the inspector item list.
    /// </summary>
    private void PopulateShopItems() {
        if (itemsContainer == null || shopItemCardPrefab == null)
        {
            Debug.LogWarning($"{nameof(ShopUI)} has missing references for items container or card prefab.", this);
            return;
        }

        ClearShopItems();
        CreateConfiguredItems();
    }

    /// <summary>
    /// Creates card instances from the configured inspector data.
    /// </summary>
    private void CreateConfiguredItems() {
        if (shopItems == null || shopItems.Count == 0)
        {
            Debug.LogWarning($"{nameof(ShopUI)} has no configured shop items.", this);
            return;
        }

        for (int index = 0; index < shopItems.Count; index++)
        {
            ShopItemData itemData = shopItems[index];
            if (itemData == null)
            {
                Debug.LogWarning($"{nameof(ShopUI)} has a null item entry at index {index}.", this);
                continue;
            }

            ShopItemCard card = Instantiate(shopItemCardPrefab, itemsContainer);
            card.Setup(itemData);
        }
    }

    /// <summary>
    /// Clears all existing shop item cards from the container.
    /// </summary>
    private void ClearShopItems() {
        foreach (Transform child in itemsContainer) {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Handles the close button click event.
    /// </summary>
    private void OnCloseClick() {
        if (ShopManager.Instance != null)
            ShopManager.Instance.CloseShop();
    }
}
