using UnityEngine;

/// <summary>
/// Defines the display data and economy settings for a shop item card.
/// Extended to support unlocks, upgrades, ammo purchases, and different item types.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemData", menuName = "Shop/Item Data")]
public class ShopItemDataSO : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Unique identifier for this item (must match ItemDataSO itemID or PlayerProgress keys).")]
    [SerializeField] private string itemID;

    [SerializeField] private string itemName;

    [SerializeField][TextArea(2, 4)] private string itemDescription;

    [Header("Item Data Reference")]
    [SerializeField] private ItemDataSO itemData;

    [Header("Visual")]
    [SerializeField] private Sprite icon;

    [Tooltip("Prefab used in the shop 3D preview.")]
    [SerializeField] private GameObject previewPrefab;

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


    #region Property Getters

    public string ItemID => itemID;

    public string ItemName => itemName;

    public string Description => itemDescription;

    public ItemDataSO ItemData => itemData;

    public Sprite Icon => icon;

    public GameObject PreviewPrefab => previewPrefab;

    public int UnlockCost => unlockCost;

    public int BaseUpgradeCost => baseUpgradeCost;

    public int AmmoCost => ammoCost;

    public int AmmoAmountPerPurchase => ammoAmountPerPurchase;

    public int LevelToUnlockExclusive => levelToUnlockExclusive;

    public int ExclusiveUpgradeCost => exclusiveUpgradeCost;

    public string ExclusivePowerDescription => exclusivePowerDescription;

    #endregion
}

