using UnityEngine;
using InfimaGames.LowPolyShooterPack;

// REFATORAÇÃO: as cores na verdade vão ser texturas, ou pinturas diferentes, precisamos de rachaduras progressivas.
// REFATORAÇÃO: esse script deveria fazer parte do BuildableDataSO ou nao? Analise necessaria

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Represents a barricade that blocks enemy path to the player.
    /// Inherits from ItemBehaviour to unify item selection system.
    /// Supports exclusive upgrade: 1.5x health at level 9+
    /// </summary>
    public class Barricade : ItemBehaviour, IDamageable {

        #region SERIALIZED FIELDS

        [Header("Barricade Data")]
        [SerializeField] private BuildableDataSO barricadeData;
        [SerializeField] private Sprite hudIcon;
        [SerializeField] private float exclusiveHealthMultiplier = 1.5f;

        [Header("Barricade Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer barricadeRenderer;
        [SerializeField] private Color greenColor = Color.green;
        [SerializeField] private Color yellowColor = Color.yellow;
        [SerializeField] private Color redColor = Color.red;

        #endregion

        #region ITEM BEHAVIOUR IMPLEMENTATION

        public override string GetItemID() {
            if (barricadeData == null) {
                Debug.LogWarning("[Barricade] barricadeData is null!", gameObject);
                return "barricade_null";
            }
            return barricadeData.itemID;
        }

        public override string GetDisplayName() {
            if (barricadeData == null) return "Unknown";
            return barricadeData.itemName;
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
            // BuildingController handles the actual placement
        }

        /// <summary>
        /// EXCLUSIVE use: Place barricade with 1.5x health.
        /// </summary>
        public override void OnUseExclusive() {
            if (!CanBeUsed() || !HasExclusiveUnlocked()) {
                return;
            }
            // TODO: Implement exclusive placement (e.g., higher health barricade variant)
        }

        /// <summary>
        /// Check if barricade can be placed (quantity > 0).
        /// </summary>
        /// <summary>
        /// Check if bar is unlocked (for selection). Quantity check happens in OnUse().
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                Debug.LogWarning($"[Barricade] CanBeUsed: PlayerProgress.Instance is NULL!");
                return false;
            }

            // CONCEITO: CanBeUsed() é para seleção, apenas verifica se desbloqueado.
            // A checagem de quantidade é feita em OnUse(), não em CanBeUsed().
            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            Debug.Log($"[Barricade] CanBeUsed check: ID={GetItemID()}, Unlocked={isUnlocked}");
            return isUnlocked;
        }

        /// <summary>
        /// Check if exclusive upgrade is unlocked (level 9+).
        /// </summary>
        public override bool HasExclusiveUnlocked() {
            if (PlayerProgress.Instance == null) {
                return false;
            }

            int level = PlayerProgress.Instance.GetItemLevel(GetItemID());
            return level >= 9;
        }

        #endregion

    #region FIELDS

    private Color currentColor;

    #endregion

    #region PROPERTIES

    public float HealthFraction => currentHealth / maxHealth;
    public bool IsDestroyed => currentHealth <= 0f;

    #endregion

    #region UNITY

    private void Awake() {
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

        float healthPercent = currentHealth / maxHealth;

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
        Destroy(gameObject);
    }

    #endregion
}
}