using System;
using UnityEngine;

/// <summary>
/// Gerencia o sistema de moeda do jogador.
/// Singleton que rastreia o dinheiro, lida com transações e notifica ouvintes sobre mudanças.
/// 
/// CONCEITOS APRENDIDOS:
/// - Padrão Singleton: Garante que existe apenas uma instância deste gerenciador no jogo
/// - Eventos (Events): Sistema de comunicação entre objetos sem acoplamento direto
/// - Encapsulamento: Variável currentCurrency é privada, só pode ser modificada por métodos controlados
/// </summary>
public class EconomyManager : MonoBehaviour {
    
    /// <summary>
    /// Ponto de acesso global à única instância do EconomyManager.
    /// Padrão Singleton: Permite que qualquer script acesse via EconomyManager.Instance
    /// </summary>
    public static EconomyManager Instance { get; private set; }

    [Header("Starting Currency")]
    [Tooltip("Quantidade de moedas que o jogador começa.")]
    [SerializeField] private int startingCurrency = 500;

    /// <summary>
    /// Quantidade atual de moedas que o jogador possui.
    /// Privado para forçar o uso dos métodos AddCurrency e SpendCurrency em todas as mudanças.
    /// PRINCIPIO: Encapsulamento - protege o dado e garante que mudanças sempre disparem eventos
    /// </summary>
    private int currentCurrency;

    /// <summary>
    /// Evento disparado sempre que a quantidade de moeda muda.
    /// Passa a nova quantidade de moedas para todos os inscritos (UI, etc).
    /// 
    /// COMO FUNCIONA: Quando a moeda muda, todos os objetos inscritos são notificados automaticamente
    /// Exemplo: CurrencyUI se inscreve e atualiza o display quando este evento dispara
    /// </summary>
    public event Action<int> OnCurrencyChanged;

    /// <summary>
    /// Evento disparado quando uma compra é bem-sucedida.
    /// Útil para tocar sons de sucesso ou mostrar feedback visual.
    /// </summary>
    public event Action<int> OnPurchaseSuccess;

    /// <summary>
    /// Evento disparado quando uma compra falha por dinheiro insuficiente.
    /// Útil para tocar sons de erro ou mostrar mensagens de aviso.
    /// Parâmetros: (custo, moedas atuais)
    /// </summary>
    public event Action<int, int> OnPurchaseFailed;

    /// <summary>
    /// Awake é chamado quando a instância do script está sendo carregada.
    /// Implementa o padrão Singleton para garantir que apenas um EconomyManager existe.
    /// 
    /// ORDEM DE EXECUÇÃO NO UNITY:
    /// 1. Awake() - Inicialização, antes de tudo
    /// 2. OnEnable() - Quando objeto é ativado
    /// 3. Start() - Antes do primeiro frame
    /// 4. Update() - A cada frame
    /// </summary>
    private void Awake() {
        // PADRÃO SINGLETON: Verifica se já existe uma instância
        if (Instance == null) {
            // Se não existe, esta se torna a instância
            Instance = this;
            // DontDestroyOnLoad mantém este objeto vivo entre mudanças de cena
            // Não necessário para jogo web mas boa prática
            DontDestroyOnLoad(gameObject);
        }
        else {
            // Se já existe uma instância, destrói esta duplicata
            // Isso evita múltiplos gerenciadores conflitantes
            Destroy(gameObject);
            return;
        }

        // Inicializa a moeda inicial do jogador
        InitializeCurrency();
    }

    /// <summary>
    /// Define a moeda inicial do jogador e notifica os ouvintes.
    /// Chamado durante Awake para configurar o estado inicial.
    /// </summary>
    private void InitializeCurrency() {
        // Define a moeda atual como a quantidade inicial
        currentCurrency = startingCurrency;
        
        // Notifica todos os ouvintes (UI) da quantidade inicial
        // O operador ?. garante que só invocamos se houver inscritos (evita NullReferenceException)
        OnCurrencyChanged?.Invoke(currentCurrency);
        
        Debug.Log($"[EconomyManager] Inicializado com {currentCurrency} moedas.");
    }

    /// <summary>
    /// Adiciona moedas ao total do jogador.
    /// Use para recompensas por matar inimigos, completar waves, etc.
    /// </summary>
    /// <param name="amount">The amount of currency to add. Must be positive.</param>
    public void AddCurrency(int amount) {
        // Validate that the amount is positive (can't add negative currency)
        // Mathf.Max returns the larger of two values, ensuring minimum of 0
        if (amount <= 0) {
            Debug.LogWarning($"[EconomyManager] Attempted to add non-positive currency: {amount}");
            return;
        }

        // Add the amount to current currency
        currentCurrency += amount;
        
        // Notify all listeners that the currency changed
        OnCurrencyChanged?.Invoke(currentCurrency);
        
        Debug.Log($"[EconomyManager] Added {amount} currency. New total: {currentCurrency}");
    }

    /// <summary>
    /// Attempts to spend currency for a purchase.
    /// Returns true if the purchase succeeded, false if insufficient funds.
    /// </summary>
    /// <param name="cost">The cost of the item/upgrade.</param>
    /// <returns>True if purchase succeeded, false if insufficient funds.</returns>
    public bool TrySpendCurrency(int cost) {
        // Validate cost is positive
        if (cost < 0) {
            Debug.LogWarning($"[EconomyManager] Attempted to spend negative currency: {cost}");
            return false;
        }

        // Allow free items (cost = 0) without checking funds
        if (cost == 0) {
            OnPurchaseSuccess?.Invoke(0);
            return true;
        }

        // Check if player has enough currency
        // If current is less than cost, they can't afford it
        if (currentCurrency < cost) {
            // Fire failure event with cost and current amount for UI feedback
            OnPurchaseFailed?.Invoke(cost, currentCurrency);
            Debug.Log($"[EconomyManager] Purchase failed. Cost: {cost}, Current: {currentCurrency}");
            return false;
        }

        // Player can afford it - deduct the cost
        currentCurrency -= cost;
        
        // Notify listeners of the change
        OnCurrencyChanged?.Invoke(currentCurrency);
        OnPurchaseSuccess?.Invoke(cost);
        
        Debug.Log($"[EconomyManager] Purchase successful. Spent {cost}, Remaining: {currentCurrency}");
        return true;
    }

    /// <summary>
    /// Checks if the player can afford a specific cost without making the purchase.
    /// Useful for UI to show if a button should be enabled/disabled.
    /// </summary>
    /// <param name="cost">The cost to check.</param>
    /// <returns>True if player has enough currency.</returns>
    public bool CanAfford(int cost) {
        // Simple comparison: current currency must be >= cost
        return currentCurrency >= cost;
    }

    /// <summary>
    /// Returns the player's current currency amount.
    /// Use this for displaying the currency in UI.
    /// </summary>
    public int GetCurrentCurrency() => currentCurrency;

    /// <summary>
    /// Resets the player's currency to the starting amount.
    /// Useful for restarting the game or testing.
    /// </summary>
    public void ResetCurrency() {
        currentCurrency = startingCurrency;
        OnCurrencyChanged?.Invoke(currentCurrency);
        Debug.Log($"[EconomyManager] Currency reset to {currentCurrency}");
    }

    /// <summary>
    /// Sets the currency to a specific value.
    /// Only use for debugging/testing purposes.
    /// </summary>
    /// <param name="amount">The exact amount to set.</param>
    public void SetCurrency(int amount) {
        // Ensure we don't set negative currency
        currentCurrency = Mathf.Max(0, amount);
        OnCurrencyChanged?.Invoke(currentCurrency);
        Debug.Log($"[EconomyManager] Currency set to {currentCurrency}");
    }
}
