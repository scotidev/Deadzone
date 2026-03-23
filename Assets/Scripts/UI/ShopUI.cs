using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the shop UI including item cards and shop panel interactions.
/// </summary>
public class ShopUI : BaseUI {
    [Header("Shop Elements")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemCard shopItemCardPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();

    protected override void Awake() {
        base.Awake();
        BindButtons();
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
