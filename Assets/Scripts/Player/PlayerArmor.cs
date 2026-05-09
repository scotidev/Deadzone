using System;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/*============================================================================
    PlayerArmor.cs - Script de Armadura do Jogador
    
    Este script gerencia a "barra de armadura" do jogador, que funciona como um escudo.
    Quando você recebe dano, primeiro a armadura absorve, e só depois o dano
    vai para a barra devida (HP).
    
    FLUXO DE DANO:
    1. Jogador recebe 30 de dano
    2. Se armadura >= 30, armadura absorve tudo (ainda sobram 0)
    3. Se armadura < 30 (ex: 20), armadura absorve 20, resto vai para HP (10)
    4. Se armadura = 0, todo dano vai para HP
    
    POR QUE USAMOS EVENTOS?
    - Eventos permitem que scripts diferentes "conversem" sem se conhecerem
    - PlayerArmorUI escuta "OnArmorChanged" para atualizar a barra visual
    - ShopUI escuta para habilitar/desabilitar botão de reparo
============================================================================*/

/*============================================================================
    [BUG VEST] - CORREÇÃO DO BUG DE REGENERAÇÃO DO EXCLUSIVO DA VEST
    
    PROBLEMA ORIGINAL:
    O jogador ficava na nevoa (poison damage) e a armadura era destruída.
    Quando saía da nevoa, a vest deveria esperar 5 segundos para re-equipar,
    mas estava re-equipando quase que instantaneamente.
    
    CAUSA DO BUG:
    O dano da nevoa é contínuo ( PoisonTick - uma coroutine que dá dano a cada X segundos).
    Quando a armadura chegava a 0, o lastDamageTime já estava muito alto porque o último
    tick de dano tinha acabado de acontecer. Então o timer considerava que já tinha
    passado tempo desde "último dano", mas na verdade o jogador ainda estava no dano
    (ou acabou de sair há poucos frames).
    
    A LÓGICA ANTIGA USAVA:
    - lastDamageTime = Time.time do último AbsorbDamage
    - timeSinceDamage = Time.time - lastDamageTime
    - Problema: não distinguia "estou no dano" vs "já saí do dano"
    
    SOLUÇÃO IMPLEMENTADA:
    1. Adicionamos um sistema de detecção de saída da zona de dano (nevoa)
    2. Só começamos a contar os 5 segundos DEPOIS que o jogador CONFIRMA que saiu
       da zona de dano (2 frames consecutivos fora)
    3. Se o jogador entrar de volta na nevoa, resetamos o timer
    4. Se tomar dano enquanto espera, resetamos o timer
    
    FLUXO CORRETO:
    - Jogador entra na nevoa → não conta tempo
    - Jogador sai da nevoa → espera 2 frames para confirmar saída
    - Após confirmar saída → começa contar 5 segundos
    - Se entrar na nevoa de novo → para de contar, reset timer
    - Se tomar dano enquanto espera → reset timer
    - Após 5 segundos → re-equipa a vest
    
============================================================================*/

/// <summary>
/// Manages the player's armor (vest). Acts as a shield that absorbs damage before health.
/// When the player has active armor, damage is applied to armor first. Once armor reaches zero,
/// damage passes through to health.
/// </summary>
public class PlayerArmor : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Armor Settings")]
    [SerializeField] private float maxArmor = 100f;  // Armadura máxima (100 = cheia)

    #endregion

    #region FIELDS

    // Armadura atual do jogador
    private float currentArmor;
    
    // Referência ao script da Vest (para tocar sons)
    // Precisamos procurar porque pode estar em outro GameObject
    private Vest vestComponent;

    // Referência ao PlayerHealth para verificar se o player está vivo
    private PlayerHealth playerHealth;

    // EVENTOS: permitem que outros scripts saibam quando algo mudar
    // Isso é usado pelo PlayerArmorUI para atualizar a barra
    public event Action<float> OnArmorChanged;    // Disparado quando armadura muda
    public event Action OnArmorDepleted;       // Disparado quando armadura chega a 0

    #endregion

    #region UNITY

    /*-----------------------------------------------------------------------------
        Awake() é chamado uma vez quando o objeto é criado.
        Inicializamos a armadura aqui.
    -----------------------------------------------------------------------------*/
    private void Awake() {
        // O jogador COMEÇA sem armadura (0)
        // Só vai ter armadura quando desbloquear na loja
        currentArmor = 0f;
        
        // Procurar o componente Vest no jogador ou seus filhos
        // Primeiro tenta no mesmo GameObject
        if (GetComponent<Vest>() != null) {
            vestComponent = GetComponent<Vest>();
        } else {
            // Se não encontrou, procurar nos filhos do Character
            vestComponent = GetComponentInParent<Character>()?.GetComponentInChildren<Vest>();
        }

        // Procurar oPlayerHealth
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) {
            playerHealth = GetComponentInParent<Character>()?.GetComponentInChildren<PlayerHealth>();
        }
    }

    private void Start() {
        InitializeArmorFromVestLevel();
    }

    /// <summary>
    /// Initializes armor based on current vest level. Called at Start for games where vest is already unlocked.
    /// </summary>
    private void InitializeArmorFromVestLevel() {
        if (vestComponent != null && PlayerProgress.Instance != null) {
            string vestID = vestComponent.GetItemID();
            if (PlayerProgress.Instance.IsItemUnlocked(vestID)) {
                float maxArmorFromLevel = GetMaxArmorFromVestLevel();
                maxArmor = maxArmorFromLevel;
                currentArmor = maxArmorFromLevel;
                OnArmorChanged?.Invoke(1f);
            }
        }
    }

    #endregion
    
    #region MÉTODOS PÚBLICOS
    /*-----------------------------------------------------------------------------
        Métodos públicos que outros scripts podem chamar.
        São o que chamamos de "API" do script.
    -----------------------------------------------------------------------------*/
    
    /// <summary>
    /// Equipa o colete quando o jogador desbloqueia na loja.
    /// Define armadura para o máximo baseado no nível do Vest e notifica a UI.
    /// </summary>
    public void EquipVest() {
        float maxArmorValue = GetMaxArmorFromVestLevel();
        currentArmor = maxArmorValue;
        maxArmor = maxArmorValue;
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
        
        if (vestComponent != null) {
            vestComponent.PlayEquippedSound();
        }
    }
    
    /// <summary>
    /// Called when vest is upgraded. Updates maxArmor to new level and fills armor to 100%.
    /// </summary>
    public void OnVestUpgraded() {
        float newMaxArmor = GetMaxArmorFromVestLevel();
        maxArmor = newMaxArmor;
        currentArmor = maxArmor;
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
        
        if (vestComponent != null) {
            vestComponent.PlayEquippedSound();
        }
    }

    /// <summary>
    /// Gets the maximum armor value based on Vest's current level.
    /// </summary>
    public float GetMaxArmorFromVestLevel() {
        if (PlayerProgress.Instance == null || vestComponent == null) {
            return 100f;
        }
        
        string vestID = vestComponent.GetItemID();
        int level = PlayerProgress.Instance.GetItemLevel(vestID);
        
        var vestData = vestComponent.VestData;
        if (vestData != null) {
            return vestData.GetResistanceAtLevel(level);
        }
        
        return 100f;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Absorbs damage from the armor. Returns the remaining damage that wasn't absorbed.
    /// Se armadura absorve todo dano, retorna 0. Se armadura é destruída, retorna o dano excedente.
    /// 
    /// Exemplo: currentArmor = 20, incomingDamage = 30
    /// absorbedDamage = min(20, 30) = 20
    /// remainingDamage = 30 - 20 = 10 (esse 10 vai para a barra de vida)
    /// </summary>
    public float AbsorbDamage(float incomingDamage) {
        // Se armadura já está vazia, todo dano vai para HP
        if (currentArmor <= 0f) {
            return incomingDamage;
        }

        // Mathf.Min pega o menor valor entre armadura atual e dano recebido
        // Isso garante que não tiramos mais armadura do que temos
        float absorbedDamage = Mathf.Min(currentArmor, incomingDamage);

        // Subtrai o dano absorvido da armadura atual
        currentArmor -= absorbedDamage;

        // Calcula quanto dano sobrou (vai para HP)
        float remainingDamage = incomingDamage - absorbedDamage;

        // Notifica a UI que armadura mudou (atualiza a barra visual)
        // Dividimos por maxArmor para получить fração entre 0 e 1
        OnArmorChanged?.Invoke(currentArmor / maxArmor);

        // Se armadura chegou a 0, dispara evento especial
        if (currentArmor <= 0f) {
            currentArmor = 0f;
            OnArmorDepleted?.Invoke();
            
            // TOCAR som APENAS se a vest está desbloqueada (não apenas existir no prefab)
            if (vestComponent != null && PlayerProgress.Instance != null) {
                string vestID = vestComponent.GetItemID();
                if (PlayerProgress.Instance.IsItemUnlocked(vestID)) {
                    vestComponent.PlayDestroyedSound();
                }
            }
        }

        return remainingDamage;
    }

/// <summary>
        /// Adds armor points without exceeding maxArmor.
        /// Called when purchasing armor from the shop.
        /// </summary>
        public void AddArmor(float amount) {
        currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
    }

    /// <summary>
    /// Repairs armor by the specified amount, without exceeding maxArmor.
    /// </summary>
    public void RepairArmor(float amount) {
        currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
    }

    /// <summary>
    /// Returns the current armor as a fraction between 0 and 1.
    /// </summary>
    public float GetArmorFraction() => currentArmor / maxArmor;

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
    /// Usado para verificar se pode usar colete.
    /// </summary>
    public bool HasArmor() => currentArmor > 0f;

    #endregion
}