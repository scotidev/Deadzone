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

    #endregion

    #region UNITY

    private void Start() {
        var audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        audioService?.PlayBGM(menuBGM, true, 1.0f);
    }

    #endregion

    #region METHODS

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

    #endregion
}