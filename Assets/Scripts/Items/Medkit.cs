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

        #region PROPERTIES

        #endregion

        #region EVENTS

        #endregion

        #region CONSTANTS

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

        #endregion

        #region METHODS

        #region ITEM BEHAVIOUR IMPLEMENTATION

        /// <summary>
        /// Gets the item ID from ScriptableObject (e.g., "4" for Medkit).
        /// </summary>
        public override string GetItemID() {
            if (medkitData == null) {
                Debug.LogWarning("[Medkit] medkitData is null! GetItemID() returning fallback.", gameObject);
                return "medkit_null";
            }
            return medkitData.ItemID;
        }

        /// <summary>
        /// Gets display name from ScriptableObject (e.g., "First Aid Kit").
        /// </summary>
        public override string GetDisplayName() {
            if (medkitData == null) return "Unknown";
            return medkitData.ItemName;
        }

        /// <summary>
        /// Gets HUD icon. Null check prevents exceptions.
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
        /// Activates visual representation (medkit model in hand).
        /// </summary>
        public override void OnSelected() {
            string id = GetItemID();
            int total = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemTotal(id) : -1;

            PlayEquipSound();
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.SetItemCurrent(id, total > 0 ? 1 : 0);
            }
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when player selects another item. Deactivates visual.
        /// </summary>
        public override void OnDeselected() {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Uses the medkit: heals the player based on upgrade level.
        /// If health is at 100%, plays deny sound and does not consume the item.
        /// If quantity reaches 0, automatically equips weapon with holster animation.
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

                if (remaining > 0) {
                    PlayerProgress.Instance.SetItemCurrent(GetItemID(), 1);
                } else {
                    PlayerProgress.Instance.SetItemCurrent(GetItemID(), 0);
                    EquipWeaponAutomatically();
                }
            }
        }

        /// <summary>
        /// Checks if medkit is unlocked (for selection). Quantity check happens in OnUse().
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

        #region ANIMATION

        /// <summary>
        /// Medkit does not need a weapon pose. Keeps hands lowered when equipped.
        /// </summary>
        public override bool KeepHolsteredOnEquip() => true;

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

        #region HELPER METHODS

        /// <summary>
        /// Automatically equips the default weapon (pistol) when medkit quantity reaches zero.
        /// Uses smooth animation transition via Character.TryRestoreWeaponSmoothly().
        /// </summary>
        private void EquipWeaponAutomatically() {
            Character character = GetComponentInParent<Character>();
            if (character == null) return;

            character.TryRestoreWeaponSmoothly();
        }

        #endregion

        #region DEBUG

        #endregion

        #endregion
    }
}
