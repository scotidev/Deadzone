using System;
using UnityEngine;

/// <summary>
/// Manages the player's armor (vest/colete). Acts as a shield that absorbs damage before health.
/// When the player has active armor, damage is applied to armor first. Once armor reaches zero,
/// damage passes through to health.
/// </summary>
public class PlayerArmor : MonoBehaviour {

    [Header("Armor Settings")]
    [Tooltip("Maximum armor points.")]
    // [SerializeField] permite que a variável apareça no Inspector mesmo sendo private
    // Isso mantém o encapsulamento (boa prática) mas permite configuração visual
    [SerializeField] private float maxArmor = 100f;

    // Valor atual do armor - começa cheio por enquanto para testes
    // Depois quando tiver shop, vai começar em 0 até o player comprar
    private float currentArmor;

    // Event (evento) é um padrão de design para comunicação entre scripts
    // Quando o armor muda, qualquer script inscrito neste evento é notificado
    // Action<float> significa: um evento que passa um float como parâmetro
    public event Action<float> OnArmorChanged;

    // Evento disparado quando o armor chega a zero
    // Action (sem parâmetros) significa: um evento que não passa nenhum valor
    public event Action OnArmorDepleted;

    // Awake() é chamado quando o objeto é criado, antes do Start()
    // É o melhor lugar para inicializar variáveis internas do componente
    private void Awake() {
        // Por enquanto, inicializamos com armor completo para testes
        // Atribuímos o valor de maxArmor (100) para currentArmor
        // Depois, quando tiver shop, isso será: currentArmor = 0;
        currentArmor = maxArmor;
    }

    /// <summary>
    /// Absorbs damage from the armor. Returns the remaining damage that wasn't absorbed.
    /// If armor absorbs all damage, returns 0. If armor is depleted, returns the overflow damage.
    /// </summary>
    /// <param name="incomingDamage">The amount of damage to absorb</param>
    /// <returns>The amount of damage that wasn't absorbed by armor</returns>
    public float AbsorbDamage(float incomingDamage) {
        // Primeiro checamos: se não há armor (currentArmor <= 0), não absorvemos nada
        // <= significa "menor ou igual a" - se for 0 ou negativo, não tem armor
        // return faz a função parar aqui e devolver o valor (todo o dano passa)
        if (currentArmor <= 0f) {
            return incomingDamage;
        }

        // Mathf.Min() retorna o MENOR valor entre dois números
        // Se currentArmor = 30 e incomingDamage = 50, absorbedDamage = 30 (o armor só pode absorver até onde ele tem)
        // Se currentArmor = 100 e incomingDamage = 20, absorbedDamage = 20 (absorve todo o dano)
        float absorbedDamage = Mathf.Min(currentArmor, incomingDamage);
        
        // Subtração: currentArmor = currentArmor - absorbedDamage
        // O operador -= é um atalho para essa subtração
        // Remove do armor a quantidade que foi absorvida
        currentArmor -= absorbedDamage;
        
        // Calcula quanto dano sobrou (overflow) que o armor não conseguiu absorver
        // Se absorveu tudo, remainingDamage = 0
        // Se tinha armor = 30 e dano = 50, remainingDamage = 20 (vai para a vida)
        float remainingDamage = incomingDamage - absorbedDamage;

        // O operador ?. é "null-conditional": só invoca se OnArmorChanged não for null
        // Invoke() dispara o evento, notificando todos os inscritos
        // Passamos a fração do armor (0.0 a 1.0) dividindo atual por máximo
        OnArmorChanged?.Invoke(currentArmor / maxArmor);

        // Se o armor chegou a zero ou menos após absorver o dano
        if (currentArmor <= 0f) {
            // Garante que não fique negativo (clamp em 0)
            currentArmor = 0f;
            // Dispara o evento de armor depletado (para feedback visual/sonoro)
            OnArmorDepleted?.Invoke();
            Debug.Log("[PlayerArmor] Armor depleted!");
        }

        // Retorna o dano que não foi absorvido (vai para o PlayerHealth processar)
        return remainingDamage;
    }

    /// <summary>
    /// Adds armor points without exceeding maxArmor.
    /// Can be called when purchasing armor from the shop.
    /// </summary>
    public void AddArmor(float amount) {
        // Mathf.Min garante que o armor não ultrapasse o máximo permitido
        // Somamos amount ao armor atual, mas limitamos ao maxArmor
        // Exemplo: se currentArmor = 80, amount = 50, maxArmor = 100, resultado = 100 (não 130)
        currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
        
        // Notifica a UI que o armor mudou
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
        
        // $"..." é string interpolation - permite inserir variáveis dentro do texto
        // {currentArmor} é substituído pelo valor da variável
        Debug.Log($"[PlayerArmor] Armor added. Current: {currentArmor}/{maxArmor}");
    }

    /// <summary>
    /// Repairs armor by the specified amount, without exceeding maxArmor.
    /// </summary>
    public void RepairArmor(float amount) {
        // Funciona igual ao AddArmor - repara o armor danificado
        // Mathf.Min impede que ultrapasse o máximo
        currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
        OnArmorChanged?.Invoke(currentArmor / maxArmor);
    }

    /// <summary>
    /// Returns the current armor as a fraction between 0 and 1.
    /// </summary>
    // => é "expression body" - sintaxe curta para métodos simples que retornam uma única expressão
    // É equivalente a: public float GetArmorFraction() { return currentArmor / maxArmor; }
    // Divide o armor atual pelo máximo para obter uma fração (ex: 50/100 = 0.5)
    public float GetArmorFraction() => currentArmor / maxArmor;

    /// <summary>
    /// Returns the current armor value.
    /// </summary>
    // Getter simples - retorna o valor atual do armor
    public float GetCurrentArmor() => currentArmor;

    /// <summary>
    /// Returns the maximum armor value.
    /// </summary>
    // Getter simples - retorna o valor máximo do armor
    public float GetMaxArmor() => maxArmor;

    /// <summary>
    /// Checks if the player currently has any armor.
    /// </summary>
    // Retorna true se currentArmor for maior que 0, false caso contrário
    // O operador > (maior que) retorna um boolean (true/false)
    public bool HasArmor() => currentArmor > 0f;
}
