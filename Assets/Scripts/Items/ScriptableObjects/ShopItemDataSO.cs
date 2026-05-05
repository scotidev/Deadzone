using UnityEngine;

// REFATORAÇÃO: pq aquui temos itemID? pois temos também em ItemDataSO, eles se ligam de alguma forma? ou é reduntante? analise necessaria se precisamos unificar

/// <summary>
/// Defines the display data and economy settings for a shop item card.
/// Extended to support unlocks, upgrades, ammo purchases, and different item types.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemData", menuName = "Shop/Item Data")]
public class ShopItemDataSO : ScriptableObject {

    #region SERIALIZED FIELDS

    [Header("Identity")]
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [SerializeField][TextArea(2, 4)] private string itemDescription;

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

    [SerializeField] private int ammoCost = 50;
    [SerializeField] private int ammoAmountPerPurchase = 30;

    [Header("Exclusive")]
    [SerializeField] private int levelToUnlockExclusive = 9;
    [SerializeField] private int exclusiveUpgradeCost = 5000;

    [TextArea(2, 4)]
    [SerializeField] private string exclusivePowerDescription;

    #endregion

    #region PROPERTIES

    public string ItemID => itemID;
    public string ItemName => itemName;
    public string Description => itemDescription;
    public ItemDataSO ItemData => itemData;
    public Sprite Icon => icon;
    public GameObject PreviewPrefab => previewPrefab;
    public Vector3 PreviewScale => previewScale;
    public Vector3 PreviewPositionOffset => previewPositionOffset;
    public Vector3 PreviewRotationOffset => previewRotationOffset;
    public int UnlockCost => unlockCost;
    public int BaseUpgradeCost => baseUpgradeCost;
    public int AmmoCost => ammoCost;
    public int AmmoAmountPerPurchase => ammoAmountPerPurchase;
    public int LevelToUnlockExclusive => levelToUnlockExclusive;
    public int ExclusiveUpgradeCost => exclusiveUpgradeCost;
    public string ExclusivePowerDescription => exclusivePowerDescription;

    #endregion

    #region SETTERS

    /// <summary>
    /// Set the item ID (for editor configuration only).
    /// </summary>
    public void SetItemID(string newID) {
        itemID = newID;
    }

    /// <summary>
    /// Set the item name (for editor configuration only).
    /// </summary>
    public void SetItemName(string newName) {
        itemName = newName;
    }

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

