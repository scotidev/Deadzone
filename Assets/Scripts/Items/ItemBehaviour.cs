using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Base class for all selectable items in the player's inventory.
    /// Unifies the interface for weapons, consumables, and buildables.
    /// Each item is a GameObject child of Inventory, and gets activated/deactivated
    /// when selected by pressing keys 1-8.
    /// </summary>
    public abstract class ItemBehaviour : MonoBehaviour {
        
        #region PROPERTIES
        
        /// <summary>
        /// Item's unique identifier (e.g., "Pistol", "MedKit", "Barricade")
        /// </summary>
        public abstract string GetItemID();
        
        /// <summary>
        /// Display name for UI
        /// </summary>
        public abstract string GetDisplayName();
        
        /// <summary>
        /// Icon to show in HUD/inventory
        /// </summary>
        public abstract Sprite GetIcon();
        
        #endregion
        
        #region METHODS

        #region LIFECYCLE
        
        /// <summary>
        /// Called when player selects this item (key 1-8 pressed).
        /// Activate visual representation, play sounds, etc.
        /// </summary>
        public abstract void OnSelected();
        
        /// <summary>
        /// Called when player selects another item.
        /// Deactivate visual, stop sounds, etc.
        /// </summary>
        public abstract void OnDeselected();
        
        /// <summary>
        /// Called when player uses the item (fire button clicked).
        /// Weapon: fires; Medkit: heals; Grenade: throws; Buildable: places.
        /// </summary>
        public abstract void OnUse();
        
        #endregion

        #region ANIMATION
        
        /// <summary>
        /// If true, the character keeps hands holstered when equipping this item.
        /// Items that don't need a shooting pose (medkit, grenade, buildables) should return true.
        /// </summary>
        public virtual bool KeepHolsteredOnEquip() => false;
        
        #endregion
        
        #region VALIDATION
        
        /// <summary>
        /// Checks if the item can be selected or used (unlocked, has ammo/quantity).
        /// Prevents selecting locked items or items without ammo.
        /// </summary>
        public abstract bool CanBeUsed();
        
        #endregion

        #endregion
    }
}
