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

    // Regeneration system for Vest exclusive (level 5+)
    private bool canRegenerate;
    private float lastDamageTime;
    private float lastRegenTime;
    private const float REGEN_DELAY = 5f;          // Seconds without damage before regeneration starts
    private const float REGEN_RATE = 2f;          // Armor regenerated per second
    private const float REGEN_INTERVAL = 0.5f;    // How often to apply regeneration

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
        float maxArmorValue = GetMaxArmorFromVest();
        currentArmor = maxArmorValue;
        maxArmor = maxArmorValue;
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
    }
    
    /// <summary>
    /// Repara o colete quando o jogador usa botão +ammo na loja.
    /// </summary>
    public void RepairVest() {
        float maxArmorValue = GetMaxArmorFromVest();
        currentArmor = maxArmorValue;
        maxArmor = maxArmorValue;
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
    }

    /// <summary>
    /// Gets the maximum armor value based on Vest's current level.
    /// </summary>
    private float GetMaxArmorFromVest() {
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

        // If we took any damage, track it and disable regeneration
        if (absorbedDamage > 0f) {
            lastDamageTime = Time.time;
            if (canRegenerate) {
                Debug.Log("[PlayerArmor] Damage taken - regeneration paused");
            }
        }

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
        // Mathf.Min garante que não passa de 100
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

    /// <summary>
    /// Enables the automatic regeneration feature for Vest exclusive (level 5+).
    /// Called when Vest reaches max upgrade level.
    /// </summary>
    public void EnableRegeneration() {
        canRegenerate = true;
        Debug.Log("[PlayerArmor] Regeneration enabled!");
    }

    /// <summary>
    /// Disables the automatic regeneration feature.
    /// Called when player takes damage while regenerating.
    /// </summary>
    public void DisableRegeneration() {
        canRegenerate = false;
        lastRegenTime = 0f;
    }

    /// <summary>
    /// Returns whether regeneration is currently enabled.
    /// </summary>
    public bool IsRegeneratingEnabled() => canRegenerate;

    #endregion

    #region REGENERATION

    private void Update() {
        // Don't regenerate if player is dead
        if (playerHealth != null && !playerHealth.IsAlive()) {
            return;
        }

        // Don't regenerate if regeneration is not enabled or armor is full
        if (!canRegenerate || currentArmor <= 0f || currentArmor >= maxArmor) {
            return;
        }

        float timeSinceDamage = Time.time - lastDamageTime;

        // If 5 seconds passed without taking damage, start regenerating
        if (timeSinceDamage >= REGEN_DELAY) {
            // Apply regeneration at regular intervals
            if (Time.time - lastRegenTime >= REGEN_INTERVAL) {
                lastRegenTime = Time.time;
                float armorToAdd = REGEN_RATE * REGEN_INTERVAL;
                currentArmor = Mathf.Min(maxArmor, currentArmor + armorToAdd);
                OnArmorChanged?.Invoke(currentArmor / maxArmor);

                // If fully regenerated, stop regenerating
                if (currentArmor >= maxArmor) {
                    Debug.Log("[PlayerArmor] Armor fully regenerated!");
                }
            }
        }
    }

    #endregion
}