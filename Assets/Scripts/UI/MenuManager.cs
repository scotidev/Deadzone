using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Manages the main menu UI and navigation.
/// </summary>
public class MenuManager : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("UI Panels")]
    [SerializeField] private OptionsUI optionsUI;
    [SerializeField] private ControlsUI controlsUI;
    [SerializeField] private CreditsUI creditsUI;

    [Header("Audio")]
    [SerializeField] private AudioClip menuBGM;
    [Range(0f, 1f)]
    [SerializeField] private float menuBGMVolume = 1f;

    [Header("Select Map")]
    [SerializeField] private string selectMapSceneName = "SelectMap";

    #endregion

    #region UNITY

    private void Start() {
        // Avisa o GameManager que estamos no Menu Principal.
        GameManager.Instance?.SetState(GameState.MainMenu);

        var audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        audioService?.PlayBGM(menuBGM, true, 1.0f, menuBGMVolume);
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Loads the map selection scene.
    /// Resets session data to ensure a clean state for the new game.
    /// </summary>
    public void OnNewGameClick() {
        GameManager.ResetGameSession();

        // Carrega a SelectMap sem loading screen (transição rápida entre menus).
        SceneLoader.Instance?.LoadSceneImmediate(selectMapSceneName);
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
    /// Exits the application or exits fullscreen in WebGL builds.
    /// Application.Quit() is a no-op in WebGL, so we also exit fullscreen
    /// which, on itch.io, returns the player to the embed page.
    /// </summary>
    public void OnExitClick() {
        Application.Quit();
        Screen.fullScreen = false;
    }

    #endregion
}