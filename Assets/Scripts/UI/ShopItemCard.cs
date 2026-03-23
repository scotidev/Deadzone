using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents an individual shop item card with icon, name, price, and description.
/// </summary>
public class ShopItemCard : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDamageText;
    [SerializeField] private TextMeshProUGUI itemFireRateText;
    [SerializeField] private TextMeshProUGUI itemAmmoCapacityText;
    [SerializeField] private TextMeshProUGUI itemPriceText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Button purchaseButton;

    private ShopItemData currentItemData;

    private void Awake() {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(OnPurchaseClick);
    }

    /// <summary>
    /// Sets up the shop item card with all necessary data.
    /// </summary>
    /// <param name="itemData">Item data asset to render on this card.</param>
    public void Setup(ShopItemData itemData) {
        currentItemData = itemData;

        if (currentItemData == null) {
            Debug.LogWarning($"{nameof(ShopItemCard)} received null item data.", this);
            return;
        }

        SetItemIcon(currentItemData.Icon);
        SetItemName(currentItemData.ItemName);
        SetItemDamage(currentItemData.Damage);
        SetItemFireRate(currentItemData.FireRate);
        SetItemAmmoCapacity(currentItemData.AmmoCapacity);
        SetItemPrice(currentItemData.Price);
        SetItemDescription(currentItemData.Description);
    }

    /// <summary>
    /// Sets the item icon sprite.
    /// </summary>
    /// <param name="icon">The sprite to display.</param>
    private void SetItemIcon(Sprite icon) {
        if (itemIcon != null)
            itemIcon.sprite = icon;
    }

    /// <summary>
    /// Sets the item name text.
    /// </summary>
    /// <param name="name">The item name.</param>
    private void SetItemName(string name) {
        if (itemNameText != null)
            itemNameText.text = name;
    }

    /// <summary>
    /// Sets the item damage text.
    /// </summary>
    /// <param name="damage">The item damage value.</param>
    private void SetItemDamage(float damage) {
        if (itemDamageText != null)
            itemDamageText.text = $"Damage: {damage:0.##}";
    }

    /// <summary>
    /// Sets the item fire-rate text.
    /// </summary>
    /// <param name="fireRate">The item fire-rate value.</param>
    private void SetItemFireRate(float fireRate) {
        if (itemFireRateText != null)
            itemFireRateText.text = $"FireRate: {fireRate:0.##}";
    }

    /// <summary>
    /// Sets the item ammo capacity text.
    /// </summary>
    /// <param name="ammoCapacity">The item ammo capacity value.</param>
    private void SetItemAmmoCapacity(int ammoCapacity) {
        if (itemAmmoCapacityText != null)
            itemAmmoCapacityText.text = $"Ammo: {ammoCapacity}";
    }

    /// <summary>
    /// Sets the item price text.
    /// </summary>
    /// <param name="price">The item price.</param>
    private void SetItemPrice(int price) {
        if (itemPriceText != null)
            itemPriceText.text = $"${price}";
    }

    /// <summary>
    /// Sets the item description text.
    /// </summary>
    /// <param name="description">The item description.</param>
    private void SetItemDescription(string description) {
        if (itemDescriptionText != null)
            itemDescriptionText.text = description;
    }

    /// <summary>
    /// Handles the purchase button click event.
    /// </summary>
    private void OnPurchaseClick() {
        // TODO: Implement purchase logic
    }
}
