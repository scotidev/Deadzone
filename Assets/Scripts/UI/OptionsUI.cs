using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;
using IGameMode = InfimaGames.LowPolyShooterPack.IGameModeService;

/// <summary>
/// Manages the options menu UI including mouse sensitivity and volume settings.
/// </summary>
public class OptionsUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Settings Controls")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Button backButton;

    #endregion

    #region FIELDS

    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string BGM_VOLUME_KEY = "BGMVolume";

    private IAudioManagerService audioService;

    #endregion

    #region PROPERTIES

    protected override bool CloseOnEscape => true;

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();

        audioService = ServiceLocator.Current.Get<IAudioManagerService>();

        BindControls();
    }

    #endregion

    #region METHODS

    private void BindControls() {
        if (mouseSensitivitySlider != null) {
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClick);
    }

    public override void Show() {
        base.Show();
        LoadSettings();
    }

    /// <summary>
    /// Saves sensitivity and applies it to the camera.
    /// </summary>
    private void OnMouseSensitivityChanged(float value) {
        SaveSetting(SENSITIVITY_KEY, value);
        ApplySensitivityToCamera(value);
    }

    /// <summary>
    /// Applies the sensitivity value to the player's CameraLook component.
    /// </summary>
    private void ApplySensitivityToCamera(float value) {
        var gameMode = ServiceLocator.Current?.Get<IGameMode>();
        var player = gameMode?.GetPlayerCharacter();
        if (player != null) {
            var cameraLook = player.GetComponentInChildren<CameraLook>(true);
            if (cameraLook != null)
                cameraLook.SetSensitivity(value);
        }
    }

    /// <summary>
    /// Saves master volume and applies it to AudioListener.
    /// </summary>
    private void OnMasterVolumeChanged(float value) {
        SaveSetting(MASTER_VOLUME_KEY, value);
        AudioListener.volume = value;
    }

    /// <summary>
    /// Saves SFX volume and applies it to the audio service.
    /// </summary>
    private void OnSFXVolumeChanged(float value) {
        SaveSetting(SFX_VOLUME_KEY, value);
        audioService?.SetSFXVolume(value);
    }

    /// <summary>
    /// Saves BGM volume and applies it to the audio service.
    /// </summary>
    private void OnBGMVolumeChanged(float value) {
        SaveSetting(BGM_VOLUME_KEY, value);
        audioService?.SetBGMVolume(value);
    }

    /// <summary>
    /// Handles the back button click, returning to pause menu.
    /// </summary>
    private void OnBackClick() {
        Hide();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPauseMenu();
    }

    /// <summary>
    /// Handles Escape key behavior by reusing the back action.
    /// </summary>
    protected override void OnEscapePressed() {
        OnBackClick();
    }

    /// <summary>
    /// Saves a float setting to PlayerPrefs.
    /// </summary>
    private void SaveSetting(string key, float value) {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads all settings from PlayerPrefs and applies them.
    /// </summary>
    private void LoadSettings() {
        if (mouseSensitivitySlider != null) {
            float sensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 1.0f);
            mouseSensitivitySlider.value = sensitivity;
            ApplySensitivityToCamera(sensitivity);
        }

        if (masterVolumeSlider != null) {
            float volume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1.0f);
            masterVolumeSlider.value = volume;
            AudioListener.volume = volume;
        }

        if (sfxVolumeSlider != null) {
            float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f);
            sfxVolumeSlider.value = sfxVolume;
            audioService?.SetSFXVolume(sfxVolume);
        }

        if (bgmVolumeSlider != null) {
            float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
            bgmVolumeSlider.value = bgmVolume;
            audioService?.SetBGMVolume(bgmVolume);
        }
    }

    #endregion
}
