using TMPro;
using UnityEngine;

/// <summary>
/// UI component that displays the player's current currency in the HUD.
/// Subscribes to EconomyManager events to update in real-time.
/// </summary>
public class CurrencyUI : MonoBehaviour {

    [Header("UI References")]
    [Tooltip("TextMeshPro component to display the currency amount.")]
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Display Format")]
    [Tooltip("Prefix to show before the currency amount (e.g., '$', 'Coins: ').")]
    [SerializeField] private string prefix = "$";

    [Tooltip("If true, formats large numbers with commas (e.g., 1,000).")]
    [SerializeField] private bool useThousandsSeparator = true;

    [Header("Animation (Optional)")]
    [Tooltip("If true, currency text will briefly scale up when it changes.")]
    [SerializeField] private bool animateOnChange = true;

    [Tooltip("Scale multiplier for the animation.")]
    [SerializeField] private float animationScale = 1.2f;

    [Tooltip("Duration of the scale animation in seconds.")]
    [SerializeField] private float animationDuration = 0.2f;

    /// <summary>
    /// Original scale of the text, stored to reset after animation.
    /// </summary>
    private Vector3 originalScale;

    /// <summary>
    /// Current animation timer.
    /// </summary>
    private float animationTimer = 0f;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Validates references.
    /// </summary>
    private void Awake() {
        // Validate that we have a TextMeshProUGUI component assigned
        if (currencyText == null) {
            Debug.LogError("[CurrencyUI] currencyText is not assigned! Please assign it in the Inspector.", this);
            enabled = false; // Disable this component to prevent errors
            return;
        }

        // Store the original scale for animation reset
        originalScale = currencyText.transform.localScale;
    }

    /// <summary>
    /// Start is called before the first frame update.
    /// Connects to EconomyManager - guaranteed to run AFTER all Awake() calls.
    /// </summary>
    /// CONCEITO PEDAGÓGICO: Start() vs Awake() vs OnEnable()
    /// 
    /// ORDEM DE EXECUÇÃO:
    /// 1. Awake() - Todos os objetos (ordem indefinida entre GameObjects)
    /// 2. OnEnable() - Logo após Awake (pode rodar antes de outros Awake!)
    /// 3. Start() - DEPOIS que TODOS os Awake terminaram (ordem indefinida, mas sempre após Awake)
    /// 
    /// QUANDO USAR CADA UM:
    /// - Awake(): Inicializar o próprio objeto (Singletons, referências internas)
    /// - Start(): Conectar com outros objetos (garantia que outros Awake já rodaram)
    /// - OnEnable(): Reativar subscriptions quando objeto é ativado/desativado
    /// 
    /// NESTE CASO: Usamos Start() porque precisamos que EconomyManager.Awake() já tenha rodado
    private void Start() {
        // Conecta ao EconomyManager (que já foi inicializado no Awake)
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged += UpdateCurrencyDisplay;
            
            Debug.Log("[CurrencyUI] ✓ Inscrito no evento OnCurrencyChanged!");
            
            // Inicializa o display com a moeda atual
            UpdateCurrencyDisplay(EconomyManager.Instance.GetCurrentCurrency());
        }
        else {
            Debug.LogError("[CurrencyUI] EconomyManager.Instance é NULL no Start()! Verifique se SystemManagers está na cena.");
        }
    }

    /// <summary>
    /// Called when the object is destroyed.
    /// Unsubscribes from EconomyManager events to prevent memory leaks.
    /// </summary>
    /// CONCEITO: Cleanup de Eventos
    /// -= remove nossa função da lista de ouvintes
    /// Se não fizermos isso, o EconomyManager vai tentar chamar um objeto que não existe mais = CRASH
    /// 
    /// NOTA: Usamos OnDestroy() em vez de OnDisable() porque o HUD não é desativado durante o jogo
    /// Se o objeto pudesse ser ativado/desativado, usaríamos OnEnable/OnDisable
    private void OnDestroy() {
        // DESINSCRIÇÃO: Sempre faça cleanup de eventos para evitar memory leaks
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged -= UpdateCurrencyDisplay;
        }
    }

    /// <summary>
    /// Updates the currency display text with the new amount.
    /// Called automatically when currency changes.
    /// </summary>
    /// <param name="newAmount">The new currency amount.</param>
    private void UpdateCurrencyDisplay(int newAmount) {
        // DEBUG: Log para verificar se o método está sendo chamado
        Debug.Log($"[CurrencyUI] UpdateCurrencyDisplay chamado com {newAmount} moedas. currencyText null? {currencyText == null}");
        
        // If currencyText is null, exit early
        if (currencyText == null) {
            Debug.LogError("[CurrencyUI] currencyText é NULL! Não pode atualizar display.");
            return;
        }

        // Format the currency string
        // useThousandsSeparator adds commas: 1000 → "1,000"
        string formattedAmount = useThousandsSeparator
            ? newAmount.ToString("N0") // N0 = number format with 0 decimal places
            : newAmount.ToString();

        // Set the text with prefix
        // Example: "$1,000" or "Coins: 1000"
        currencyText.text = $"{prefix}{formattedAmount}";
        
        Debug.Log($"[CurrencyUI] Texto atualizado para: {currencyText.text}");

        // Trigger animation if enabled
        if (animateOnChange) {
            TriggerAnimation();
        }
    }

    /// <summary>
    /// Triggers a brief scale-up animation on the currency text.
    /// Gives visual feedback when currency changes.
    /// </summary>
    private void TriggerAnimation() {
        // Reset animation timer to start a new animation
        animationTimer = animationDuration;
    }

    /// <summary>
    /// Update is called once per frame.
    /// Handles the scale animation.
    /// </summary>
    private void Update() {
        // If animation is not active, skip
        if (animationTimer <= 0f || currencyText == null) return;

        // Decrease timer
        // Time.deltaTime is the time since last frame (makes animation frame-rate independent)
        animationTimer -= Time.deltaTime;

        // Calculate animation progress (1.0 at start, 0.0 at end)
        float progress = animationTimer / animationDuration;

        // Calculate current scale using lerp (linear interpolation)
        // At progress=1 (start), scale is animationScale (e.g., 1.2x)
        // At progress=0 (end), scale is 1.0 (original size)
        float currentScale = Mathf.Lerp(1f, animationScale, progress);

        // Apply scale
        currencyText.transform.localScale = originalScale * currentScale;

        // When animation finishes, ensure scale is reset to original
        if (animationTimer <= 0f) {
            currencyText.transform.localScale = originalScale;
        }
    }

    /// <summary>
    /// Called when the script is loaded or a value changes in the Inspector.
    /// Attempts to find missing references automatically.
    /// </summary>
    private void OnValidate() {
        // If currencyText is not assigned, try to find it on this GameObject
        if (currencyText == null) {
            currencyText = GetComponent<TextMeshProUGUI>();
        }
    }
}
