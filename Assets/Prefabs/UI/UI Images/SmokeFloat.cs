using UnityEngine;

/// <summary>
/// Applies a subtle and continuous floating motion to a UI smoke element
/// without resetting or leaving the visible area abruptly.
/// </summary>
public class SmokeFloat : MonoBehaviour
{
    [Header("Vertical Motion")]
    [SerializeField] private float verticalAmplitude = 18f;
    [SerializeField] private float verticalFrequency = 0.2f;

    [Header("Horizontal Drift")]
    [SerializeField] private float horizontalAmplitude = 8f;
    [SerializeField] private float horizontalFrequency = 0.12f;

    [Header("Phase")]
    [SerializeField] private bool randomizePhase = true;

    private Vector3 initialLocalPosition;
    private float motionPhaseOffset;

    /// <summary>
    /// Caches the initial position and prepares the motion phase offset.
    /// </summary>
    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        motionPhaseOffset = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    /// <summary>
    /// Updates the smoke floating motion every frame.
    /// </summary>
    private void Update()
    {
        ApplyFloatingMotion();
    }

    /// <summary>
    /// Applies a smooth looped offset using sine waves to keep motion continuous.
    /// </summary>
    private void ApplyFloatingMotion()
    {
        float time = Time.time + motionPhaseOffset;

        float verticalOffset = Mathf.Sin(time * Mathf.PI * 2f * verticalFrequency) * verticalAmplitude;
        float horizontalOffset = Mathf.Cos(time * Mathf.PI * 2f * horizontalFrequency) * horizontalAmplitude;

        Vector3 offset = new Vector3(horizontalOffset, verticalOffset, 0f);
        transform.localPosition = initialLocalPosition + offset;
    }
}

