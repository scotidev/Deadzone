using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  UIManager é o coordenador central de todos os painéis de UI.
//  Ele age como um mediador: outros scripts (PauseManager, ShopManager)
//  chamam UIManager em vez de acessar diretamente cada painel.
//
//  Isso segue o padrão "Mediator": os sistemas de jogo não se
//  conhecem entre si — todos se comunicam através do UIManager.
//
//  MODIFICAÇÃO ADICIONADA:
//  Adicionamos o campo "waveUI" e os métodos ShowWaveHUD/HideWaveHUD.
//  WaveUI é diferente dos outros painéis: é um HUD persistente e
//  NÃO é ocultado por HideAllPanels (que oculta apenas overlays
//  temporários como Pause, Shop e Options).

/// <summary>
/// Coordenador central de UI. Gerencia todos os painéis do jogo.
/// Atua como mediador entre os sistemas de jogo e os componentes de UI.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ==============================================================
    //  SINGLETON
    // ==============================================================
    public static UIManager Instance { get; private set; }

    // ==============================================================
    //  REFERÊNCIAS AOS PAINÉIS — arrastar no Inspector
    // ==============================================================
    //  [SerializeField] expõe campos privados no Inspector.
    //  [Header("...")] cria um separador visual entre grupos de campos.
    //  Cada campo deve receber o GameObject/componente correspondente
    //  da hierarquia do Canvas na cena.

    [Header("Painéis de UI")]
    [SerializeField] private PauseUI pauseUI;              // Painel de pausa
    [SerializeField] private ShopUI shopUI;                // Painel da loja
    [SerializeField] private OptionsUI optionsUI;          // Painel de opções
    [SerializeField] private ControlsUI controlsUI;        // Painel de controles
    [SerializeField] private InteractionPromptUI interactionPromptUI; // Prompt de interação

    [Header("HUD Persistente")]
    [Tooltip("HUD de informações de onda. NÃO é ocultado por HideAllPanels.")]
    [SerializeField] private WaveUI waveUI; // HUD sempre visível durante gameplay

    private void Awake()
    {
        InitializeSingleton();
    }

    // ==============================================================
    //  INICIALIZAÇÃO DO SINGLETON
    // ==============================================================

    private void InitializeSingleton()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ==============================================================
    //  MÉTODOS DE EXIBIÇÃO DE PAINÉIS
    // ==============================================================
    //  Cada método oculta todos os painéis antes de mostrar o seu,
    //  garantindo que apenas um painel overlay esteja visível por vez.
    //  (WaveUI é exceção — não é ocultado, pois é HUD persistente.)

    /// <summary>
    /// Exibe o menu de pausa e oculta todos os outros painéis.
    /// </summary>
    public void ShowPauseMenu()
    {
        HideAllPanels();
        if (pauseUI != null)
            pauseUI.Show();
    }

    /// <summary>
    /// Exibe a loja e oculta todos os outros painéis.
    /// </summary>
    public void ShowShop()
    {
        HideAllPanels();
        if (shopUI != null)
            shopUI.Show();
    }

    /// <summary>
    /// Exibe o painel de opções (mantendo o painel de pausa oculto).
    /// </summary>
    public void ShowOptions()
    {
        if (pauseUI != null)
            pauseUI.Hide();

        if (optionsUI != null)
            optionsUI.Show();
    }

    /// <summary>
    /// Exibe o painel de controles (mantendo o painel de pausa oculto).
    /// </summary>
    public void ShowControls()
    {
        if (pauseUI != null)
            pauseUI.Hide();

        if (controlsUI != null)
            controlsUI.Show();
    }

    // ==============================================================
    //  OCULTAR TODOS OS PAINÉIS
    // ==============================================================
    //  IMPORTANTE: WaveUI NÃO é incluído aqui de propósito.
    //  WaveUI é um HUD persistente — deve ficar visível mesmo quando
    //  o jogador abre a loja ou pausa. Ocultar pauseUI, shopUI etc.
    //  não deve apagar as informações de onda da tela.

    /// <summary>
    /// Oculta todos os painéis de overlay (Pause, Shop, Options, Controls).
    /// WaveUI é intencionalmente excluído — é um HUD persistente.
    /// </summary>
    public void HideAllPanels()
    {
        if (pauseUI != null)     pauseUI.Hide();
        if (shopUI != null)      shopUI.Hide();
        if (optionsUI != null)   optionsUI.Hide();
        if (controlsUI != null)  controlsUI.Hide();
    }

    // ==============================================================
    //  CONTROLE DO WAVE HUD
    // ==============================================================
    //  Métodos separados para o WaveUI porque ele segue regras
    //  diferentes dos painéis: é persistente e não vai com HideAllPanels.

    /// <summary>
    /// Exibe o HUD de ondas.
    /// Normalmente já fica visível (WaveUI.Start() chama Show()),
    /// mas este método pode ser usado para reexibir se necessário.
    /// </summary>
    public void ShowWaveHUD()
    {
        if (waveUI != null)
            waveUI.Show();
    }

    /// <summary>
    /// Oculta o HUD de ondas.
    /// Use em telas que não devem mostrar informações de onda
    /// (ex: tela de game over, menu principal).
    /// </summary>
    public void HideWaveHUD()
    {
        if (waveUI != null)
            waveUI.Hide();
    }

    // ==============================================================
    //  PROMPT DE INTERAÇÃO
    // ==============================================================

    /// <summary>
    /// Exibe o prompt de interação com a mensagem fornecida.
    /// Chamado por PlayerInteraction ao detectar um Interactable.
    /// </summary>
    public void ShowInteractionPrompt(string message)
    {
        if (interactionPromptUI != null)
            interactionPromptUI.Show(message);
    }

    /// <summary>
    /// Oculta o prompt de interação.
    /// </summary>
    public void HideInteractionPrompt()
    {
        if (interactionPromptUI != null)
            interactionPromptUI.Hide();
    }

    /// <summary>
    /// Alterna visibilidade do prompt de interação.
    /// Método legado mantido para compatibilidade.
    /// </summary>
    public void ToggleInteractionPrompt(bool show, string message = "")
    {
        if (show)
            ShowInteractionPrompt(message);
        else
            HideInteractionPrompt();
    }
}
