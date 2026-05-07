using UnityEngine;
using InfimaGames.LowPolyShooterPack;

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
    public class Vest : ItemBehaviour {
        
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