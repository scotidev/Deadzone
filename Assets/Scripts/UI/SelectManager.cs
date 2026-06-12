using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using UnityEngine.UI;

/// <summary>
/// Controls the map selection screen, including background preview
/// transitions and loading the selected map scene.
/// </summary>
public class SelectManager : MonoBehaviour {

    /// <summary>
    /// The available map options in the selection screen.
    /// </summary>
    private enum MapOption {
        City,
        Desert,
        Forest
    }

    /// <summary>
    /// Data container for each map card and its scene/preview configuration.
    /// </summary>
    [System.Serializable]
    private class MapEntry {
        [Tooltip("Map identifier used by SelectManager methods")]
        public MapOption option;

        [Tooltip("UI element representing this map card")]
        public RectTransform mapCard;

        [Tooltip("Background object shown while hovering/selecting this map")]
        public GameObject previewBackgroundObject;

        [Tooltip("Scene name loaded when this map is selected")]
        public string sceneName;
    }

    [Header("Background Preview")]
    [Tooltip("Default background object shown when no map is selected")]
    [SerializeField] private GameObject defaultBackgroundObject;

    [Header("Map Cards")]
    [Tooltip("Configuration for City, Desert, and Forest map cards")]
    [SerializeField] private MapEntry[] mapEntries;

    [Header("Audio")]
    [Tooltip("BGM played while the map selection screen is active")]
    [SerializeField] private AudioClip selectScreenBGM;

    [Range(0f, 1f)]
    [Tooltip("Per-track volume for the selection screen BGM")]
    [SerializeField] private float selectScreenBGMVolume = 1f;

    [Tooltip("Smooth fade duration when starting selection screen BGM")]
    [SerializeField] private float selectScreenBGMFadeDuration = 0.5f;

    private GameObject selectedBackgroundObject;
    private IAudioManagerService audioService;

    /// <summary>
    /// Initializes visual defaults for backgrounds and map cards.
    /// </summary>
    private void Start() {
        // We are in a menu context, so the GameState should be MainMenu.
        GameManager.Instance?.SetState(GameState.MainMenu);

        // We resolve the shared audio service from the locator so this screen uses the same audio pipeline as the whole game.
        audioService = ServiceLocator.Current.Get<IAudioManagerService>();

        // We start the selection screen BGM once, keeping music management centralized in the audio service.
        audioService?.PlayBGM(selectScreenBGM, true, selectScreenBGMFadeDuration, selectScreenBGMVolume);

        InitializeBackgroundImages();
        ConfigureBackgroundRaycastBehavior();
    }

    /// <summary>
    /// Handles hover enter for the City map card.
    /// </summary>
    public void OnCityHoverEnter() {
        HandleMapHoverEnter(MapOption.City);
    }

    /// <summary>
    /// Handles hover enter for the Desert map card.
    /// </summary>
    public void OnDesertHoverEnter() {
        HandleMapHoverEnter(MapOption.Desert);
    }

    /// <summary>
    /// Handles hover enter for the Forest map card.
    /// </summary>
    public void OnForestHoverEnter() {
        HandleMapHoverEnter(MapOption.Forest);
    }

    /// <summary>
    /// Handles hover exit for any map card, restoring selected or default visuals.
    /// </summary>
    public void OnMapHoverExit() {
        ShowSelectedOrDefaultBackground();
    }

    /// <summary>
    /// Loads the City map scene configured in the inspector.
    /// </summary>
    public void OnCitySelect() {
        SelectAndLoadMap(MapOption.City);
    }

    /// <summary>
    /// Loads the Desert map scene configured in the inspector.
    /// </summary>
    public void OnDesertSelect() {
        SelectAndLoadMap(MapOption.Desert);
    }

    /// <summary>
    /// Loads the Forest map scene configured in the inspector.
    /// </summary>
    public void OnForestSelect() {
        SelectAndLoadMap(MapOption.Forest);
    }

    /// <summary>
    /// Shows the background preview for a hovered map card.
    /// </summary>
    /// <param name="option">The hovered map option.</param>
    private void HandleMapHoverEnter(MapOption option) {
        MapEntry entry = GetEntry(option);
        if (entry == null) {
            return;
        }

        ShowEntryBackground(entry);
    }

    /// <summary>
    /// Stores the selected map preview as persistent background and then loads its scene.
    /// </summary>
    /// <param name="option">The selected map option.</param>
    private void SelectAndLoadMap(MapOption option) {
        MapEntry entry = GetEntry(option);
        if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName)) {
            Debug.LogWarning($"[SelectManager] SelectAndLoadMap FAILED: entry null ou sceneName vazio | option={option}");
            return;
        }

        Debug.Log($"[SelectManager] SelectAndLoadMap | option={option} | sceneName='{entry.sceneName}'");

        // We persist the selected background so hover exit returns to this selection.
        selectedBackgroundObject = entry.previewBackgroundObject;
        ShowSelectedOrDefaultBackground();
        SceneLoader.Instance?.LoadScene(entry.sceneName);
    }

    /// <summary>
    /// Displays the selected background if available, otherwise displays the configured default background.
    /// </summary>
    private void ShowSelectedOrDefaultBackground() {
        ShowBackgroundObject(selectedBackgroundObject != null ? selectedBackgroundObject : defaultBackgroundObject);
    }

    /// <summary>
    /// Displays the background configured for a specific map entry.
    /// </summary>
    /// <param name="entry">Map entry that provides object or sprite background.</param>
    private void ShowEntryBackground(MapEntry entry) {
        ShowBackgroundObject(entry.previewBackgroundObject != null ? entry.previewBackgroundObject : defaultBackgroundObject);
    }

    /// <summary>
    /// Activates one background object and deactivates the others so only one preview is visible at a time.
    /// </summary>
    /// <param name="targetObject">Background object that should remain active.</param>
    private void ShowBackgroundObject(GameObject targetObject) {
        // We first disable every known background object to enforce a single visual source.
        if (defaultBackgroundObject != null) {
            defaultBackgroundObject.SetActive(false);
        }

        if (mapEntries != null) {
            for (int i = 0; i < mapEntries.Length; i++) {
                if (mapEntries[i] != null && mapEntries[i].previewBackgroundObject != null) {
                    mapEntries[i].previewBackgroundObject.SetActive(false);
                }
            }
        }

        // Then we enable only the target object (if assigned).
        if (targetObject != null) {
            // We keep preview backgrounds visually behind cards so they never overlap interactive UI.
            targetObject.transform.SetAsFirstSibling();
            targetObject.SetActive(true);
        }
    }

    /// <summary>
    /// Disables raycast blocking on all configured background objects so map cards always receive pointer events.
    /// </summary>
    private void ConfigureBackgroundRaycastBehavior() {
        ConfigureObjectRaycast(defaultBackgroundObject);

        if (mapEntries == null || mapEntries.Length == 0) {
            return;
        }

        for (int i = 0; i < mapEntries.Length; i++) {
            if (mapEntries[i] == null) {
                continue;
            }

            ConfigureObjectRaycast(mapEntries[i].previewBackgroundObject);
        }
    }

    /// <summary>
    /// Disables raycast target on every Graphic under a background object hierarchy.
    /// </summary>
    /// <param name="backgroundObject">Background object root that may contain UI graphics.</param>
    private void ConfigureObjectRaycast(GameObject backgroundObject) {
        if (backgroundObject == null) {
            return;
        }

        // We disable raycast on all graphics to prevent invisible/fullscreen layers from stealing mouse hover.
        Graphic[] graphics = backgroundObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++) {
            graphics[i].raycastTarget = false;
        }
    }

    /// <summary>
    /// Finds a configured map entry by option.
    /// </summary>
    /// <param name="option">The map option to search.</param>
    /// <returns>The configured entry or null when not found.</returns>
    private MapEntry GetEntry(MapOption option) {
        if (mapEntries == null || mapEntries.Length == 0) {
            return null;
        }

        for (int i = 0; i < mapEntries.Length; i++) {
            if (mapEntries[i] != null && mapEntries[i].option == option) {
                return mapEntries[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Prepares background images to support smooth overlay fades.
    /// </summary>
    private void InitializeBackgroundImages() {
        // We initialize object mode so only the default background stays active at startup.
        ShowBackgroundObject(defaultBackgroundObject);
        selectedBackgroundObject = defaultBackgroundObject;
    }

}
