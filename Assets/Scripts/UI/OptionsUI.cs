using UnityEngine;
using UnityEngine.UI;

// refatoração: como o jogo será jogado no Itch.io, não é preciso salvar as configurações no navegador nem localmente, mas  uma vez dado play, as configurações podem ser salvas usando PlayerPrefs, que é uma solução simples e eficaz para armazenar configurações de jogo. salvando somente as configurações durante a sessão de jogo, não há necessidade de lidar com a complexidade de armazenamento persistente, o que simplifica o código e melhora a experiência do usuário.

// Refatoraçaõ: de fato como implementar os sistemas de sensibilidade do mouse e volume ? precisamos analisar que o projeto lida com IAudioManagerService com ServiceLocator, então precisamos analisar como inteegrar tudo.

/// <summary>
/// Manages the options menu UI including mouse sensitivity and volume settings.
/// </summary>
public class OptionsUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Settings Controls")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button backButton;

    #endregion

    #region FIELDS

    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const string VOLUME_KEY = "MasterVolume";

    #endregion

    #region PROPERTIES

    /// <summary>
    /// Enables Escape-close behavior for this panel.
    /// </summary>
    protected override bool CloseOnEscape => true;

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();
        BindControls();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Binds all UI controls to their respective event handlers.
    /// </summary>
    private void BindControls() {
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClick);
    }

    /// <summary>
    /// Shows the options panel and loads saved settings.
    /// </summary>
    public override void Show() {
        base.Show();
        LoadSettings();
    }

    /// <summary>
    /// Handles mouse sensitivity slider value changes.
    /// </summary>
    /// <param name="value">The new sensitivity value.</param>
    private void OnMouseSensitivityChanged(float value) {
        SaveSetting(SENSITIVITY_KEY, value);
        // TODO: Apply the new sensitivity value to the game's input system, e.g., via a service or event or whatever is appropriate for the architecture.
    }

    /// <summary>
    /// Handles volume slider value changes.
    /// </summary>
    /// <param name="value">The new volume value.</param>
    private void OnVolumeChanged(float value) {
        SaveSetting(VOLUME_KEY, value);
        AudioListener.volume = value;

        //TODO: devemos também notificar o IAudioManagerService sobre a mudança de volume, para que ele possa aplicar a nova configuração em todos os sistemas de áudio do jogo?
    }

    /// <summary>
    /// Handles the Back button click event.
    /// </summary>
    private void OnBackClick() {
        Hide();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPauseMenu();
    }

    /// <summary>
    /// Handles Escape key behavior by reusing the Back action.
    /// </summary>
    protected override void OnEscapePressed() {
        OnBackClick();
    }

    /// <summary>
    /// Saves a setting value to PlayerPrefs.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to save.</param>
    private void SaveSetting(string key, float value) {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads all settings from PlayerPrefs and updates UI controls.
    /// </summary>
    private void LoadSettings() {
        if (mouseSensitivitySlider != null) {
            float sensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 1.0f);
            mouseSensitivitySlider.value = sensitivity;
        }

        if (volumeSlider != null) {
            float volume = PlayerPrefs.GetFloat(VOLUME_KEY, 1.0f);
            volumeSlider.value = volume;
        }
    }

    #endregion
}
