using System.Collections;
using UnityEngine;

/// <summary>
/// Simulates a flickering light effect for faulty or unstable light sources.
/// Randomly changes light intensity at varying intervals to create a dynamic flicker pattern.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Flicker Event Settings")]
    public bool useFlicker = true;
    public float minIntensity = 0f;
    public float maxIntensity = 1.5f;
    public float defaultIntensity = 1f;

    [Header("Burst Logic")]
    public Vector2 timeBetweenBursts = new Vector2(2f, 5f);
    public Vector2 flickersPerBurst = new Vector2(3, 8);
    public float flickerSpeed = 0.05f;

    [Header("Audio")]
    public AudioClip flickerAudio;

    #endregion

    #region FIELDS

    private Light myLight;

    #endregion

    #region UNITY

    void Start() {
        myLight = GetComponent<Light>();
        myLight.intensity = defaultIntensity;

        if (useFlicker) {
            StartCoroutine(FlickerRoutine());
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Main flicker loop that waits a random interval between bursts then triggers a flicker burst.
    /// </summary>
    private IEnumerator FlickerRoutine() {
        while (useFlicker) {
            float waitTime = Random.Range(timeBetweenBursts.x, timeBetweenBursts.y);
            yield return new WaitForSeconds(waitTime);

            yield return StartCoroutine(DoBurst());
        }
    }

    /// <summary>
    /// Executes a single flicker burst, randomly changing light intensity multiple times in rapid succession.
    /// </summary>
    private IEnumerator DoBurst() {
        int burstCount = (int)Random.Range(flickersPerBurst.x, flickersPerBurst.y);

        for (int i = 0; i < burstCount; i++) {
            myLight.intensity = Random.Range(minIntensity, maxIntensity);

            yield return new WaitForSeconds(flickerSpeed);
        }

        myLight.intensity = (Random.value > 0.5f) ? defaultIntensity : 0f;
    }

    #endregion
}
