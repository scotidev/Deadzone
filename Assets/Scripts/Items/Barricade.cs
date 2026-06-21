using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

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

        [Header("Visual Feedback")]
        [Tooltip("Renderer whose material will be swapped as damage progresses.")]
        [SerializeField] private Renderer barricadeRenderer;
        [Tooltip("Materials for each damage state: 0=Intact (>66%), 1=Damaged (>33%), 2=Heavy (>0%), 3=Critical (<=0%).")]
        [SerializeField] private Material[] damageStateMaterials;

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
        private float maxHealth;
        private float currentHealth;

        #endregion

        #region PROPERTIES

        public float HealthFraction => maxHealth > 0f ? currentHealth / maxHealth : 0f;
        public bool IsDestroyed => currentHealth <= 0f;

        #endregion

        #region EVENTS

        #endregion

        #region CONSTANTS

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            InitializeHealth();
            if (barricadeRenderer == null)
                barricadeRenderer = GetComponent<Renderer>();
        }

        private void Start() {
            UpdateBarricadeVisual();
        }

        #endregion

        #region METHODS

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
        /// Starts placement mode (ghost preview appears).
        /// </summary>
        public override void OnSelected() {
            PlayEquipSound();
            if (PlayerProgress.Instance != null) {
                string id = GetItemID();
                int total = PlayerProgress.Instance.GetItemTotal(id);
                PlayerProgress.Instance.SetItemCurrent(id, total > 0 ? 1 : 0);
            }
            if (BuildingController.Instance != null && barricadeData != null) {
                BuildingController.Instance.StartPlacement(barricadeData);
            } else {
                Debug.LogWarning($"[Barricade] OnSelected: BuildingController.Instance={BuildingController.Instance}, barricadeData={barricadeData}");
            }
        }

        /// <summary>
        /// Called when player selects another item. Cancels placement mode.
        /// </summary>
        public override void OnDeselected() {
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                BuildingController.Instance.CancelPlacement();
            }
        }

        /// <summary>
        /// Normal use: Place barricade with normal health.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed()) {
                return;
            }
        }

        /// <summary>
        /// Checks if barricade can be placed (unlocked and has quantity in inventory).
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
            return isUnlocked && quantity > 0;
        }

        #endregion

        #region ANIMATION

        /// <summary>
        /// Barricade does not need a weapon pose. Keeps hands lowered when equipped.
        /// </summary>
        public override bool KeepHolsteredOnEquip() => true;

        #endregion

        #region HEALTH

        /// <summary>
        /// Reads the barricade's max health from BuildableDataSO scaled by the player's current upgrade level.
        /// </summary>
        private void InitializeHealth() {
            if (barricadeData == null) {
                Debug.LogWarning("[Barricade] barricadeData is null, cannot initialize health!", gameObject);
                return;
            }

            int level = PlayerProgress.Instance?.GetItemLevel(GetItemID()) ?? 1;
            maxHealth = barricadeData.GetResistanceAtLevel(level);
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Reduces the barricade's current health by the specified amount and triggers destruction if health reaches zero.
        /// </summary>
        public void TakeDamage(float amount) {
            if (currentHealth <= 0f) return;

            currentHealth -= amount;
            UpdateBarricadeVisual();

            if (currentHealth <= 0f) {
                DestroyBarricade();
            }
        }

        /// <summary>
        /// Swaps the barricade's material based on current health percentage.
        /// Indexes: 0=Intact (>66%), 1=Damaged (>33%), 2=Heavy (>0%), 3=Critical (<=0%).
        /// </summary>
        private void UpdateBarricadeVisual() {
            if (barricadeRenderer == null) return;
            if (damageStateMaterials == null || damageStateMaterials.Length == 0) return;

            float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;

            int index;
            if (healthPercent > 0.66f)
                index = 0;
            else if (healthPercent > 0.33f)
                index = 1;
            else if (healthPercent > 0f)
                index = 2;
            else
                index = 3;

            index = Mathf.Clamp(index, 0, damageStateMaterials.Length - 1);
            barricadeRenderer.material = damageStateMaterials[index];
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

        #region DEBUG

        #endregion

        #endregion
    }
}
