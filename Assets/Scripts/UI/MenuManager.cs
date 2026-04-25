using UnityEngine;
using InfimaGames.LowPolyShooterPack; // Namespace necessário para acessar o IAudioManagerService.

/// <summary>
/// Manages the main menu UI and navigation.
/// </summary>
public class MenuManager : MonoBehaviour {

    [Header("UI Panels")]
    [SerializeField] private OptionsUI optionsUI;
    [SerializeField] private ControlsUI controlsUI;
    [SerializeField] private CreditsUI creditsUI;

    [Header("Audio")]
    // AudioClip que armazenará a música de fundo do menu.
    [SerializeField] private AudioClip menuBGM;

    /// <summary>
    /// O Start inicializa a música do menu assim que o objeto é ativado na cena.
    /// </summary>
    private void Start() {
        // Obtemos o serviço de áudio global.
        var audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        // Reproduz a música com fade-in de 1 segundo para suavizar o início.
        audioService?.PlayBGM(menuBGM, true, 1.0f);
    }

    /// <summary>
    /// Loads the game scene.
    /// </summary>
    public void OnNewGameClick() {
        SceneLoader.Instance.LoadGame();
    }

    /// <summary>
    /// Shows the options panel.
    /// </summary>
    public void OnOptionsClick() {
        optionsUI?.Show();
    }

    /// <summary>
    /// Shows the controls panel.
    /// </summary>
    public void OnControlsClick() {
        controlsUI?.Show();
    }

    /// <summary>
    /// Shows the credits panel.
    /// </summary>
    public void OnCreditsClick() {
        creditsUI?.Show();
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    public void OnExitClick() {
        Application.Quit();
    }
}