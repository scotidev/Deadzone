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

    // [BUG VEST] Sistema de regeneração do exclusivo da Vest (nível máximo)
    private bool canRegenerate;                  // Se a regeneração está ativa
    private float lastDamageTime;                // Momento do último dano (usado quando NÃO está na zona)
    private float lastRegenTime;                 // Último tick de regeneração
    private const float REGEN_DELAY = 5f;        // Segundos sem dano para começar regeneração
    private const float REGEN_RATE = 2f;         // Armor regenerado por segundo
    private const float REGEN_INTERVAL = 0.5f;  // Intervalo entre cada tick de regeneração
    private bool wasArmorZero = false;           // Rastreia se a armadura estava em 0
    
    // [BUG VEST] NOVOS CAMPOS PARA DETECÇÃO DE SAÍDA DA ZONA DE DANO:
    
    // Momento em que o jogador saiu da zona de dano (nevoa/poison)
    // Se for 0, significa que ainda não saiu ou já re-equipou
    private float timeExitedDamageZone = 0f;
    
    // Indica se o jogador ESTAVA na zona de dano no frame anterior
    // Usado para detectar quando ele SAIU da zona
    private bool wasInDamageZone = false;
    
    // Contador de frames fora da zona de dano
    // Precisamos de alguns frames consecutivos fora para CONFIRMAR saída
    // (evita falsos positivos quando o jogador "pisca" na borda da nevoa)
    private int framesExitedDamageZone = 0;
    
    // Mínimo de frames fora da zona para confirmar saída
    // 2 frames = ~0.033 segundos em 60fps (evita erros de colisão)
    private const int DAMAGE_ZONE_EXIT_FRAMES = 2;

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

        // [BUG VEST] Se tomou qualquer dano, marca o tempo
        // Isso é usado para detectar dano enquanto espera a regeneração
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
    /// [BUG VEST] Ativa a regeneração automática da armadura.
    /// Chamado quando a Vest alcanza o nível máximo (upgrade exclusivo).
    /// </summary>
    public void EnableRegeneration() {
        canRegenerate = true;
        Debug.Log("[PlayerArmor] Regeneration enabled!");
    }

    /// <summary>
    /// Desativa a regeneração automática da armadura.
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

    /*============================================================================
        [BUG VEST] SEÇÃO DE REGENERAÇÃO - EXPLICAÇÃO DIDÁTICA
        
        PROBLEMA ORIGINAL:
        A regeneration era baseada em lastDamageTime, que era setado toda vez que o
        jogador tomava dano. Se o jogador ficasse na nevoa até a armadura zerar,
        o último dano era muito recente, e o timer já começava alto.
        
        Por exemplo:
        - Frame 100: jogador na nevoa, último dano em t=100.0
        - Frame 150: armadura chega a 0
        - Frame 151: jogador sai da nevoa
        - No Update, timeSinceDamage = Time.time - 100.0 = 50 segundos?!?!
        
        O bug era que não diferenciávamos "estou no dano" vs "já saí do dano".
        
        SOLUÇÃO:
        Usamos dois campos novos: wasInDamageZone e timeExitedDamageZone.
        A lógica agora é:
        1. Mientras está na zona de dano (poison/fog), NÃO conta tempo
        2. Quando sai da zona, esperamos 2 frames para confirmar (evita falsos positivos)
        3. Só DEPOIS de confirmar saída, começamos a contar os 5 segundos
        4. Se entrar de volta na zona, resetamos tudo
        5. Se tomar dano enquanto espera, resetamos o timer
    ============================================================================*/

    private void Update() {
        // Não regenera se o jogador estiver morto
        if (playerHealth != null && !playerHealth.IsAlive()) {
            return;
        }

        // [BUG VEST] Se regeneração não está ativa, limpa os estados e sai
        if (!canRegenerate) {
            wasArmorZero = false;
            timeExitedDamageZone = 0f;      // Reseta o timer de saída
            framesExitedDamageZone = 0;     // Reseta o contador de frames
            return;
        }

        // [BUG VEST] Verifica se o jogador está atualmente na zona de dano (nevoa)
        bool isInDamageZone = playerHealth != null && playerHealth.IsInPoison;

        // [BUG VEST] DETECÇÃO DE SAÍDA DA ZONA DE DANO:
        // Esta seção detecta quando o jogador acabou de sair da nevoa
        if (wasInDamageZone && !isInDamageZone) {
            // Jogador acabou de sair da zona de dano
            // Começamos a contar frames fora para confirmar que realmente saiu
            framesExitedDamageZone = 1;
        } else if (!isInDamageZone && framesExitedDamageZone > 0 && framesExitedDamageZone < DAMAGE_ZONE_EXIT_FRAMES) {
            // Ainda está fora da zona mas não confirmou saída ainda (menos de 2 frames)
            // Incrementa o contador de frames
            framesExitedDamageZone++;
        }
        
        // [BUG VEST] Se o jogador ENTROU de volta na zona de dano, reseta tudo
        // Isso cobre o caso de o jogador sair e voltar muito rápido
        if (!wasInDamageZone && isInDamageZone) {
            framesExitedDamageZone = 0;     // Reseta contador de frames
            timeExitedDamageZone = 0f;      // Reseta timer de saída
        }
        
        // Atualiza o estado anterior para o próximo frame
        wasInDamageZone = isInDamageZone;

        // [BUG VEST] Se ainda está na zona de dano OU ainda não confirmou saída,
        // não conta o tempo (evita que o timer comece antes da hora)
        if (isInDamageZone || framesExitedDamageZone < DAMAGE_ZONE_EXIT_FRAMES) {
            return;
        }

        // [BUG VEST] INICIAR TIMER APÓS CONFIRMAR SAÍDA:
        // Só开始a contar depois que confirmou que o jogador realmente saiu
        // (2 frames consecutivos fora da zona de dano)
        if (timeExitedDamageZone <= 0f && framesExitedDamageZone >= DAMAGE_ZONE_EXIT_FRAMES) {
            timeExitedDamageZone = Time.time;
            Debug.Log($"[PlayerArmor] Confirmed exit from damage zone, starting timer at {Time.time:F2}");
        }

        // [BUG VEST] SE TOMOU DANO ENQUANTO AGUARDA, RESET O TIMER:
        // Se o jogador levou dano nos últimos 0.5 segundos enquanto esperava,
        // significa que ainda está em perigo, então resetamos o timer
        if (timeExitedDamageZone > 0f && (Time.time - lastDamageTime) < 0.5f) {
            timeExitedDamageZone = Time.time;
        }

        // [BUG VEST] CALCULA O TEMPO EFETIVO:
        // Se temos timeExitedDamageZone, usa ele (tempo desde saída da zona)
        // Se não, usa lastDamageTime (para casos onde não passou pela zona)
        float effectiveTime = 0f;
        if (timeExitedDamageZone > 0f) {
            effectiveTime = Time.time - timeExitedDamageZone;
        } else {
            effectiveTime = Time.time - lastDamageTime;
        }

        // [BUG VEST] SE ARMADURA ESTÁ EM 0 E TEM EXCLUSIVO, RE-EQUIPA APÓS 5s
        if (currentArmor <= 0f) {
            if (effectiveTime >= REGEN_DELAY) {
                Debug.Log($"[PlayerArmor] {effectiveTime:F2}s since confirmed exit, re-equipping!");
                ReEquipVest();
                timeExitedDamageZone = 0f;      // Limpa timer após re-equipar
                framesExitedDamageZone = 0;      // Limpa contador
                wasArmorZero = true;
            }
            return;
        }

        // Se a armadura já está cheia, não precisa regenerar
        if (currentArmor >= maxArmor) {
            wasArmorZero = false;
            return;
        }

        // [BUG VEST] RASTREIA SE COMEÇOU A REGENERAR A PARTIR DE 0
        if (wasArmorZero && currentArmor > 0f) {
            Debug.Log("[PlayerArmor] Vest re-equipped - showing HUD!");
            if (vestComponent != null) {
                vestComponent.TriggerRegeneratedEvent();
            }
            wasArmorZero = false;
            timeExitedDamageZone = 0f;      // Limpa timer
            framesExitedDamageZone = 0;     // Limpa contador
        }

        // [BUG VEST] APLICA REGENERAÇÃO EM INTERVALOS REGULARES
        if (effectiveTime >= REGEN_DELAY) {
            if (Time.time - lastRegenTime >= REGEN_INTERVAL) {
                lastRegenTime = Time.time;
                float armorToAdd = REGEN_RATE * REGEN_INTERVAL;
                currentArmor = Mathf.Min(maxArmor, currentArmor + armorToAdd);
                OnArmorChanged?.Invoke(currentArmor / maxArmor);

                if (currentArmor >= maxArmor) {
                    Debug.Log("[PlayerArmor] Armor fully regenerated!");
                }
            }
        }
    }

    /*============================================================================
        [BUG VEST] ReEquipVest() - Explicação do método
        
        Este método é responsável por "re-equipar" a vest quando ela foi destruída
        e o jogador tem o upgrade exclusivo ativo.
        
        ANTES DO BUG:
        A regeneração não funcionava corretamente porque o timer começava muito
        antes de o jogador sair da zona de dano.
        
        DEPOIS DO BUG:
        O timer só começa depois que o jogador confirma que saiu da zona de dano
        (2 frames consecutivos fora), garantindo que os 5 segundos são contados
        corretamente no tempo real.
        
        O QUE ESTE MÉTODO FAZ:
        1. Calcula o máximo de armor baseado no nível atual da Vest
        2. Define currentArmor e maxArmor para esse valor
        3. Notifica a UI que a armadura mudou (atualiza a barra visual)
        4. Dispara o evento de regeneração para mostrar HUD e tocar som
    ============================================================================*/

    /// <summary>
    /// [BUG VEST] Re-equipa a vest quando a armadura está em 0.
    /// Chamado automaticamente quando o exclusivo está ativo e 5 segundos
    /// passam sem dano desde que o jogador saiu da zona de dano.
    /// </summary>
    private void ReEquipVest() {
        float maxArmorValue = GetMaxArmorFromVest();
        currentArmor = maxArmorValue;
        maxArmor = maxArmorValue;
        
        Debug.Log($"[PlayerArmor] Vest re-equipped! New max armor: {maxArmorValue}");
        
        // Notifica a UI que a armadura mudou
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
        
        // [BUG VEST] Dispara o evento de regeneração para mostrar HUD e tocar som
        // Este evento é ouvido pelo PlayerArmorUI que mostra a barra e toca o som
        if (vestComponent != null) {
            vestComponent.TriggerRegeneratedEvent();
        }
    }

    #endregion
}