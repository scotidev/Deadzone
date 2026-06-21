using System;
using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Vest armor item. Auto-equipped when unlocked/upgraded.
    /// NOT selectable via keys 1-8 (not added to Inventory.selectableItems).
    /// Provides armor damage reduction.
    /// </summary>
    public class Vest : ItemBehaviour, IShopItemCallback {

        #region SERIALIZED FIELDS

        [SerializeField] private VestDataSO vestData;
        [SerializeField] private float damageReductionPercentage = 0.1f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip vestEquippedClip;
        [SerializeField] private AudioClip vestDestroyedClip;

        #endregion

        #region FIELDS

        private IAudioManagerService audioService;
        private PlayerHealth playerHealth;
        private float currentArmor;
        private float maxArmor;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Public accessor to vest data for other scripts.
        /// </summary>
        public VestDataSO VestData => vestData;

        #endregion

        #region EVENTS

        public event Action<float> OnArmorChanged;
        public event Action OnArmorDepleted;
        public static event System.Action OnVestDestroyed;

        #endregion

        #region CONSTANTS

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            currentArmor = 0f;
            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth == null) {
                playerHealth = GetComponentInParent<Character>()?.GetComponentInChildren<PlayerHealth>();
            }
        }

        private void Start() {
            InitializeArmorFromVestLevel();
        }

        private void OnDestroy() {
        }

        /// <summary>
        /// Initializes armor based on current vest level. Called at Start for games where vest is already unlocked.
        /// </summary>
        private void InitializeArmorFromVestLevel() {
            if (PlayerProgress.Instance != null && PlayerProgress.Instance.IsItemUnlocked(GetItemID())) {
                float maxArmorFromLevel = GetMaxArmorFromCurrentLevel();
                maxArmor = maxArmorFromLevel;
                currentArmor = maxArmorFromLevel;
                OnArmorChanged?.Invoke(1f);
            }
        }

        #endregion

        #region METHODS

        #region ITEM BEHAVIOUR IMPLEMENTATION

        public override string GetItemID() {
            if (vestData == null) {
                Debug.LogWarning("[Vest] vestData é null! Configure no Inspector.", gameObject);
                return "vest_null";
            }
            return vestData.ItemID;
        }

        public override string GetDisplayName() {
            if (vestData == null) return "Unknown";
            return vestData.ItemName;
        }

        /// <summary>
        /// Vest does not have a HUD icon (not selectable).
        /// </summary>
        public override Sprite GetIcon() {
            return null;
        }

        /// <summary>
        /// Vest does not respond to selection.
        /// </summary>
        public override void OnSelected() {
        }

        /// <summary>
        /// Vest does not respond to deselection.
        /// </summary>
        public override void OnDeselected() {
        }

        /// <summary>
        /// Vest has no use action. It provides passive damage reduction.
        /// </summary>
        public override void OnUse() {
        }

        /// <summary>
        /// Vest can always be used (always equipped).
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                return false;
            }
            return PlayerProgress.Instance.IsItemUnlocked(GetItemID());
        }

        /// <summary>
        /// Gets the damage reduction percentage for this vest.
        /// Used by PlayerHealth to reduce incoming damage.
        /// </summary>
        public float GetDamageReductionPercentage() {
            return damageReductionPercentage;
        }

        #endregion

        #region ARMOR MANAGEMENT

        /// <summary>
        /// Gets the maximum armor value based on Vest's current level.
        /// </summary>
        public float GetMaxArmorFromCurrentLevel() {
            if (PlayerProgress.Instance == null || vestData == null) {
                Debug.LogWarning("[Vest] GetMaxArmorFromCurrentLevel: PlayerProgress or vestData is null");
                return 100f;
            }

            string vestID = GetItemID();
            int level = PlayerProgress.Instance.GetItemLevel(vestID);
            return vestData.GetResistanceAtLevel(level);
        }

        /// <summary>
        /// Equips the vest when the player unlocks it in the shop.
        /// Sets armor to maximum based on vest level and notifies UI.
        /// </summary>
        public void Equip() {
            float maxArmorValue = GetMaxArmorFromCurrentLevel();
            currentArmor = maxArmorValue;
            maxArmor = maxArmorValue;
            OnArmorChanged?.Invoke(currentArmor / maxArmor);
            PlayEquippedSound();
        }

        /// <summary>
        /// Called when vest is upgraded. Updates maxArmor to new level and fills armor to 100%.
        /// </summary>
        public void OnUpgraded() {
            float newMaxArmor = GetMaxArmorFromCurrentLevel();
            maxArmor = newMaxArmor;
            currentArmor = maxArmor;
            OnArmorChanged?.Invoke(currentArmor / maxArmor);
            PlayEquippedSound();
        }

        /// <summary>
        /// Absorbs damage from the armor. Returns remaining damage that was not absorbed.
        /// </summary>
        public float AbsorbDamage(float incomingDamage) {
            if (currentArmor <= 0f) {
                return incomingDamage;
            }

            float absorbedDamage = Mathf.Min(currentArmor, incomingDamage);
            currentArmor -= absorbedDamage;
            float remainingDamage = incomingDamage - absorbedDamage;

            OnArmorChanged?.Invoke(currentArmor / maxArmor);

            if (currentArmor <= 0f) {
                currentArmor = 0f;
                OnArmorDepleted?.Invoke();

                if (PlayerProgress.Instance != null && PlayerProgress.Instance.IsItemUnlocked(GetItemID())) {
                    PlayDestroyedSound();
                }
            }

            return remainingDamage;
        }

        /// <summary>
        /// Adds armor points without exceeding maxArmor.
        /// </summary>
        public void AddArmor(float amount) {
            currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
            OnArmorChanged?.Invoke(currentArmor / maxArmor);
        }

        /// <summary>
        /// Returns the current armor as a fraction between 0 and 1.
        /// </summary>
        public float GetArmorFraction() => maxArmor > 0f ? currentArmor / maxArmor : 0f;

        /// <summary>
        /// Returns the current armor value.
        /// </summary>
        public float GetCurrentArmor() => currentArmor;

        /// <summary>
        /// Returns the maximum armor value.
        /// </summary>
        public float GetMaxArmor() => maxArmor;

        /// <summary>
        /// Returns whether the player currently has any armor.
        /// </summary>
        public bool HasArmor() => currentArmor > 0f;

        #endregion

        #region AUDIO

        /// <summary>
        /// Plays the vest equipped sound effect.
        /// Called when player unlocks or buys the vest from the shop.
        /// </summary>
        public void PlayEquippedSound() {
            if (vestEquippedClip != null && audioService != null) {
                audioService.PlaySFX2D(vestEquippedClip);
            }
        }

        /// <summary>
        /// Plays the vest destroyed sound effect.
        /// Called when the vest is destroyed (armor reaches 0).
        /// </summary>
        public void PlayDestroyedSound() {
            if (vestDestroyedClip != null && audioService != null) {
                audioService.PlaySFX2D(vestDestroyedClip);
            }
            OnVestDestroyed?.Invoke();
        }

        #endregion

        #region SHOP

        /// <summary>
        /// Gets the Vest component from the player character.
        /// Used by ShopUI to get Vest reference.
        /// </summary>
        public static Vest GetFromPlayer(Character player) {
            if (player == null) return null;

            Vest vest = player.GetComponent<Vest>();
            if (vest == null) {
                vest = player.GetComponentInChildren<Vest>();
            }
            return vest;
        }

        /// <summary>
        /// Called from ShopUI when the vest is selected.
        /// Updates the ammo/repair button display in the shop.
        /// </summary>
        public void UpdateShopAmmoDisplay(UnityEngine.UI.Button ammoButton, TMPro.TextMeshProUGUI priceText, int costPerPurchase) {
            if (ammoButton == null) return;

            float armorFraction = GetArmorFraction();
            bool isFull = armorFraction >= 1f;
            bool isUnlocked = PlayerProgress.Instance != null && PlayerProgress.Instance.IsItemUnlocked(GetItemID());

            if (!isUnlocked) {
                if (priceText != null) priceText.text = "LOCKED";
                ammoButton.interactable = false;
            } else if (isFull) {
                if (priceText != null) priceText.text = "FULL";
                ammoButton.interactable = false;
            } else {
                if (priceText != null) priceText.text = $"${costPerPurchase:N0}";
                ammoButton.interactable = EconomyManager.Instance != null &&
                                         EconomyManager.Instance.CanAfford(costPerPurchase);
            }
        }

        /// <summary>
        /// Called from ShopUI when the vest is unlocked.
        /// Equips the vest and shows the armor UI.
        /// </summary>
        public void OnShopUnlock() {
            Equip();
            ShowArmorUI();
        }

        /// <summary>
        /// Called from ShopUI when the vest is upgraded.
        /// </summary>
        public void OnShopUpgrade() {
            OnUpgraded();
        }

        /// <summary>
        /// Shows the VestUI after unlock/upgrade.
        /// </summary>
        private void ShowArmorUI() {
            var vestUI = UnityEngine.Object.FindFirstObjectByType<VestUI>();
            if (vestUI != null) {
                vestUI.ShowArmorUI();
            }
        }

        #endregion

        #region DEBUG

        #endregion

        #endregion
    }
}
