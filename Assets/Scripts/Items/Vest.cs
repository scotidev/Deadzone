using UnityEngine;
using InfimaGames.LowPolyShooterPack;

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
    public class Vest : ItemBehaviour {
        
#region SERIALIZED FIELDS
    /*-----------------------------------------------------------------------------
        SERIALIZED FIELDS são variáveis que aparecem no Inspector do Unity.
        Usamos [SerializeField] para forçar Unity a mostrar variáveis private.
    -----------------------------------------------------------------------------*/
    
    [SerializeField] private VestDataSO vestData;
    [SerializeField] private float damageReductionPercentage = 0.1f;  // 10% reduction base
    [SerializeField] private float exclusiveDamageReductionPercentage = 0.2f;  // 20% reduction exclusive
    
    /*-----------------------------------------------------------------------------
        Audio Clips - Aqui você arrasta os arquivos de áudio no Inspector.
        Esses sons tocarão em momentos específicos do jogo.
    -----------------------------------------------------------------------------*/
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
            return vestData.itemID;
        }
        
        // GetDisplayName() retorna o nome shown na UI
        public override string GetDisplayName() {
            if (vestData == null) return "Unknown";
            return vestData.itemName;
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
        /// Vest não tem ação "exclusive" de uso.
        /// Exclusive apenas aumenta a redução para 20%.
        /// </summary>
        public override void OnUseExclusive() {
            // Vest é passivo, exclusive significa melhor redução
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
        /// Verifica se Vest tem upgrade exclusive (máximo nível).
        /// </summary>
        public override bool HasExclusiveUnlocked() {
            if (PlayerProgress.Instance == null) {
                return false;
            }
            
            int level = PlayerProgress.Instance.GetItemLevel(GetItemID());
            int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(GetItemID());
            return level >= maxLevel;
        }

        /// <summary>
        /// Get damage reduction percentage for this vest.
        /// Used by PlayerHealth or PlayerArmor to reduce incoming damage.
        /// </summary>
        public float GetDamageReductionPercentage() {
            // If at max level, use exclusive reduction, otherwise use normal
            return HasExclusiveUnlocked() ? exclusiveDamageReductionPercentage : damageReductionPercentage;
        }
        
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
    }
}