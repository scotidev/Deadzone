using UnityEngine;

/// <summary>
/// Defines the display data and economy settings for a shop item card.
/// Extended to support unlocks, upgrades, ammo purchases, and different item types.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemData", menuName = "Shop/Item Data")]
public class ShopItemDataSO : ScriptableObject {

    #region SERIALIZED FIELDS

    [Header("Identity")]
    [TextArea(2, 4)] private string itemDescription;

    [Header("Item Data Reference")]
    [SerializeField] private ItemDataSO itemData;

    [Header("Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject previewPrefab;

    [Header("Preview Configuration")]
    [SerializeField] private Vector3 previewScale = Vector3.one;
    [SerializeField] private Vector3 previewPositionOffset;
    [SerializeField] private Vector3 previewRotationOffset;

    [Header("Economy")]
    [SerializeField] private int unlockCost = 0;
    [SerializeField] private int baseUpgradeCost = 100;
    [Tooltip("Multiplier applied to baseUpgradeCost for each upgrade level")]
    [SerializeField] private float upgradeCostMultiplier = 1.5f;

    [Header("Purchase Settings")]
    [SerializeField] private int costPerPurchase = 50;
    [SerializeField] private int quantityPerPurchase = 30;

    #endregion

    #region PROPERTIES

    public string ItemID => itemData?.ItemID ?? string.Empty;
    public string ItemName => itemData?.ItemName ?? string.Empty;
    public string Description => itemDescription;
    public ItemDataSO ItemData => itemData;
    public Sprite Icon => icon;
    public GameObject PreviewPrefab => previewPrefab;
    public Vector3 PreviewScale => previewScale;
    public Vector3 PreviewPositionOffset => previewPositionOffset;
    public Vector3 PreviewRotationOffset => previewRotationOffset;
    public int UnlockCost => unlockCost;
    public int BaseUpgradeCost => baseUpgradeCost;
    public float UpgradeCostMultiplier => upgradeCostMultiplier;
    public int CostPerPurchase => costPerPurchase;
    public int QuantityPerPurchase => quantityPerPurchase;
    public int MaxAmmo => itemData?.MaxAmmo ?? 10;

    #endregion

    #region METHODS

    /// <summary>
    /// Calculates the upgrade cost for a specific level using exponential scaling.
    /// Formula: unlockCost + (baseUpgradeCost * (multiplier ^ currentLevel))
    /// Example with unlock=1000, base=100, mult=1.5:
    /// - Level 1→2: 1000 + 100*(1.5^1) = 1150
    /// - Level 2→3: 1000 + 100*(1.5^2) = 1225
    /// - Level 5→6: 1000 + 100*(1.5^5) = 1875
    /// </summary>
    /// <param name="currentLevel">Current upgrade level (1-based)</param>
    /// <returns>Cost for the next upgrade</returns>
    public int GetUpgradeCost(int currentLevel) {
        if (currentLevel < 1) currentLevel = 1;
        float exponentialCost = baseUpgradeCost * Mathf.Pow(upgradeCostMultiplier, currentLevel);
        return unlockCost + Mathf.RoundToInt(exponentialCost);
    }

    #endregion

    #region SETTERS

    /// <summary>
    /// Set the item description (for editor configuration only).
    /// </summary>
    public void SetItemDescription(string newDescription) {
        itemDescription = newDescription;
    }

    /// <summary>
    /// Set the item data reference (for editor configuration only).
    /// </summary>
    public void SetItemData(ItemDataSO newItemData) {
        itemData = newItemData;
    }

    /// <summary>
    /// Set the item icon (for editor configuration only).
    /// </summary>
    public void SetIcon(Sprite newIcon) {
        icon = newIcon;
    }

    /// <summary>
    /// Set the preview prefab (for editor configuration only).
    /// </summary>
    public void SetPreviewPrefab(GameObject newPrefab) {
        previewPrefab = newPrefab;
    }

    #endregion
}

