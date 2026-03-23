using UnityEngine;

/// <summary>
/// Defines the display data used by a shop item card.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemData", menuName = "Shop/Item Data")]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemName;
    [SerializeField][TextArea(2, 4)] private string description;

    [Header("Visual")]
    [SerializeField] private Sprite icon;

    [Header("Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float fireRate;
    [SerializeField] private int ammoCapacity;

    [Header("Economy")]
    [SerializeField] private int price;

    /// <summary>
    /// Returns the item display name.
    /// </summary>
    public string ItemName => itemName;

    /// <summary>
    /// Returns the item short description.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// Returns the item icon sprite.
    /// </summary>
    public Sprite Icon => icon;

    /// <summary>
    /// Returns the item damage stat.
    /// </summary>
    public float Damage => damage;

    /// <summary>
    /// Returns the item fire-rate stat.
    /// </summary>
    public float FireRate => fireRate;

    /// <summary>
    /// Returns the item ammo capacity stat.
    /// </summary>
    public int AmmoCapacity => ammoCapacity;

    /// <summary>
    /// Returns the item shop price.
    /// </summary>
    public int Price => price;
}
