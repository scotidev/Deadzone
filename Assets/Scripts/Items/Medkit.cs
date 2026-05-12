using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Medkit consumable item. Heals the player when used.
    /// </summary>
    public class Medkit : ItemBehaviour {
        
        #region SERIALIZED FIELDS
        
        [SerializeField] private MedkitDataSO medkitData;
        [SerializeField] private Sprite hudIcon;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip equipClip;
        [SerializeField] private float equipVolume = 1f;
        [SerializeField] private AudioClip useClip;
        [SerializeField] private float useVolume = 1f;
        
        #endregion

        #region FIELDS

        private IAudioManagerService audioService;

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

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
            PlayEquipSound();
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
                PlayUseSound();
                
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

        #region AUDIO

        private void PlayEquipSound() {
            if (equipClip != null && audioService != null) {
                audioService.PlaySFX2D(equipClip, equipVolume);
            }
        }

        private void PlayUseSound() {
            if (useClip != null && audioService != null) {
                audioService.PlaySFX2D(useClip, useVolume);
            }
        }

        #endregion
    }
}
