using UnityEngine;

// refatoração: o UI  da wave deve ficar aqui? se parar pra analisar, o HUD do player vem do Element.cs e ElementText.cs, o que é um pouco inconsistente. Talvez seja melhor criar um HUDManager para lidar com os elementos do HUD, e deixar o UIManager apenas para os painéis de menu e interação. Assim, o UIManager fica mais focado em gerenciar as interfaces de usuário relacionadas a menus e interações, enquanto o HUDManager cuida dos elementos do HUD durante o jogo. ai pra isso precisariamos analisar como está feita o HUD do player todo, como as armas são mostradas, os icones, a vida, e unificar tudo em um HUD só, e o UIManager só cuida dos painéis de menu e interação. isso deixaria a arquitetura mais limpa e organizada, com responsabilidades bem definidas para cada manager.

//REFATORAÇÃO: dá pracolocar a lógica d einicialização do singleton no awake. A nao ser que, após analise, decidimos que  o UIManager na verdade deveria ser um serviço do Service locator, é preciso uma analise profunda

/// <summary>
/// Central coordinator for UI. Manages all game panels in the game.
/// Acts as a mediator between game systems and UI components.
/// </summary>
public class UIManager : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="UIManager"/> instance.</summary>
    public static UIManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("UI Panels")]
    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private OptionsUI optionsUI;
    [SerializeField] private ControlsUI controlsUI;
    [SerializeField] private InteractionPromptUI interactionPromptUI;

    [Header("HUD")]
    [SerializeField] private WaveUI waveUI;
    [SerializeField] private GameObject hudRoot;

    #endregion

    #region UNITY

    private void Awake() {
        InitializeSingleton();
    }

    #endregion

    #region METHODS

    private void InitializeSingleton() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Exhibits the pause menu and hides all other panels.
    /// </summary>
    public void ShowPauseMenu() {
        HideAllPanels();
        if (pauseUI != null)
            pauseUI.Show();
    }

    /// <summary>
    /// Exhibits the shop panel and hide all other panels.
    /// </summary>
    public void ShowShop() {
        HideAllPanels();
        if (hudRoot != null) hudRoot.SetActive(false);
        if (shopUI != null)
            shopUI.Show();
    }

    /// <summary>
    /// Exhibits the options panel (keeping the pause panel hidden)
    /// </summary>
    public void ShowOptions() {
        if (pauseUI != null)
            pauseUI.Hide();

        if (optionsUI != null)
            optionsUI.Show();
    }

    /// <summary>
    /// Exhibits the controls panel (keeping the pause panel hidden)
    /// </summary>
    public void ShowControls() {
        if (pauseUI != null)
            pauseUI.Hide();

        if (controlsUI != null)
            controlsUI.Show();
    }

    /// <summary>
    /// Hides all panels except WaveUI. 
    /// Hides all overlays.
    /// </summary>
    public void HideAllPanels() {
        if (pauseUI != null) pauseUI.Hide();
        if (shopUI != null) shopUI.Hide();
        if (optionsUI != null) optionsUI.Hide();
        if (controlsUI != null) controlsUI.Hide();
        if (hudRoot != null) hudRoot.SetActive(true);
    }

    /// <summary>
    /// Exhibits the wave HUD. 
    /// </summary>
    public void ShowWaveHUD() {
        if (waveUI != null)
            waveUI.Show();
    }

    /// <summary>
    /// Hides the wave HUD.
    /// </summary>
    public void HideWaveHUD() {
        if (waveUI != null)
            waveUI.Hide();
    }

    /// <summary>
    /// Exhibits the interaction prompt with the specified message.
    /// Called by PlayerInteraction when detecting an Interactable.
    /// </summary>
    public void ShowInteractionPrompt(string message) {
        if (interactionPromptUI != null)
            interactionPromptUI.Show(message);
    }

    /// <summary>
    /// Hides the Interaction Prompt.
    /// </summary>
    public void HideInteractionPrompt() {
        if (interactionPromptUI != null)
            interactionPromptUI.Hide();
    }

    /// <summary>
    /// Switches the interaction prompt visibility. 
    /// Legacy method kept for compatibility.
    /// </summary>
    public void ToggleInteractionPrompt(bool show, string message = "") {
        if (show)
            ShowInteractionPrompt(message);
        else
            HideInteractionPrompt();
    }

    #endregion
}
