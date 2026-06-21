using System.Collections;
using UnityEngine;

/// <summary>
/// Simulates a police light siren effect by alternating between two light sources (red and blue).
/// Creates a visual loop where one light is active while the other is inactive, switching at a defined interval.
/// </summary>
public class PoliceLight : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Light References")]
    [SerializeField] private Light redLight;
    [SerializeField] private Light blueLight;

    [Header("Mesh References (Sirens)")]
    [SerializeField] private Renderer redSirenRenderer;
    [SerializeField] private Renderer blueSirenRenderer;

    [Header("Siren Settings")]
    [SerializeField] private float flashInterval = 0.25f;

    [Header("Red Siren Colors")]
    [SerializeField] private Color redBaseColor = Color.red;
    [SerializeField] private Color redEmissionColor = Color.red;
    [Range(0f, 30f)]
    [SerializeField] private float redIntensity = 10f;

    [Header("Blue Siren Colors")]
    [SerializeField] private Color blueBaseColor = Color.blue;
    [SerializeField] private Color blueEmissionColor = Color.blue;
    [Range(0f, 30f)]
    [SerializeField] private float blueIntensity = 10f;

    #endregion

    #region FIELDS

    private Color cachedRedEmission;
    private Color cachedBlueEmission;

    #endregion

    #region UNITY

    /// <summary>
    /// Initializes the siren logic, sets the base colors on shared materials, and starts the siren loop.
    /// </summary>
    void Start() {
        if (redLight != null && blueLight != null && redSirenRenderer != null && blueSirenRenderer != null) {
            redSirenRenderer.sharedMaterial.SetColor("_BaseColor", redBaseColor);
            blueSirenRenderer.sharedMaterial.SetColor("_BaseColor", blueBaseColor);

            redSirenRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            blueSirenRenderer.sharedMaterial.EnableKeyword("_EMISSION");

            cachedRedEmission = Color.black;
            cachedBlueEmission = Color.black;

            StartCoroutine(SirenLoop());
        }
        else {
            Debug.LogWarning("PoliceLight: Some references are missing!");
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Alternates red and blue lights at the configured flash interval.
    /// </summary>
    private IEnumerator SirenLoop() {
        while (true) {
            SetLightsState(true, false);
            yield return new WaitForSeconds(flashInterval);

            SetLightsState(false, true);
            yield return new WaitForSeconds(flashInterval);
        }
    }

    /// <summary>
    /// Updates light enabled state and material emission colors, skipping GPU updates when the color has not changed.
    /// </summary>
    /// <param name="redActive">Should the red side be active?</param>
    /// <param name="blueActive">Should the blue side be active?</param>
    private void SetLightsState(bool redActive, bool blueActive) {
        redLight.enabled = redActive;
        blueLight.enabled = blueActive;

        Color finalRedColor = redActive ? redEmissionColor * redIntensity : Color.black;
        Color finalBlueColor = blueActive ? blueEmissionColor * blueIntensity : Color.black;

        if (cachedRedEmission != finalRedColor)
        {
            redSirenRenderer.sharedMaterial.SetColor("_EmissionColor", finalRedColor);
            cachedRedEmission = finalRedColor;
        }
        if (cachedBlueEmission != finalBlueColor)
        {
            blueSirenRenderer.sharedMaterial.SetColor("_EmissionColor", finalBlueColor);
            cachedBlueEmission = finalBlueColor;
        }
    }

    #endregion
}
