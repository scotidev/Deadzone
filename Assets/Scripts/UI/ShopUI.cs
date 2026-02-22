using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the shop UI including item cards and shop panel interactions.
/// </summary>
public class ShopUI : BaseUI {
    [Header("Shop Elements")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject shopItemCardPrefab;
    [SerializeField] private Button closeButton;

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
    /// Populates the shop with item cards (placeholders for now).
    /// </summary>
    private void PopulateShopItems() {
        if (itemsContainer == null || shopItemCardPrefab == null)
            return;

        ClearShopItems();

        // Create placeholder items for testing
        CreatePlaceholderItems();
    }

    /// <summary>
    /// Creates placeholder shop items for testing purposes.
    /// </summary>
    private void CreatePlaceholderItems() {
        CreateItemCard(null, "Pistol", 150, "High fire rate weapon");
        CreateItemCard(null, "Shotgun", 200, "Close range power");
        CreateItemCard(null, "AK-47", 300, "Long range precision");
        CreateItemCard(null, "Medkit", 80, "Reliable sidearm");
        CreateItemCard(null, "Grenades", 500, "Explosive area damage");
        CreateItemCard(null, "Wall", 50, "Restores 50 HP");
        CreateItemCard(null, "Explosive Barrel", 30, "Restores ammunition");
        CreateItemCard(null, "Landmines", 100, "Increases defense");
        CreateItemCard(null, "Special", 60, "Throwable explosive");
        CreateItemCard(null, "Armor", 75, "Increases movement speed");
    }

    /// <summary>
    /// Creates a single shop item card.
    /// </summary>
    /// <param name="icon">The item icon sprite.</param>
    /// <param name="name">The item name.</param>
    /// <param name="price">The item price.</param>
    /// <param name="description">The item description.</param>
    private void CreateItemCard(Sprite icon, string name, int price, string description) {
        GameObject cardObject = Instantiate(shopItemCardPrefab, itemsContainer);
        ShopItemCard card = cardObject.GetComponent<ShopItemCard>();

        if (card != null)
            card.Setup(icon, name, price, description);
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
        if (ShopInterface.Instance != null)
            ShopInterface.Instance.CloseShop();
    }
}
