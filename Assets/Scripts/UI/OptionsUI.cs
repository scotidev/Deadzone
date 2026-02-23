using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the options menu UI including mouse sensitivity and volume settings.
/// </summary>
public class OptionsUI : BaseUI
{
    [Header("Settings Controls")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button backButton;

    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const string VOLUME_KEY = "MasterVolume";

    protected override void Awake()
    {
        base.Awake();
        BindControls();
    }

    /// <summary>
    /// Binds all UI controls to their respective event handlers.
    /// </summary>
    private void BindControls()
    {
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
    public override void Show()
    {
        base.Show();
        LoadSettings();
    }

    /// <summary>
    /// Handles mouse sensitivity slider value changes.
    /// </summary>
    /// <param name="value">The new sensitivity value.</param>
    private void OnMouseSensitivityChanged(float value)
    {
        SaveSetting(SENSITIVITY_KEY, value);
        // TODO: Apply to player controller
    }

    /// <summary>
    /// Handles volume slider value changes.
    /// </summary>
    /// <param name="value">The new volume value.</param>
    private void OnVolumeChanged(float value)
    {
        SaveSetting(VOLUME_KEY, value);
        AudioListener.volume = value;
    }

    /// <summary>
    /// Handles the Back button click event.
    /// </summary>
    private void OnBackClick()
    {
        Hide();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPauseMenu();
    }

    /// <summary>
    /// Saves a setting value to PlayerPrefs.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to save.</param>
    private void SaveSetting(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads all settings from PlayerPrefs and updates UI controls.
    /// </summary>
    private void LoadSettings()
    {
        if (mouseSensitivitySlider != null)
        {
            float sensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 1.0f);
            mouseSensitivitySlider.value = sensitivity;
        }

        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat(VOLUME_KEY, 1.0f);
            volumeSlider.value = volume;
        }
    }
}
