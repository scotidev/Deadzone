using UnityEngine;

/// <summary>
/// Manages the main menu UI and navigation.
/// </summary>
public class MenuManager : MonoBehaviour {
    [Header("UI Panels")]
    [SerializeField] private OptionsUI optionsUI;
    [SerializeField] private ControlsUI controlsUI;
    [SerializeField] private CreditsUI creditsUI;

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