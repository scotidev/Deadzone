using UnityEngine;

namespace Deadzone.Interfaces {

/// <summary>
/// Interface for items that need to respond to shop events (unlock/upgrade).
/// Implement this on any MonoBehaviour that needs callback when purchased/upgraded in the shop.
/// </summary>
public interface IShopItemCallback {

    /// <summary>
    /// Called when the item is unlocked in the shop.
    /// </summary>
    void OnShopUnlock();

    /// <summary>
    /// Called when the item is upgraded in the shop.
    /// </summary>
    void OnShopUpgrade();
}

}