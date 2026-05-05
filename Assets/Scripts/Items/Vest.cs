using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Vest armor item. Auto-equipped when unlocked/upgraded.
    /// NOT selectable via keys 1-8 (not added to Inventory.selectableItems).
    /// Provides armor damage reduction.
    /// </summary>
    public class Vest : ItemBehaviour {
        
        #region SERIALIZED FIELDS
        
        [SerializeField] private VestDataSO vestData;
        [SerializeField] private float damageReductionPercentage = 0.1f;  // 10% reduction base
        [SerializeField] private float exclusiveDamageReductionPercentage = 0.2f;  // 20% reduction exclusive
        
        #endregion
        
        #region ITEM BEHAVIOUR IMPLEMENTATION
        
        public override string GetItemID() {
            if (vestData == null) {
                Debug.LogWarning("[Vest] vestData is null!", gameObject);
                return "vest_null";
            }
            return vestData.itemID;
        }
        
        public override string GetDisplayName() {
            if (vestData == null) return "Unknown";
            return vestData.itemName;
        }
        
        /// <summary>
        /// Vest doesn't have a HUD icon (not selectable).
        /// </summary>
        public override Sprite GetIcon() {
            return null;
        }
        
        /// <summary>
        /// Vest doesn't respond to selection (never called).
        /// It's auto-equipped, not manually selectable.
        /// </summary>
        public override void OnSelected() {
            // Vest is auto-equipped, not selectable via keys
        }
        
        /// <summary>
        /// Vest doesn't respond to deselection.
        /// </summary>
        public override void OnDeselected() {
            // Vest is always equipped
        }
        
        /// <summary>
        /// Vest doesn't have a "use" action.
        /// It provides passive armor reduction.
        /// </summary>
        public override void OnUse() {
            // Vest is passive, no use action
        }
        
        /// <summary>
        /// Vest doesn't have an exclusive "use" action.
        /// Exclusive just increases passive reduction to 20%.
        /// </summary>
        public override void OnUseExclusive() {
            // Vest is passive, exclusive just means better reduction
        }
        
        /// <summary>
        /// Vest can always be "used" (it's always equipped).
        /// But this is never called in practice since Vest is not selectable.
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                return false;
            }
            
            // Check if Vest is unlocked
            return PlayerProgress.Instance.IsItemUnlocked(GetItemID());
        }
        
        /// <summary>
        /// Check if Vest has exclusive upgrade (level 9+).
        /// </summary>
        public override bool HasExclusiveUnlocked() {
            if (PlayerProgress.Instance == null) {
                return false;
            }
            
            int level = PlayerProgress.Instance.GetItemLevel(GetItemID());
            return level >= 9;
        }
        
        /// <summary>
        /// Get damage reduction percentage for this vest.
        /// Used by PlayerHealth or PlayerArmor to reduce incoming damage.
        /// </summary>
        public float GetDamageReductionPercentage() {
            return HasExclusiveUnlocked() ? exclusiveDamageReductionPercentage : damageReductionPercentage;
        }
        
        #endregion
    }
}
