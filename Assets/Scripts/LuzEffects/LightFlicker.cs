using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    public bool useFlicker = true;
    public float minIntensity = 0.5f;
    public float maxIntensity = 2f;
    public float flickerSpeed = 0.1f;

    [Header("Color Settings (Traffic Light Mode)")]
    public bool useColorCycle = false;
    public Color[] colors;
    public float colorChangeTime = 2f;

    private Light myLight;
    private float timer;
    private int currentColorIndex = 0;

    void Start()
    {
        myLight = GetComponent<Light>();

        if (colors.Length > 0)
        {
            myLight.color = colors[0];
        }
    }

    void Update()
    {
        // 🔥 Flicker (poste piscando)
        if (useFlicker)
        {
            myLight.intensity = Random.Range(minIntensity, maxIntensity);
        }

        // 🚦 Troca de cor (semafaro)
        if (useColorCycle && colors.Length > 0)
        {
            timer += Time.deltaTime;

            if (timer >= colorChangeTime)
            {
                timer = 0f;
                currentColorIndex = (currentColorIndex + 1) % colors.Length;
                myLight.color = colors[currentColorIndex];
            }
        }
    }
}