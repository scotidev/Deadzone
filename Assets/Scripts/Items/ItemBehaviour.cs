// Copyright 2021, Infima Games. All Rights Reserved.

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
        /// Se true, o personagem mantém as mãos abaixadas (holstered) ao equipar este item.
        /// Itens que não precisam de pose de tiro (medkit, grenade, buildables) devem retornar true.
        /// </summary>
        public virtual bool KeepHolsteredOnEquip() => false;
        
        #endregion
        
        #region VALIDATION
        
        /// <summary>
        /// Check if item can be selected/used (desbloqueado? tem munição/quantidade?).
        /// Used to prevent selecting locked items or items without ammo.
        /// </summary>
        public abstract bool CanBeUsed();
        
        #endregion
    }
}
