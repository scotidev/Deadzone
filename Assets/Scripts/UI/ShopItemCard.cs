using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents an individual shop item card with icon, name, price, and description.
/// </summary>
public class ShopItemCard : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemPriceText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Button purchaseButton;

    private int itemPrice;
    private string itemName;

    private void Awake()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(OnPurchaseClick);
    }

    /// <summary>
    /// Sets up the shop item card with all necessary data.
    /// </summary>
    /// <param name="icon">The item icon sprite.</param>
    /// <param name="name">The item name.</param>
    /// <param name="price">The item price.</param>
    /// <param name="description">The item description.</param>
    public void Setup(Sprite icon, string name, int price, string description)
    {
        itemName = name;
        itemPrice = price;

        SetItemIcon(icon);
        SetItemName(name);
        SetItemPrice(price);
        SetItemDescription(description);
    }

    /// <summary>
    /// Sets the item icon sprite.
    /// </summary>
    /// <param name="icon">The sprite to display.</param>
    private void SetItemIcon(Sprite icon)
    {
        if (itemIcon != null)
            itemIcon.sprite = icon;
    }

    /// <summary>
    /// Sets the item name text.
    /// </summary>
    /// <param name="name">The item name.</param>
    private void SetItemName(string name)
    {
        if (itemNameText != null)
            itemNameText.text = name;
    }

    /// <summary>
    /// Sets the item price text.
    /// </summary>
    /// <param name="price">The item price.</param>
    private void SetItemPrice(int price)
    {
        if (itemPriceText != null)
            itemPriceText.text = $"${price}";
    }

    /// <summary>
    /// Sets the item description text.
    /// </summary>
    /// <param name="description">The item description.</param>
    private void SetItemDescription(string description)
    {
        if (itemDescriptionText != null)
            itemDescriptionText.text = description;
    }

    /// <summary>
    /// Handles the purchase button click event.
    /// </summary>
    private void OnPurchaseClick()
    {
        // TODO: Implement purchase logic
    }
}
