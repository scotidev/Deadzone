using System.Collections;
using UnityEngine;

/// <summary>
/// This script simulates a police light siren effect by alternating between two light sources (red and blue).
/// It creates a visual loop where one light is active while the other is inactive, switching at a defined interval.
/// </summary>
public class PoliceLight : MonoBehaviour {

    #region FIELDS

    [Header("Light References")]
    // Serialized field to assign the red light component from the inspector.
    [SerializeField] private Light redLight;
    // Serialized field to assign the blue light component from the inspector.
    [SerializeField] private Light blueLight;

    [Header("Mesh References (Sirens)")]
    // Renderer of the red siren cube to control its visual emission.
    [SerializeField] private Renderer redSirenRenderer;
    // Renderer of the blue siren cube to control its visual emission.
    [SerializeField] private Renderer blueSirenRenderer;

    [Header("Siren Settings")]
    // The duration in seconds each light remains active before switching.
    [SerializeField] private float flashInterval = 0.25f;

    [Header("Red Siren Colors")]
    // The base color of the red siren when it is OFF (Albedo).
    [SerializeField] private Color redBaseColor = Color.red;
    // The color used when the red siren is ON (Emission).
    [SerializeField] private Color redEmissionColor = Color.red;
    // Slider to control the glow intensity of the red siren. Values above 1.0 create the "neon" effect.
    [Range(0f, 30f)]
    [SerializeField] private float redIntensity = 10f;

    [Header("Blue Siren Colors")]
    // The base color of the blue siren when it is OFF (Albedo).
    [SerializeField] private Color blueBaseColor = Color.blue;
    // The color used when the blue siren is ON (Emission).
    [SerializeField] private Color blueEmissionColor = Color.blue;
    // Slider to control the glow intensity of the blue siren. Values above 1.0 create the "neon" effect.
    [Range(0f, 30f)]
    [SerializeField] private float blueIntensity = 10f;

    #endregion

    #region UNITY

    // Cache das cores emissivas pra evitar SetColor toda vez que o estado não muda
    private Color cachedRedEmission;
    private Color cachedBlueEmission;

    /// <summary>
    /// Starts the siren logic and initializes the base colors of the materials.
    /// Uses sharedMaterial para não criar instâncias únicas de material.
    /// CONCEITO: .material cria um clone na memória, quebrando o batching de instância.
    /// .sharedMaterial reusa o asset original, mantendo o batching.
    /// </summary>
    void Start() {
        if (redLight != null && blueLight != null && redSirenRenderer != null && blueSirenRenderer != null) {
            // CONCEITO: sharedMaterial em vez de material — não cria clones, preserva batching
            redSirenRenderer.sharedMaterial.SetColor("_BaseColor", redBaseColor);
            blueSirenRenderer.sharedMaterial.SetColor("_BaseColor", blueBaseColor);

            // CONCEITO: EnableKeyword só precisa ser chamado UMA VEZ no Start,
            // não a cada ciclo. O material já suporta _EMISSION, só precisa ser ativado.
            redSirenRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            blueSirenRenderer.sharedMaterial.EnableKeyword("_EMISSION");

            // Inicializa o cache com cores inválidas pra forçar o primeiro SetColor
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
    /// Infinite loop that manages the timing and state switching of the lights and emission.
    /// </summary>
    /// <returns>IEnumerator for Coroutine execution.</returns>
    private IEnumerator SirenLoop() {
        while (true) {
            SetLightsState(true, false);
            yield return new WaitForSeconds(flashInterval);

            SetLightsState(false, true);
            yield return new WaitForSeconds(flashInterval);
        }
    }

    /// <summary>
    /// Updates the state of lights and material emission colors using intensity multiplication.
    /// Só chama SetColor quando a cor muda — evita trabalho de GPU desnecessário.
    /// CONCEITO: SetColor força a GPU a atualizar o material property block.
    /// Se a cor é a mesma do frame anterior, chamar SetColor de novo é desperdício.
    /// </summary>
    /// <param name="redActive">Should the red side be active?</param>
    /// <param name="blueActive">Should the blue side be active?</param>
    private void SetLightsState(bool redActive, bool blueActive) {
        redLight.enabled = redActive;
        blueLight.enabled = blueActive;

        // Principle: Math Multiplication. To get HDR (neon), we multiply the color by an intensity value.
        // If intensity is 10, the color becomes 10x brighter, triggering the Bloom effect in the camera.
        Color finalRedColor = redActive ? redEmissionColor * redIntensity : Color.black;
        Color finalBlueColor = blueActive ? blueEmissionColor * blueIntensity : Color.black;

        // CONCEITO: Só atualiza a GPU se a cor realmente mudou
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
