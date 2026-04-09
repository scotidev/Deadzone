using UnityEngine;

/// <summary>
/// Defines the display data and economy settings for a shop item card.
/// Extended to support unlocks, upgrades, ammo purchases, and different item types.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemData", menuName = "Shop/Item Data")]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this item (must match WeaponDataSO weaponID or PlayerProgress keys).")]
    [SerializeField] private string itemID;
    
    [SerializeField] private string itemName;
    [SerializeField][TextArea(2, 4)] private string description;

    [Header("Visual")]
    [SerializeField] private Sprite icon;

    [Header("3D Preview")]
    [Tooltip("Optional lightweight prefab used in the shop right-side 3D preview.")]
    [SerializeField] private GameObject previewPrefab;

    [Header("Item Type")]
    [Tooltip("Is this a weapon (Pistol, SMG, etc.) or a utility item (Medkit, Grenades)?")]
    [SerializeField] private bool isWeapon = true;
    
    [Tooltip("Is this a buildable item (Barricades, Explosive Barrels, Traps)?")]
    [SerializeField] private bool isBuildable = false;
    
    [Tooltip("Is this the vest/armor item?")]
    [SerializeField] private bool isVest = false;

    [Header("Stats Display (for UI)")]
    [Tooltip("Damage stat shown in the card (actual values come from WeaponDataSO).")]
    [SerializeField] private float damageDisplay;
    
    [Tooltip("Fire rate stat shown in the card.")]
    [SerializeField] private float fireRateDisplay;
    
    [Tooltip("Ammo capacity stat shown in the card.")]
    [SerializeField] private int ammoCapacityDisplay;

    [Header("Economy - Unlock")]
    [Tooltip("Cost to unlock this item for the first time. Set to 0 for default-unlocked items like Pistol.")]
    [SerializeField] private int unlockCost = 0;

    [Header("Economy - Upgrade")]
    [Tooltip("Base cost for the first upgrade (level 1→2). Scales exponentially for higher levels.")]
    [SerializeField] private int baseUpgradeCost = 100;

    [Header("Economy - Ammo/Repair")]
    [Tooltip("Cost to buy one ammo pack or repair the vest.")]
    [SerializeField] private int ammoCost = 50;
    
    [Tooltip("Amount of ammo given per purchase.")]
    [SerializeField] private int ammoAmountPerPurchase = 30;
    
    [Tooltip("For buildables: cost per unit. For vest: cost per repair.")]
    [SerializeField] private int unitCost = 100;

    [Header("Data References")]
    [Tooltip("Reference to the weapon's data asset (only for weapons).")]
    [SerializeField] private WeaponDataSO weaponData;

    #region Property Getters

    /// <summary>Returns the unique item identifier.</summary>
    public string ItemID => itemID;
    
    /// <summary>Returns the item display name.</summary>
    public string ItemName => itemName;

    /// <summary>Returns the item short description.</summary>
    public string Description => description;

    /// <summary>Returns the item icon sprite.</summary>
    public Sprite Icon => icon;

    /// <summary>Returns the optional prefab used for 3D preview rendering in the shop.</summary>
    public GameObject PreviewPrefab => previewPrefab;

    /// <summary>Returns whether this is a weapon.</summary>
    public bool IsWeapon => isWeapon;
    
    /// <summary>Returns whether this is a buildable item.</summary>
    public bool IsBuildable => isBuildable;
    
    /// <summary>Returns whether this is the vest.</summary>
    public bool IsVest => isVest;

    /// <summary>Returns the damage stat for display.</summary>
    public float DamageDisplay => damageDisplay;

    /// <summary>Returns the fire-rate stat for display.</summary>
    public float FireRateDisplay => fireRateDisplay;

    /// <summary>Returns the ammo capacity stat for display.</summary>
    public int AmmoCapacityDisplay => ammoCapacityDisplay;

    /// <summary>Returns the cost to unlock this item.</summary>
    public int UnlockCost => unlockCost;

    /// <summary>Returns the base cost for upgrades.</summary>
    public int BaseUpgradeCost => baseUpgradeCost;

    /// <summary>Returns the cost to buy ammo.</summary>
    public int AmmoCost => ammoCost;
    
    /// <summary>Returns the amount of ammo per purchase.</summary>
    public int AmmoAmountPerPurchase => ammoAmountPerPurchase;
    
    /// <summary>Returns the cost per buildable unit or vest repair.</summary>
    public int UnitCost => unitCost;

    /// <summary>Returns the weapon data asset reference.</summary>
    public WeaponDataSO WeaponData => weaponData;

    #endregion

    #region Editor Validation

    /// <summary>
    /// Validates the item configuration in the Unity Editor.
    /// Warns about missing or incorrect data.
    /// </summary>
    private void OnValidate() {
        // Check for empty item ID
        if (string.IsNullOrEmpty(itemID)) {
            Debug.LogWarning($"[ShopItemData] {name} has no itemID assigned!", this);
        }

        // Weapons should have weapon data
        if (isWeapon && weaponData == null) {
            Debug.LogWarning($"[ShopItemData] {name} is marked as weapon but has no WeaponDataSO assigned!", this);
        }

        // Non-weapons shouldn't have weapon data
        if (!isWeapon && weaponData != null) {
            Debug.LogWarning($"[ShopItemData] {name} is not a weapon but has WeaponDataSO assigned!", this);
        }

        // Check for negative costs
        if (unlockCost < 0 || baseUpgradeCost < 0 || ammoCost < 0 || unitCost < 0) {
            Debug.LogWarning($"[ShopItemData] {name} has negative costs!", this);
        }
    }

    #endregion
}

