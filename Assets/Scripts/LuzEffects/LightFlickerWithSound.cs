using UnityEngine;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(AudioSource))]
public class LightFlickerWithSound : MonoBehaviour
{
    [Header("Flicker Settings")]
    public bool useFlicker = true;
    public float minIntensity = 0.2f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 10f;

    [Header("Sound Settings")]
    public AudioClip flickerSound;
    public float soundChance = 0.3f; // chance de tocar som a cada flicker
    public float minSoundPitch = 0.8f;
    public float maxSoundPitch = 1.2f;

    private Light myLight;
    private AudioSource audioSource;

    void Start()
    {
        myLight = GetComponent<Light>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void Update()
    {
        if (useFlicker)
        {
            // Flicker mais suave (Perlin Noise)
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            myLight.intensity = intensity;

            // Quando a luz "cai", chance de som
            if (noise < 0.2f && Random.value < soundChance)
            {
                PlayFlickerSound();
            }
        }
    }

    void PlayFlickerSound()
    {
        if (flickerSound != null && !audioSource.isPlaying)
        {
            audioSource.pitch = Random.Range(minSoundPitch, maxSoundPitch);
            audioSource.PlayOneShot(flickerSound);
        }
    }
}
