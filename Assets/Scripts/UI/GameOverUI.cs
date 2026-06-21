using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Game Over screen, displaying the wave the player reached
/// and providing buttons to retry (SelectMap) or quit to the main menu.
/// </summary>
public class GameOverUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Game Over Info")]
    [SerializeField] private TMP_Text waveNumberText;

    [Header("Game Over Buttons")]
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private Button quitButton;

    #endregion

    #region PROPERTIES

    protected override bool CloseOnEscape => false;

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();
        BindButtons();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Binds all button click events to their respective handlers.
    /// </summary>
    private void BindButtons() {
        if (tryAgainButton != null)
            tryAgainButton.onClick.AddListener(OnTryAgainClick);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClick);
    }

    /// <summary>
    /// Updates the displayed wave number text.
    /// </summary>
    public void SetWaveNumber(int wave) {
        if (waveNumberText != null)
            waveNumberText.text = wave.ToString();
    }

    /// <summary>
    /// Handles the Try Again button click event.
    /// Resets time scale and loads the SelectMap scene.
    /// </summary>
    private void OnTryAgainClick() {
        GameManager.ResetGameSession();
        GameManager.Instance?.SetTimeScale(1f);

        SceneLoader.Instance.LoadSceneImmediate("SelectMap");
    }

    /// <summary>
    /// Handles the Quit button click event.
    /// Resets time scale, sets game state to MainMenu, and loads the Menu scene.
    /// </summary>
    private void OnQuitClick() {
        GameManager.ResetGameSession();
        GameManager.Instance?.SetTimeScale(1f);
        GameManager.Instance?.SetState(GameState.MainMenu);

        SceneLoader.Instance.LoadSceneImmediate("Menu");
    }

    #endregion
}
