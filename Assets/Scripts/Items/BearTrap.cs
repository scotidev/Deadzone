using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// BearTrap buildable item. Places a bear trap in the world.
    /// </summary>
    public class BearTrap : ItemBehaviour {
        
        #region SERIALIZED FIELDS
        
        [SerializeField] private BuildableDataSO bearTrapData;
        [SerializeField] private Sprite hudIcon;
        
        #endregion
        
        #region ITEM BEHAVIOUR IMPLEMENTATION
        
        public override string GetItemID() {
            if (bearTrapData == null) {
                Debug.LogWarning("[BearTrap] bearTrapData is null!", gameObject);
                return "beartrap_null";
            }
            return bearTrapData.ItemID;
        }
        
        public override string GetDisplayName() {
            if (bearTrapData == null) return "Unknown";
            return bearTrapData.ItemName;
        }
        
        public override Sprite GetIcon() {
            if (hudIcon == null) {
                Debug.LogWarning("[BearTrap] hudIcon is null!", gameObject);
                return null;
            }
            return hudIcon;
        }
        
        /// <summary>
        /// Called when player selects this item (key 8).
        /// Start placement mode (ghost preview appears).
        /// </summary>
        public override void OnSelected() {
            if (BuildingController.Instance != null && bearTrapData != null) {
                BuildingController.Instance.StartPlacement(bearTrapData);
            }
        }
        
        /// <summary>
        /// Called when player selects another item.
        /// Cancel placement mode.
        /// </summary>
        public override void OnDeselected() {
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                BuildingController.Instance.CancelPlacement();
            }
        }
        
        /// <summary>
        /// NORMAL use: Place bear trap with normal damage.
        /// Placement logic is handled by BuildingController.
        /// This method is here for interface compliance.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed()) {
                return;
            }
        }
        
        /// <summary>
        /// Check if bear trap is unlocked (for selection). Quantity check happens in OnUse().
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                return false;
            }
            
            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            return isUnlocked;
        }
        
        #endregion
    }
}
