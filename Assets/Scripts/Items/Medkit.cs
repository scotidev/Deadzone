using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Medkit consumable item. Heals the player when used.
    /// </summary>
    public class Medkit : ItemBehaviour {
        
        #region SERIALIZED FIELDS
        
        [SerializeField] private MedkitDataSO medkitData;
        [SerializeField] private Sprite hudIcon;
        
        #endregion
        
        #region ITEM BEHAVIOUR IMPLEMENTATION
        
        /// <summary>
        /// Get the item ID from ScriptableObject (e.g., "4" for Medkit).
        /// CONCEITO: Não duplicamos dados. Tudo vem do SO (source of truth).
        /// </summary>
        public override string GetItemID() {
            if (medkitData == null) {
                Debug.LogWarning("[Medkit] medkitData is null! GetItemID() returning fallback.", gameObject);
                return "medkit_null";
            }
            return medkitData.ItemID;
        }
        
        /// <summary>
        /// Get display name from ScriptableObject (e.g., "First Aid Kit").
        /// </summary>
        public override string GetDisplayName() {
            if (medkitData == null) return "Unknown";
            return medkitData.ItemName;
        }
        
        /// <summary>
        /// Get HUD icon. Null check prevents exceptions.
        /// </summary>
        public override Sprite GetIcon() {
            if (hudIcon == null) {
                Debug.LogWarning("[Medkit] hudIcon is null! Show warning in HUD.", gameObject);
                return null;
            }
            return hudIcon;
        }
        
        /// <summary>
        /// Called when player selects this item (key 4).
        /// Activate visual representation (medkit model in hand).
        /// </summary>
        public override void OnSelected() {
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Called when player selects another item.
        /// Deactivate visual.
        /// </summary>
        public override void OnDeselected() {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// NORMAL use: player presses fire button.
        /// Heals the player by medkitData.healAmount.
        /// CONCEITO: A cura é baseada no SO. Se mudar o SO, o comportamento muda automaticamente.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed() || medkitData == null) {
                Debug.LogWarning("[Medkit] Cannot use medkit (not unlocked or no quantity).", gameObject);
                return;
            }
            
            PlayerHealth playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth != null) {
                playerHealth.Heal(medkitData.healAmount);
                
                // Consume 1 medkit from inventory
                if (PlayerProgress.Instance != null) {
                    PlayerProgress.Instance.ConsumeItem(GetItemID(), 1);
                }
            }
        }
        
        /// <summary>
        /// Check if medkit is unlocked (for selection). Quantity check happens in OnUse().
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                Debug.LogWarning($"[Medkit] CanBeUsed: PlayerProgress.Instance is NULL!");
                return false;
            }
            
            // CONCEITO: CanBeUsed() é para seleção, apenas verifica se desbloqueado.
            // A checagem de quantidade/ammo é feita em OnUse(), não em CanBeUsed().
            // Isso permite selecionar qualquer item desbloqueado, mesmo sem quantidade.
            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            Debug.Log($"[Medkit] CanBeUsed check: ID={GetItemID()}, Unlocked={isUnlocked}");
            return isUnlocked;
        }
        
        #endregion
    }
}
