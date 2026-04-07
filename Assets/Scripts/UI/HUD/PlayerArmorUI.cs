using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player armor bar UI. Subscribes to PlayerArmor events and updates
/// the blue-gray bar fill amount in real-time when the player takes damage or adds armor.
/// Works similarly to PlayerHealthUI but displays armor instead of health.
/// The armor bar is displayed above the health bar.
/// </summary>
public class PlayerArmorUI : MonoBehaviour {

    [Header("Armor References")]
    [Tooltip("Reference to the PlayerArmor script on the player GameObject.")]
    // Referência para o script PlayerArmor - precisamos dela para nos inscrever nos eventos
    [SerializeField] private PlayerArmor playerArmor;

    [Tooltip("The Image component that represents current armor (blue-gray bar).")]
    // Image é um componente do Unity UI que pode exibir sprites e cores
    // Esta é a barra que vai diminuir quando tomar dano (equivalente ao greenBar da vida)
    [SerializeField] private Image armorBar;

    [Tooltip("The Image component that represents missing armor (darker background).")]
    // Background (fundo) que mostra a parte do armor perdido (equivalente ao redBar da vida)
    [SerializeField] private Image armorBackground;

    [Tooltip("Optional decorative armor/shield icon image.")]
    // Ícone decorativo de escudo - opcional, pode ser null
    [SerializeField] private Image shieldIcon;

    [Header("Animation Settings")]
    [Tooltip("Speed of armor bar fill animation when using smooth transitions.")]
    // Velocidade da animação de lerp (transição suave)
    // Quanto maior o número, mais rápida a animação
    [SerializeField] private float lerpSpeed = 5f;

    [Tooltip("Enable smooth lerp transitions instead of instant updates.")]
    // Boolean (true/false) que ativa ou desativa a animação suave
    // Se false, a barra muda instantaneamente
    [SerializeField] private bool useSmoothTransition = true;

    // O valor alvo (target) para o qual a barra está animando
    // Usado pelo Lerp para saber onde chegar
    private float targetFillAmount;

    // Flag (bandeira) booleana que rastreia se o componente já foi inicializado
    // Evita inicializar duas vezes
    private bool hasInitialized;

    // Awake() é chamado antes do Start(), ideal para validações e inscrições em eventos
    private void Awake() {
        // Validação: checa se playerArmor foi atribuído no Inspector
        // == null significa "é nulo/vazio/não atribuído"
        if (playerArmor == null) {
            // Debug.LogError exibe uma mensagem de erro no Console do Unity (em vermelho)
            Debug.LogError("[PlayerArmorUI] PlayerArmor reference is missing! Assign it in the Inspector.");
            // enabled = false desliga este componente, evitando erros no Update()
            enabled = false;
            // return para a execução aqui, não continua o resto do Awake()
            return;
        }

        // Mesma validação para a barra de armor
        if (armorBar == null) {
            Debug.LogError("[PlayerArmorUI] ArmorBar Image reference is missing! Assign it in the Inspector.");
            enabled = false;
            return;
        }

        // Mesma validação para o background do armor
        if (armorBackground == null) {
            Debug.LogError("[PlayerArmorUI] ArmorBackground Image reference is missing! Assign it in the Inspector.");
            enabled = false;
            return;
        }

        // += é o operador de inscrição em eventos
        // Dizemos: "quando OnArmorChanged disparar, chame meu método OnArmorChanged()"
        // É como assinar uma newsletter - quando algo acontecer, você será notificado
        playerArmor.OnArmorChanged += OnArmorChanged;
        playerArmor.OnArmorDepleted += OnArmorDepleted;

        // fillAmount controla quanto da imagem é exibida (0.0 = vazio, 1.0 = cheio)
        // Inicializa com a barra cheia (100%)
        armorBar.fillAmount = 1f;
        armorBackground.fillAmount = 1f;
        targetFillAmount = 1f;
    }

    // Start() é chamado após o Awake(), no primeiro frame antes do Update()
    // Aqui sincronizamos com o valor inicial do PlayerArmor
    private void Start() {
        // ! é o operador NOT (negação) - se hasInitialized for false, entra no if
        // Isso garante que só inicializa uma vez
        if (!hasInitialized) {
            // Pega a fração atual do armor (0.0 a 1.0)
            float initialArmor = playerArmor.GetArmorFraction();
            // Define instantaneamente a barra com esse valor
            SetArmorInstant(initialArmor);
            // Marca como inicializado
            hasInitialized = true;
        }
    }

    // Update() é chamado a cada frame (normalmente 60 vezes por segundo)
    // Seguindo as boas práticas: Update só chama funções, a lógica fica dentro delas
    private void Update() {
        // Se smooth transition está desativado, não fazemos nada (return sai do método)
        if (!useSmoothTransition) return;

        // Chama a função que atualiza a barra suavemente
        UpdateArmorFill();
    }

    // OnDestroy() é chamado quando o objeto é destruído (ex: ao trocar de cena)
    // É importante remover event listeners para evitar memory leaks (vazamento de memória)
    private void OnDestroy() {
        // Checa se playerArmor ainda existe antes de tentar desinscrever
        if (playerArmor != null) {
            // -= é o operador de desinscrição de eventos
            // Remove nossos métodos da lista de callbacks do evento
            playerArmor.OnArmorChanged -= OnArmorChanged;
            playerArmor.OnArmorDepleted -= OnArmorDepleted;
        }
    }

    /// <summary>
    /// Called whenever PlayerArmor.OnArmorChanged is invoked.
    /// This method receives the armor fraction (0.0 to 1.0) and updates the bar accordingly.
    /// If smooth transitions are enabled, it sets the target and lets Update lerp to it.
    /// If smooth transitions are disabled, it updates the bar instantly.
    /// </summary>
    // Este método é chamado automaticamente quando o evento OnArmorChanged dispara
    // O parâmetro armorFraction recebe o valor que o evento passou (0.0 a 1.0)
    private void OnArmorChanged(float armorFraction) {
        // Se smooth transition está ativo
        if (useSmoothTransition) {
            // Define o valor alvo (target) para onde a barra deve ir
            // O Update() fará o lerp até chegar lá
            targetFillAmount = armorFraction;

            // Mathf.Abs() retorna o valor absoluto (sempre positivo)
            // Se a diferença entre atual e target for > 0.1 (10%), atualiza instantaneamente
            // Isso evita animações lentas quando há mudanças bruscas
            if (Mathf.Abs(armorBar.fillAmount - targetFillAmount) > 0.1f) {
                armorBar.fillAmount = armorFraction;
            }
        }
        else {
            // Atualização instantânea - sem animação
            // Muda o fillAmount diretamente
            armorBar.fillAmount = armorFraction;
        }
    }

    /// <summary>
    /// Smoothly updates the armor bar fill amount toward the target value.
    /// This method is called every frame only when useSmoothTransition is enabled.
    /// It uses Mathf.Lerp to create a smooth animation between the current and target values.
    /// </summary>
    // Função chamada pelo Update() para animar a barra suavemente
    private void UpdateArmorFill() {
        // Pega o valor atual do fillAmount da barra
        float currentFill = armorBar.fillAmount;
        
        // Mathf.Lerp = Linear Interpolation (interpolação linear)
        // Calcula um valor entre currentFill e targetFillAmount
        // Time.deltaTime = tempo desde o último frame (ex: ~0.016s a 60fps)
        // lerpSpeed controla a velocidade (quanto maior, mais rápido)
        // Resultado: a barra se move gradualmente do valor atual para o target
        float newFill = Mathf.Lerp(currentFill, targetFillAmount, Time.deltaTime * lerpSpeed);
        
        // Aplica o novo valor calculado à barra
        armorBar.fillAmount = newFill;
    }

    /// <summary>
    /// Sets the armor bar to a specific fill amount instantly, bypassing any lerping.
    /// Useful for initialization at startup or when you need immediate visual feedback.
    /// </summary>
    public void SetArmorInstant(float armorFraction) {
        // Mathf.Clamp01 limita o valor entre 0 e 1
        // Se passar 1.5, vira 1.0. Se passar -0.2, vira 0.0
        // Garante que fillAmount nunca saia do range válido
        float clampedFraction = Mathf.Clamp01(armorFraction);

        // Define instantaneamente o fillAmount (sem animação)
        armorBar.fillAmount = clampedFraction;
        // Também atualiza o target para evitar lerp indesejado depois
        targetFillAmount = clampedFraction;

        // O background sempre fica cheio (mostra a área total possível)
        armorBackground.fillAmount = 1f;
    }

    /// <summary>
    /// Called when the armor is completely depleted. Can be extended to add visual effects
    /// like fading, color changes, or special animations.
    /// </summary>
    // Callback chamado quando o armor chega a zero
    private void OnArmorDepleted() {
        // Log para debug - pode adicionar efeitos visuais aqui depois
        Debug.Log("[PlayerArmorUI] Armor bar depleted - visual feedback could go here");
        
        // Exemplo de feedback visual: mudar a cor do ícone quando o armor acabar
        // != null significa "não é nulo" - só executa se o ícone existe
        if (shieldIcon != null) {
            // new Color(r, g, b, a) - valores de 0.0 a 1.0
            // 0.5 = 50% - meio cinza, meio transparente
            Color depletedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            shieldIcon.color = depletedColor;
        }
    }

    /// <summary>
    /// Public method to get the current armor fill amount (0.0 to 1.0).
    /// Useful for other systems that need to know the armor bar state.
    /// </summary>
    // Método público que outros scripts podem chamar para saber o fillAmount atual da barra
    public float GetCurrentFillAmount() {
        return armorBar.fillAmount;
    }

    /// <summary>
    /// Public method to get the target fill amount (0.0 to 1.0).
    /// Useful for debugging or checking where the armor bar is animating toward.
    /// </summary>
    // Método público que retorna o valor target (útil para debug)
    public float GetTargetFillAmount() {
        return targetFillAmount;
    }
}
