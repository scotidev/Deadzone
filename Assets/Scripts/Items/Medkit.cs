using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

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
        [SerializeField] private AudioClip reliefClip;
        [SerializeField] private float reliefVolume = 1f;
        [SerializeField] private AudioClip denyClip;
        [SerializeField] private float denyVolume = 1f;

        [Header("Visual Feedback")]
        [SerializeField] private float feedbackDuration = 1f;
        
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
            string id = GetItemID();
            int total = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemTotal(id) : -1;
            Debug.Log($"[Medkit] OnSelected: itemID={id}, total={total}");

            PlayEquipSound();
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.SetItemCurrent(id, total > 0 ? 1 : 0);
            }
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
        /// Use the medkit: heal the player based on upgrade level.
        /// If health is at 100%, play deny sound and do not consume the item.
        /// If no medkit in hand, play deny sound.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed() || medkitData == null) {
                Debug.LogWarning("[Medkit] Cannot use medkit (not unlocked or no quantity).", gameObject);
                return;
            }

            if (PlayerProgress.Instance != null) {
                int currentInHand = PlayerProgress.Instance.GetItemCurrent(GetItemID());
                if (currentInHand <= 0) {
                    PlayDenySound();
                    Debug.Log("[Medkit] No medkit in hand, cannot use.");
                    return;
                }
            }
            
            PlayerHealth playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) {
                Debug.LogWarning("[Medkit] PlayerHealth not found!", gameObject);
                return;
            }

            if (playerHealth.GetHealthFraction() >= 1f) {
                PlayDenySound();
                Debug.Log("[Medkit] Health already at 100%, cannot use medkit.");
                return;
            }

            int currentLevel = 1;
            if (PlayerProgress.Instance != null) {
                currentLevel = PlayerProgress.Instance.GetItemLevel(GetItemID());
            }

            float[] statValues = medkitData.GetStatValues(currentLevel);
            float healAmount = statValues[0];

            playerHealth.Heal(healAmount);
            PlayUseSound();
            PlayReliefSound();
            ShowHealFeedback();

            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.UseItem(GetItemID(), 1);
                int remaining = PlayerProgress.Instance.GetItemTotal(GetItemID());
                PlayerProgress.Instance.SetItemCurrent(GetItemID(), remaining > 0 ? 1 : 0);
                Debug.Log($"[Medkit] OnUse: itemID={GetItemID()}, remainingAfterUse={remaining}");
            }

            Debug.Log($"[Medkit] Healed for {healAmount} HP. Level: {currentLevel}");
        }
        
        /// <summary>
        /// Check if medkit is unlocked (for selection). Quantity check happens in OnUse().
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                return false;
            }
            
            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            int total = PlayerProgress.Instance.GetItemTotal(GetItemID());
            if (isUnlocked && total <= 0)
                FeedbackMessageUI.Instance?.Show();
            return isUnlocked && total > 0;
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

        private void PlayDenySound() {
            if (denyClip != null && audioService != null) {
                audioService.PlaySFX2D(denyClip, denyVolume);
            }
        }

        private void PlayReliefSound() {
            if (reliefClip != null && audioService != null) {
                audioService.PlaySFX2D(reliefClip, reliefVolume);
            }
        }

        #endregion

        #region VISUAL FEEDBACK

        private void ShowHealFeedback() {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowHealFeedback(feedbackDuration);
            }
        }

        #endregion
    }
}