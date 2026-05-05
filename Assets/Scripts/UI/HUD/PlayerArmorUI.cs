using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

/*============================================================================
    PlayerArmorUI.cs - Script da Barra de Armadura no HUD
    
    Este script.controla a barra visual de armadura no canto da tela.
    Ele "ouve" os eventos do PlayerArmor para saber quando atualizar.
    
    COMPONENTES DO UNITY USADOS:
    - Image: componente para mostrar imagens em Unity
    - fillAmount: propriedade que determina quanto da barra está preenchida
      (0 = vazio, 1 = cheio)
    
    FLUXO DE TRABALHO:
    1. Quando PlayerArmor muda (recebe dano ou repara), dispara OnArmorChanged
    2. Esse script recebe o evento e atualiza o fillAmount da barra
    3. Também mostra/esconde a UI dependendo se tem armadura ou não
============================================================================*/

/// <summary>
/// Manages the player armor bar UI. Subscribes to PlayerArmor events and updates
/// the blue-gray bar fill amount in real-time when the player takes damage or adds armor.
/// Works similarly to PlayerHealthUI but displays armor instead of health.
/// </summary>
public class PlayerArmorUI : MonoBehaviour {

    #region SERIALIZED FIELDS
    /*-----------------------------------------------------------------------------
        REFERÊNCIAS DO INSPECTOR - Arraste os objetos here:
        - PlayerArmor: o script que gerencia a armadura
        - ArmorBar: a imagem azul que representa a armadura
        - ArmorBackground: a imagem de fundo (escura)
        - ShieldIcon: o ícone de conteú
    -----------------------------------------------------------------------------*/

    [Header("Armor References")]
    [SerializeField] private PlayerArmor playerArmor;
    [SerializeField] private Image armorBar;          // Barra azul (preenchida com dano)
    [SerializeField] private Image armorBackground; // Fundo escuro
    [SerializeField] private Image shieldIcon;     // Ícone do escudo

    [Header("Animation Settings")]
    [SerializeField] private float lerpSpeed = 5f;           // Velocidade da animação
    [SerializeField] private bool useSmoothTransition = true; // Usar interpolação

    #endregion

    #region FIELDS

    // targetFillAmount é para onde a barra está se movendo (para animação suave)
    private float targetFillAmount;
    
    // hasInitialized garante que só inicializamos uma vez
    private bool hasInitialized;

    #endregion

    #region UNITY

    /*-----------------------------------------------------------------------------
        Awake() é chamado quando o objeto UI é criado.
        Aqui "assinamos" os eventos que queremos ouvir.
        
        ASSINAR EVENTOS:
        - É como dizer "me liga quando X acontecer"
        - Quando PlayerArmor.OnArmorChanged disparar, chamamos OnArmorChanged()
    -----------------------------------------------------------------------------*/
    private void Awake() {
        // OUVIR EVENTOS DO PLAYERARMOR
        playerArmor.OnArmorChanged += OnArmorChanged;
        playerArmor.OnArmorDepleted += OnArmorDepleted;
        
        // Também ouvimos o evento estático da Vest (quando é destruída)
        Vest.OnVestDestroyed += OnVestDestroyed;

        // Inicializar valores da barra
        armorBar.fillAmount = 1f;
        armorBackground.fillAmount = 1f;
        targetFillAmount = 1f;
    }

    /*-----------------------------------------------------------------------------
        Start() é chamado uma vez no primeiro frame.
        Aqui inicializamos o estado visual inicial.
    -----------------------------------------------------------------------------*/
    private void Start() {
        if (!hasInitialized) {
            // Pega a armadura inicial do PlayerArmor
            float initialArmor = playerArmor.GetArmorFraction();
            
            // Atualiza a barra para o valor inicial
            SetArmorInstant(initialArmor);
            
            // Se armadura começar em 0, esconder a UI
            // Se começar > 0, mostrar a UI
            if (initialArmor <= 0f) {
                HideArmorUI();
            } else {
                ShowArmorUI();
            }
            
            hasInitialized = true;
        }
    }

    /*-----------------------------------------------------------------------------
        Update() é chamado todo frame.
        Usamos para animada barra suavemente.
    -----------------------------------------------------------------------------*/
    private void Update() {
        if (!useSmoothTransition) return;

        UpdateArmorFill();
    }

    /*-----------------------------------------------------------------------------
        OnDestroy() é chamado quando o objeto é destruído.
        IMPORTANTE: Sempre removemos os eventos aqui para evitar erros!
    -----------------------------------------------------------------------------*/
    private void OnDestroy() {
        if (playerArmor != null) {
            playerArmor.OnArmorChanged -= OnArmorChanged;
            playerArmor.OnArmorDepleted -= OnArmorDepleted;
        }
        
        Vest.OnVestDestroyed -= OnVestDestroyed;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Called whenever PlayerArmor.OnArmorChanged is invoked.
    /// This method receives the armor fraction (0.0 to 1.0) and updates the bar accordingly.
    /// 
    /// Lógica especial: Se armadura vai de 0 para > 0, mostrar a UI automaticamente.
    /// Isso acontece quando o jogador compra/desbloqueia o colete na loja.
    /// </summary>
    private void OnArmorChanged(float armorFraction) {
        // CONCEITO: Se o armor muda de 0 para > 0, mostrar a UI automaticamente
        // Isso acontece quando desbloqueia/compra o colete na loja
        if (armorFraction > 0f && !gameObject.activeSelf) {
            ShowArmorUI();
        }
        
        if (useSmoothTransition) {
            targetFillAmount = armorFraction;

            // Se a diferença for grande, atualiza imediatamente para evitar delay visual
            if (Mathf.Abs(armorBar.fillAmount - targetFillAmount) > 0.1f) {
                armorBar.fillAmount = armorFraction;
            }
        } else {
            armorBar.fillAmount = armorFraction;
        }
    }

    /// <summary>
    /// Smoothly updates the armor bar fill amount toward the target value.
    /// This method is called every frame only when useSmoothTransition is enabled.
    /// It uses Mathf.Lerp to create a smooth animation between the current and target values.
    /// 
    /// Mathf.Lerp(a, b, t) calcula um valor entre a e b:
    /// - Se t = 0, retorna a
    /// - Se t = 0.5, retorna (a+b)/2
    /// - Se t = 1, retorna b
    /// 
    /// Multiplicamos por Time.deltaTime para que a animação seja suave independentemente do framerate.
    /// </summary>
    private void UpdateArmorFill() {
        float currentFill = armorBar.fillAmount;

        // Mathf.Lerp membuat transisi halus antar nilaiFill
        float newFill = Mathf.Lerp(currentFill, targetFillAmount, Time.deltaTime * lerpSpeed);

        armorBar.fillAmount = newFill;
    }

    /// <summary>
    /// Sets the armor bar to a specific fill amount instantly, bypassing any lerping.
    /// Useful for initialization at startup or when you need immediate visual feedback.
    /// </summary>
    public void SetArmorInstant(float armorFraction) {
        // Mathf.Clamp01 garante que o valor fica entre 0 e 1
        float clampedFraction = Mathf.Clamp01(armorFraction);

        armorBar.fillAmount = clampedFraction;
        targetFillAmount = clampedFraction;

        armorBackground.fillAmount = 1f;
    }

    /// <summary>
    /// Called when the armor is completely depleted. Can be extended to add visual effects
    /// like fading, color changes, or special animations.
    /// </summary>
    private void OnArmorDepleted() {
        // Escurece o ícone do escudo para indicar que quebrou
        if (shieldIcon != null) {
            Color depletedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            shieldIcon.color = depletedColor;
        }
        
        // Esconde a UI quando a armadura é destruída
        HideArmorUI();
    }

    /// <summary>
    /// Called when the vest is destroyed (armor reaches 0).
    /// Hides the armor UI from the HUD.
    /// </summary>
    private void OnVestDestroyed() {
        HideArmorUI();
    }

    /// <summary>
    /// Shows the armor UI in the HUD.
    /// Used when the player buys/repairs the vest.
    /// Also restores the shield icon color.
    /// </summary>
    public void ShowArmorUI() {
        // Ativa o GameObject (torna visível)
        gameObject.SetActive(true);
        
        // CONCEITO: Restaurar a cor do ícone quando volta a ter armadura
        // O ícone tinha ficado escuro quando quebrou, agora volta ao normal
        if (shieldIcon != null) {
            shieldIcon.color = Color.white;
        }
    }

    /// <summary>
    /// Hides the armor UI from the HUD.
    /// Used when the vest is destroyed.
    /// </summary>
    public void HideArmorUI() {
        // Desativa o GameObject (torna invisível)
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Public method to get the current armor fill amount (0.0 to 1.0).
    /// Useful for other systems that need to know the armor bar state.
    /// </summary>
    public float GetCurrentFillAmount() {
        return armorBar.fillAmount;
    }

    /// <summary>
    /// Public method to get the target fill amount (0.0 to 1.0).
    /// Useful for debugging or checking where the armor bar is animating toward.
    /// </summary>
    public float GetTargetFillAmount() {
        return targetFillAmount;
    }

    #endregion
}