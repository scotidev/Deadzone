using System.Collections;
using UnityEngine;

// REFATORAÇÃO: o audio deve tocar por aqui implementando o Service de Audio, e não por script separado. Usar o AudioManagerService de dentro do dicionário de serviçoos para tocar o som de falha de luz, por exemplo, quando a luz "falha" e não pisca naquele intervalo, usar som 3D
// REFATORAÇÃO: isFlickering é necesasrio?

/// <summary>
/// This VFX script simulates a flickering light effect, used to represent faulty or unstable light sources in the game. It randomly changes the light's intensity at varying intervals to create a dynamic and unpredictable flickering pattern.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour {

    #region FIELDS

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

    private Light myLight;
    private bool isFlickering = false;

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

    private IEnumerator FlickerRoutine() {
        while (useFlicker) {
            float waitTime = Random.Range(timeBetweenBursts.x, timeBetweenBursts.y);
            yield return new WaitForSeconds(waitTime);

            yield return StartCoroutine(DoBurst());
        }
    }

    private IEnumerator DoBurst() {
        isFlickering = true;
        int burstCount = (int)Random.Range(flickersPerBurst.x, flickersPerBurst.y);

        //PlayFlickerSound();

        for (int i = 0; i < burstCount; i++) {
            myLight.intensity = Random.Range(minIntensity, maxIntensity);

            yield return new WaitForSeconds(flickerSpeed);
        }

        myLight.intensity = (Random.value > 0.5f) ? defaultIntensity : 0f;

        isFlickering = false;
    }

    //private void PlayFlickerSound() {
    //    // Exemplo de uso do Service Locator para o AudioManager
    //    var audioManager = ServiceLocator.Instance.GetService<IAudioManagerService>();
    //    if (audioManager != null) {
    //        // Toca com volume baixo conforme pedido
    //        audioManager.PlayOneShot(flickerAudioKey, 0.3f);
    //    }
    //}

    #endregion
}