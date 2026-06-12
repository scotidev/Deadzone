using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

// CONCEITO: Este script agora segue o princípio da responsabilidade única.
// maxHealth NÃO é mais um campo serializado — cada instância lê seu valor
// diretamente do BuildableDataSO.GetResistanceAtLevel() via PlayerProgress.
// O SO é a única fonte da verdade para os dados de design.
//
// CONCEITO: A cor foi substituída por um array de materiais (damageStateMaterials),
// permitindo texturas de rachadura progressiva em vez de simples tintas.

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
            // FIXED: Set current ammo to 1 when buildable is selected (1 in hand ready to place).
            if (PlayerProgress.Instance != null) {
                string id = GetItemID();
                int total = PlayerProgress.Instance.GetItemTotal(id);
                PlayerProgress.Instance.SetItemCurrent(id, total > 0 ? 1 : 0);
            }
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

        #region ANIMATION

        /// <summary>
        /// Barricade nao precisa de pose de arma. Mantem maos abaixadas ao equipar.
        /// </summary>
        public override bool KeepHolsteredOnEquip() => true;

        #endregion

        #region PROPERTIES

        public float HealthFraction => maxHealth > 0f ? currentHealth / maxHealth : 0f;
        public bool IsDestroyed => currentHealth <= 0f;

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();

            // CONCEITO: Inicializa a saúde lendo do BuildableDataSO + nível do jogador.
            // O valor serializado antigo (maxHealth = 100f) foi removido — o SO é a fonte da verdade.
            InitializeHealth();

            if (barricadeRenderer == null)
                barricadeRenderer = GetComponent<Renderer>();
        }

        private void Start() {
            UpdateBarricadeVisual();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Reads the barricade's max health from BuildableDataSO scaled by the player's current upgrade level.
        /// CONCEITO: O SO define o valor base + scaling por nível. PlayerProgress fornece o nível atual.
        /// Isso garante que barricadas recém-colocadas sempre usem o nível de upgrade mais recente.
        /// </summary>
        private void InitializeHealth() {
            if (barricadeData == null) {
                Debug.LogWarning("[Barricade] barricadeData is null, cannot initialize health!", gameObject);
                return;
            }

            // CONCEITO: Lê o nível de upgrade atual do jogador para este item.
            // Se PlayerProgress ainda não estiver disponível, usa level 1 como fallback.
            int level = PlayerProgress.Instance?.GetItemLevel(GetItemID()) ?? 1;

            // CONCEITO: GetResistanceAtLevel(level) aplica resistanceScaling configurado no SO.
            // level 1 = valor base do SO; level N = valor base * (1 + resistanceScaling * (N-1)).
            maxHealth = barricadeData.GetResistanceAtLevel(level);
            currentHealth = maxHealth;

            Debug.Log($"[Barricade] Initialized: maxHealth={maxHealth}, level={level}, itemID={GetItemID()}");
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
            UpdateBarricadeVisual();

            if (currentHealth <= 0f) {
                DestroyBarricade();
            }
        }

        /// <summary>
        /// Swaps the barricade's material based on current health percentage.
        /// CONCEITO: Ao invés de tintar a cor via código, trocamos o material inteiro.
        /// Isso permite usar texturas com rachaduras progressivas definidas pelo artista.
        /// Índices: 0=Intact (>66%), 1=Damaged (>33%), 2=Heavy (>0%), 3=Critical (<=0%).
        /// </summary>
        private void UpdateBarricadeVisual() {
            if (barricadeRenderer == null) return;
            if (damageStateMaterials == null || damageStateMaterials.Length == 0) return;

            float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;

            // CONCEITO: Mapeia a fração de vida para um índice no array de materiais.
            // Se o array não tiver material suficiente para o índice calculado,
            // usa o último material disponível como fallback.
            int index;
            if (healthPercent > 0.66f)
                index = 0;
            else if (healthPercent > 0.33f)
                index = 1;
            else if (healthPercent > 0f)
                index = 2;
            else
                index = 3;

            // Clamp: se o array for menor que 4, usa o último elemento como fallback
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
    }
}