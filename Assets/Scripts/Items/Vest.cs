using System;
using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;

/*============================================================================
    [BUG VEST] - CORREÇÃO DO BUG DE REGENERAÇÃO DO EXCLUSIVO DA VEST
    
    PROBLEMA ORIGINAL:
    O PlayerArmor consegue re-equipar a vest automaticamente após 5 segundos
    fora da zona de dano, MAS o HUD não voltava a aparecer e o som não
    tocava porque não havia comunicação entre o sistema de regeneração e
    o sistema de UI.
    
    SOLUÇÃO IMPLEMENTADA:
    1. Criamos um NOVO evento estático: Vest.OnVestRegenerated
    2. Criamos um método público: TriggerRegeneratedEvent()
    3. O PlayerArmor chama TriggerRegeneratedEvent() quando re-equipa a vest
    4. Este método dispara o evento estático OnVestRegenerated
    5. O PlayerArmorUI se inscreve neste evento e responde mostrando o HUD + som
    
    FLUXO COMPLETO:
    PlayerArmor.Update()
        → detecta 5s fora da zona de dano com armor = 0
        → chama ReEquipVest()
        → chama vestComponent.TriggerRegeneratedEvent()
        → TriggerRegeneratedEvent() dispara Vest.OnVestRegenerated
        → PlayerArmorUI.OnVestRegenerated() é chamado
        → Mostramos HUD + tocamos som
    
============================================================================*/

/*============================================================================
    Vest.cs - Script do Item Colete (Armor)
    
    Este script controla o item "Colete" do jogo (Item 9).
    
    CARACTERÍSTICAS DO VEST:
    - Não é selecionável pelo jogador (não aparece nas teclas 1-8)
    - É "equipado automaticamente" quando desbloqueado na loja
    - Fornece redução de dano ao jogador
    - Tiene uma barra de armor no HUD que é destruída ao receber dano suficiente
    
    POR QUE USAMOS "ScriptableObject" (VestDataSO)?
    - Permite criar o dado do item no editor Unity sem escrever código
    - Facilita balancing (preços, níveis, descrições) sem compilar
    
    O sistema usa "ServiceLocator" para tocarsons de forma centralizada,
    isso garante que o som toque em qualquer cena do jogo.
============================================================================*/

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Vest armor item. Auto-equipped when unlocked/upgraded.
    /// NOT selectable via keys 1-8 (not added to Inventory.selectableItems).
    /// Provides armor damage reduction.
    /// </summary>
    public class Vest : ItemBehaviour, IShopItemCallback {
        
#region SERIALIZED FIELDS
    /*-----------------------------------------------------------------------------
        SERIALIZED FIELDS são variáveis que aparecem no Inspector do Unity.
        Usamos [SerializeField] para forçar Unity a mostrar variáveis private.
    -----------------------------------------------------------------------------*/
    
    [SerializeField] private VestDataSO vestData;
    [SerializeField] private float damageReductionPercentage = 0.1f;  // 10% reduction
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip vestEquippedClip;    // Som quando equipar/equipar
    [SerializeField] private AudioClip vestDestroyedClip; // Som quando o colete quebra
    
    #endregion
    
    #region FIELDS
    /*-----------------------------------------------------------------------------
        FIELDS são variáveis privadas que não aparecem no Inspector.
        Usamos para guardar referências necessárias no código.
    -----------------------------------------------------------------------------*/

    // IAudioManagerService é a interface do sistema de áudio do jogo.
    // Permite tocar sons de forma centralizada usando ServiceLocator.
    private IAudioManagerService audioService;

    // Referência ao PlayerHealth para verificar se o player está vivo
    private PlayerHealth playerHealth;

    // Armadura atual e máxima do jogador
    private float currentArmor;
    private float maxArmor;

    // EVENTOS: permitem que outros scripts saibam quando algo mudar
    public event Action<float> OnArmorChanged;
    public event Action OnArmorDepleted;

    #endregion

    #region PROPERTIES

    /// <summary>
    /// Public accessor to vest data for other scripts (like PlayerArmor).
    /// </summary>
    public VestDataSO VestData => vestData;

    #endregion
        
        #region EVENTS
        /*-----------------------------------------------------------------------------
            EVENTOS servem para notificar outros scripts sobre algo que aconteceu.
            Outros scripts podem "assinar" esses eventos para executar código quando ocorrer.
        -----------------------------------------------------------------------------*/
        
        // Evento estático - usado quando o colete é destruído
        // Outros scripts podem ouvir isso para atualizar a UI, por exemplo
        public static event System.Action OnVestDestroyed;

        #endregion

        #region UNITY
        
        /*-----------------------------------------------------------------------------
            Awake() é chamado uma vez quando o objeto é criado na memória.
            É usado para inicializar referências e configurações iniciais.
        -----------------------------------------------------------------------------*/
        private void Awake() {
            // ServiceLocator.Current.Get<IAudioManagerService>() pega o serviço de áudio
            // que foi registrado no Bootstraper do jogo.
            // Isso garante que sempre teremos acesso ao sistema de áudio.
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            
            // Inscreve no evento de upgrade do UpgradeManager para atualizar armor quando a vest subir de nível
            UpgradeManager.OnItemUpgraded += OnUpgradeManagerItemUpgraded;

            // O jogador COMEÇA sem armadura (0)
            // Só vai ter armadura quando desbloquear na loja
            currentArmor = 0f;

            // Procurar o PlayerHealth
            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth == null) {
                playerHealth = GetComponentInParent<Character>()?.GetComponentInChildren<PlayerHealth>();
            }
        }

private void Start() {
            InitializeArmorFromVestLevel();
        }

        private void OnDestroy() {
            // Desinscreve do evento de upgrade para evitar memory leaks
            UpgradeManager.OnItemUpgraded -= OnUpgradeManagerItemUpgraded;
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

        /// <summary>
        /// Called when UpgradeManager emits an upgrade event. Checks if this vest was upgraded and updates armor.
        /// </summary>
        private void OnUpgradeManagerItemUpgraded(string itemID, ItemDataSO itemData) {
            string myID = GetItemID();
            string receivedID = itemID ?? "null";
            bool idsMatch = (myID == receivedID);
            bool isVestData = (itemData is VestDataSO);
            
            Debug.Log($"[Vest] ⚠️ OnUpgradeManagerItemUpgraded called!");
            Debug.Log($"[Vest]   My ID: '{myID}' (Type: {this.GetType().Name})");
            Debug.Log($"[Vest]   Received ID: '{receivedID}' (itemData type: {itemData?.GetType().Name})");
            Debug.Log($"[Vest]   IDs match: {idsMatch}, Is VestDataSO: {isVestData}");
            Debug.Log($"[Vest]   String comparison: '{myID}' == '{receivedID}' ? {string.Equals(myID, receivedID)}");
            
            if (idsMatch && isVestData) {
                Debug.Log($"[Vest] ✓ Condition met! Playing equipped sound for {myID}");
                OnUpgraded();
            } else {
                Debug.Log($"[Vest] ✗ Condition NOT met. Not playing sound.");
            }
        }
        
        #endregion

        #region ITEM BEHAVIOUR IMPLEMENTATION
        /*-----------------------------------------------------------------------------
            ITEM BEHAVIOUR IMPLEMENTATION - Métodos da classe base ItemBehaviour
            Estes métodos implementam a interface que todo item do jogo precisa ter.
        -----------------------------------------------------------------------------*/
        
        // GetItemID() retorna o ID único do item
        // O sistema de progresso usa isso para salvar/carregar o estado do item
        public override string GetItemID() {
            if (vestData == null) {
                Debug.LogWarning("[Vest] vestData é null! Configure no Inspector.", gameObject);
                return "vest_null";
            }
            return vestData.ItemID;
        }
        
        // GetDisplayName() retorna o nome shown na UI
        public override string GetDisplayName() {
            if (vestData == null) return "Unknown";
            return vestData.ItemName;
        }

        /// <summary>
        /// Vest não tem ícone HUD (não é selecionável).
        /// </summary>
        public override Sprite GetIcon() {
            return null;
        }

        /// <summary>
        /// Vest não responde a seleção (nunca chamado).
        /// É equipadode forma automática, não manualmente.
        /// </summary>
        public override void OnSelected() {
            // Vest é equipadode forma automática, não selecionável pelas teclas
        }

        /// <summary>
        /// Vest não responde a deseleção.
        /// </summary>
        public override void OnDeselected() {
            // Vest sempre está equipado
        }

        /// <summary>
        /// Vest não tem ação de "usar".
        /// Ele fornece redução de daman passiva.
        /// </summary>
        public override void OnUse() {
            // Vest é passivo, sem ação de uso
        }

        /// <summary>
        /// Vest pode sempre ser "usado" (sempre está equipado).
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                return false;
            }
            
            // Verifica se Vest está desbloqueado
            return PlayerProgress.Instance.IsItemUnlocked(GetItemID());
        }

        /// <summary>
        /// Get damage reduction percentage for this vest.
        /// Used by PlayerHealth or PlayerArmor to reduce incoming damage.
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
        /// Equipa o colete quando o jogador desbloqueia na loja.
        /// Define armadura para o máximo baseado no nível do Vest e notifica a UI.
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
        /// Absorbs damage from the armor. Returns the remaining damage that wasn't absorbed.
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
        public void AddArmor(float amount, bool playSound = true) {
            currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
            OnArmorChanged?.Invoke(currentArmor / maxArmor);
            
            if (playSound) {
                PlayEquippedSound();
            }
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
        /// Checks if the player currently has any armor.
        /// </summary>
        public bool HasArmor() => currentArmor > 0f;

        #endregion
        
        #region AUDIO
        /*-----------------------------------------------------------------------------
            AUDIO - Métodos para tocarsons
            Estes métodos são chamados pelo ShopUI quando o jogador
            compra/equipa/repara o colete.
        -----------------------------------------------------------------------------*/
        
        /// <summary>
        /// Plays the vest equipped sound effect.
        /// Called when player unlocks/buys the vest from shop.
        /// </summary>
        public void PlayEquippedSound() {
            // Verificamos se o clip e o serviço existem antes de tocar
            // Isso evita erros se você esqueceu de arrastar o áudio no Inspector
            if (vestEquippedClip != null && audioService != null) {
                // PlaySFX2D toca som 2D (não espacial, same volume em qualquer lugar)
                // Isso é usado para sons de UI e feedback do jogador
                audioService.PlaySFX2D(vestEquippedClip);
            }
        }

        /// <summary>
        /// Plays the vest destroyed sound effect.
        /// Called when the vest is destroyed (armor reaches 0).
        /// </summary>
        public void PlayDestroyedSound() {
            // Toca som de destruir
            if (vestDestroyedClip != null && audioService != null) {
                audioService.PlaySFX2D(vestDestroyedClip);
            }
            
            // Dispara o evento para notificar outros scripts (ex: PlayerArmorUI)
            // Isso permite que a UI seja atualizada quando colete quebra
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
        /// BUG: This is being called for ALL items, not just the vest!
        /// </summary>
        public void OnShopUpgrade() {
            Debug.Log($"[Vest] ⚠️ OnShopUpgrade() called! This should ONLY be called for the VEST!");
            Debug.Log($"[Vest] Stack trace: {System.Environment.StackTrace}");
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
    }
}