using UnityEngine;

/// <summary>
/// Responsible for controlling the emission of the fog ParticleSystem based on the player's poison state from PlayerHealth (inside or outside the SafeZone).
/// Attatch this script to the fog GameObject, which should be a child of the Player.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class FogController : MonoBehaviour {

    [Header("Fog Emission")]
    [Tooltip("Particles per second emitted when the player is outside the SafeZone.")]
    [SerializeField] private float emissionRateOutside = 40f;

    private PlayerHealth playerHealth;
    private ParticleSystem fogParticles;

    private ParticleSystem.EmissionModule emission;

    private void Awake() {
        fogParticles = GetComponent<ParticleSystem>();
        emission = fogParticles.emission;

        playerHealth = GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            Debug.LogError("[FogController] PlayerHealth not found in parent! ");
    }

    private void OnEnable() {
        if (playerHealth != null)
            playerHealth.OnPoisonStateChanged += HandlePoisonStateChanged;
    }

    private void OnDisable() {
        if (playerHealth != null)
            playerHealth.OnPoisonStateChanged -= HandlePoisonStateChanged;
    }

    private void Start() {
        SetEmissionRate(0f);
        fogParticles.Play();
    }

    /// <summary>
    /// Called by PlayerHealth.OnPoisonStateChanged.
    /// poisoned = true -> player left the house → dense fog active.
    /// poisoned = false -> player entered the safezone → fog stops.
    /// </summary>
    private void HandlePoisonStateChanged(bool poisoned) {
        SetEmissionRate(poisoned ? emissionRateOutside : 0f);
    }

    /// <summary>
    /// rateOverTime accepts a MinMaxCurve. Assigning a float directly creates a constant value curve equal to that float.
    /// </summary>
    /// <param name="rate"></param>
    private void SetEmissionRate(float rate) {
        emission.rateOverTime = rate;
    }
}
