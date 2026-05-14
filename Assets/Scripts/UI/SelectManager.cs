using System.Collections;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using UnityEngine.UI;

/// <summary>
/// Controls the map selection screen, including hover scale feedback,
/// background preview transitions, and loading the selected map scene.
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

        [Tooltip("UI element that scales when this map is hovered")]
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

    [Tooltip("Scale for map cards while idle")]
    [SerializeField] private Vector3 normalScale = Vector3.one;

    [Tooltip("Scale for map cards while hovered")]
    [SerializeField] private Vector3 hoveredScale = new Vector3(1.08f, 1.08f, 1f);

    [Tooltip("Seconds used to animate card scale")]
    [SerializeField] private float cardScaleDuration = 0.12f;

    [Header("Audio")]
    [Tooltip("BGM played while the map selection screen is active")]
    [SerializeField] private AudioClip selectScreenBGM;

    [Tooltip("Smooth fade duration when starting selection screen BGM")]
    [SerializeField] private float selectScreenBGMFadeDuration = 0.5f;

    [Tooltip("SFX played when hovering a map card")]
    [SerializeField] private AudioClip mapHoverSFX;

    [Tooltip("SFX played when selecting a map")]
    [SerializeField] private AudioClip mapSelectSFX;

    [Tooltip("Volume multiplier for map hover SFX")]
    [SerializeField] private float mapHoverVolume = 1f;

    [Tooltip("Volume multiplier for map select SFX")]
    [SerializeField] private float mapSelectVolume = 1f;

    private GameObject selectedBackgroundObject;
    private IAudioManagerService audioService;

    /// <summary>
    /// Initializes visual defaults for backgrounds and map cards.
    /// </summary>
    private void Start() {
        // We resolve the shared audio service from the locator so this screen uses the same audio pipeline as the whole game.
        audioService = ServiceLocator.Current.Get<IAudioManagerService>();

        // We start the selection screen BGM once, keeping music management centralized in the audio service.
        audioService?.PlayBGM(selectScreenBGM, true, selectScreenBGMFadeDuration);

        InitializeBackgroundImages();
        ConfigureBackgroundRaycastBehavior();
        ResetAllCardsScale();
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
        ResetAllCardsScale();
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
    /// Applies hover feedback to a map card and starts its background preview.
    /// </summary>
    /// <param name="option">The hovered map option.</param>
    private void HandleMapHoverEnter(MapOption option) {
        MapEntry entry = GetEntry(option);
        if (entry == null) {
            return;
        }

        // We play a 2D hover sound because UI audio should not be spatialized in world coordinates.
        audioService?.PlaySFX2D(mapHoverSFX, mapHoverVolume);

        ResetAllCardsScale();
        AnimateCardScale(entry.mapCard, hoveredScale);
        ShowEntryBackground(entry);
    }

    /// <summary>
    /// Stores the selected map preview as persistent background and then loads its scene.
    /// </summary>
    /// <param name="option">The selected map option.</param>
    private void SelectAndLoadMap(MapOption option) {
        MapEntry entry = GetEntry(option);
        if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName)) {
            return;
        }

        // We confirm selection with a 2D UI sound before scene transition for immediate user feedback.
        audioService?.PlaySFX2D(mapSelectSFX, mapSelectVolume);

        // We persist the selected background object so hover exit returns to this selection.
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

    /// <summary>
    /// Resets all map cards to idle scale.
    /// </summary>
    private void ResetAllCardsScale() {
        if (mapEntries == null || mapEntries.Length == 0) {
            return;
        }

        for (int i = 0; i < mapEntries.Length; i++) {
            if (mapEntries[i] == null || mapEntries[i].mapCard == null) {
                continue;
            }

            AnimateCardScale(mapEntries[i].mapCard, normalScale);
        }
    }

    /// <summary>
    /// Animates one card scale to the target value.
    /// </summary>
    /// <param name="targetCard">Card transform to animate.</param>
    /// <param name="targetScale">Final scale value.</param>
    private void AnimateCardScale(RectTransform targetCard, Vector3 targetScale) {
        if (targetCard == null) {
            return;
        }

        StartCoroutine(ScaleCardRoutine(targetCard, targetScale));
    }

    /// <summary>
    /// Smoothly interpolates a card scale over time.
    /// </summary>
    /// <param name="targetCard">Card transform to animate.</param>
    /// <param name="targetScale">Final scale value.</param>
    /// <returns>Enumerator used by Unity coroutine system.</returns>
    private IEnumerator ScaleCardRoutine(RectTransform targetCard, Vector3 targetScale) {
        if (cardScaleDuration <= 0f) {
            targetCard.localScale = targetScale;
            yield break;
        }

        // We capture the starting scale so interpolation always begins at the current visual state.
        Vector3 startScale = targetCard.localScale;

        // Time is accumulated from 0 to duration to produce a normalized interpolation value.
        float elapsedTime = 0f;

        while (elapsedTime < cardScaleDuration) {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / cardScaleDuration);

            // Lerp blends start and target scale using normalizedTime for smooth motion.
            targetCard.localScale = Vector3.Lerp(startScale, targetScale, normalizedTime);
            yield return null;
        }

        // We snap to final value so floating-point rounding does not leave tiny visual offsets.
        targetCard.localScale = targetScale;
    }

}
