using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

// REFATORAÇÃO: as cores na verdade vão ser texturas, ou pinturas diferentes, precisamos de rachaduras progressivas.
// REFATORAÇÃO: esse script deveria fazer parte do BuildableDataSO ou nao? Analise necessaria

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Represents a barricade that blocks enemy path to the player.
    /// Inherits from ItemBehaviour to unify item selection system.
    /// </summary>
    public class Barricade : ItemBehaviour, IDamageable {

        #region SERIALIZED FIELDS

        [Header("Barricade Data")]
        [SerializeField] private BuildableDataSO barricadeData;
        [SerializeField] private Sprite hudIcon;

        [Header("Barricade Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer barricadeRenderer;
        [SerializeField] private Color greenColor = Color.green;
        [SerializeField] private Color yellowColor = Color.yellow;
        [SerializeField] private Color redColor = Color.red;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip equipClip;
        [SerializeField] private float equipVolume = 1f;
        [SerializeField] private AudioClip placementClip;
        [SerializeField] private float placementVolume = 1f;
        [SerializeField] private AudioClip destroyClip;
        [SerializeField] private float destroyVolume = 1f;

        #endregion

        #region FIELDS

        private IAudioManagerService audioService;

        #endregion

        #region ITEM BEHAVIOUR IMPLEMENTATION

        public override string GetItemID() {
            if (barricadeData == null) {
                Debug.LogWarning("[Barricade] barricadeData is null!", gameObject);
                return "barricade_null";
            }
            return barricadeData.ItemID;
        }

        public override string GetDisplayName() {
            if (barricadeData == null) return "Unknown";
            return barricadeData.ItemName;
        }

        public override Sprite GetIcon() {
            if (hudIcon == null) {
                Debug.LogWarning("[Barricade] hudIcon is null!", gameObject);
                return null;
            }
            return hudIcon;
        }

        /// <summary>
        /// Called when player selects this item (key 6).
        /// Start placement mode (ghost preview appears).
        /// </summary>
        public override void OnSelected() {
            Debug.Log($"[Barricade] OnSelected called");
            PlayEquipSound();
            if (BuildingController.Instance != null && barricadeData != null) {
                Debug.Log($"[Barricade] Starting placement with BuildingController");
                BuildingController.Instance.StartPlacement(barricadeData);
            } else {
                Debug.LogWarning($"[Barricade] OnSelected: BuildingController.Instance={BuildingController.Instance}, barricadeData={barricadeData}");
            }
        }

        /// <summary>
        /// Called when player selects another item.
        /// Cancel placement mode.
        /// </summary>
        public override void OnDeselected() {
            Debug.Log($"[Barricade] OnDeselected called");
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                Debug.Log($"[Barricade] Canceling placement");
                BuildingController.Instance.CancelPlacement();
            }
        }

        /// <summary>
        /// NORMAL use: Place barricade with normal health.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed()) {
                return;
            }
        }

        /// <summary>
        /// Check if barricade can be placed (unlocked AND has quantity in inventory).
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                Debug.LogWarning($"[Barricade] CanBeUsed: PlayerProgress.Instance is NULL!");
                return false;
            }

            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            int quantity = PlayerProgress.Instance.GetBuildableQuantity(GetItemID());
            if (isUnlocked && quantity <= 0)
                FeedbackMessageUI.Instance?.Show();
            bool canUse = isUnlocked && quantity > 0;
            Debug.Log($"[Barricade] CanBeUsed check: ID={GetItemID()}, Unlocked={isUnlocked}, Quantity={quantity}, CanUse={canUse}");
            return canUse;
        }

        #endregion

        #region FIELDS

        private Color currentColor;

        #endregion

        #region PROPERTIES

        public float HealthFraction => maxHealth > 0f ? currentHealth / maxHealth : 0f;
        public bool IsDestroyed => currentHealth <= 0f;

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            
            currentHealth = maxHealth;

            if (barricadeRenderer == null)
                barricadeRenderer = GetComponent<Renderer>();
        }

        private void Start() {
            UpdateBarricadeColor();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Initializes the barricade's health and visual state. Should be called after instantiating the barricade.
        /// </summary>
        /// <param name="health"></param>
        public void Initialize(float health) {
            maxHealth = health;
            currentHealth = maxHealth;
            UpdateBarricadeColor();
        }

        /// <summary>
        /// Reduces the barricade's current health by the specified amount and triggers destruction if health reaches zero.
        /// </summary>
        /// <remarks>If the barricade's health is already zero or less, this method has no effect. When health
        /// drops to zero or below, the barricade is destroyed.</remarks>
        /// <param name="amount">The amount of damage to apply to the barricade. Must be a non-negative value.</param>
        public void TakeDamage(float amount) {
            if (currentHealth <= 0f) return;

            currentHealth -= amount;
            UpdateBarricadeColor();

            if (currentHealth <= 0f) {
                DestroyBarricade();
            }
        }

        /// <summary>
        /// Updates the barricade's color based on its current health percentage.
        /// </summary>
        private void UpdateBarricadeColor() {
            if (barricadeRenderer == null) return;

            float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;

            if (healthPercent > 0.66f) {
                currentColor = greenColor;
            }
            else if (healthPercent > 0.33f) {
                currentColor = yellowColor;
            }
            else {
                currentColor = redColor;
            }

            barricadeRenderer.material.color = currentColor;
        }

        /// <summary>
        /// Destroys the barricade object and removes it from the scene.
        /// </summary>
        private void DestroyBarricade() {
            PlayDestroySound();
            Destroy(gameObject);
        }

        #endregion

        #region AUDIO

        public void PlayEquipSound() {
            if (equipClip != null && audioService != null) {
                audioService.PlaySFX2D(equipClip, equipVolume);
            }
        }

        public void PlayPlacementSound() {
            if (placementClip != null && audioService != null) {
                audioService.PlaySFX2D(placementClip, placementVolume);
            }
        }

        private void PlayDestroySound() {
            if (destroyClip != null && audioService != null) {
                audioService.PlaySFX3D(destroyClip, transform.position, destroyVolume);
            }
        }

        #endregion
    }
}