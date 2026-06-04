using UnityEngine;

/// <summary>
/// Controls the fog ParticleSystem lifecycle.
/// Fog starts disabled and is enabled by TutorialEndTrigger when the tutorial ends.
/// After activation, the fog emits at the rate configured in the ParticleSystem Inspector.
/// SafeZone uses the ParticleSystem Trigger Module to kill particles inside safe areas.
/// Attach this script to the fog GameObject, child of the Player.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class FogController : MonoBehaviour {

    #region FIELDS

    private ParticleSystem fogParticles;
    private Color originalFogColor;

    #endregion

    #region UNITY 

    private void Awake() {
        fogParticles = GetComponent<ParticleSystem>();

        var main = fogParticles.main;
        originalFogColor = main.startColor.color;
    }

    private void Start() {
        fogParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        fogParticles.Clear(true);
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Enables fog emission. Called by TutorialEndTrigger when the tutorial ends.
    /// After this, the fog emits at the rate configured in the ParticleSystem Inspector.
    /// </summary>
    public void EnableFog() {
        fogParticles.Play();
    }

    /// <summary>
    /// Changes the color of the fog particles to the specified color.
    /// Used by WaveManager during boss waves to change fog to red.
    /// </summary>
    public void SetFogColor(Color newColor) {
        if (fogParticles == null)
            return;

        var main = fogParticles.main;
        main.startColor = newColor;
    }

    /// <summary>
    /// Resets the fog color back to its original color.
    /// Used by WaveManager when a boss wave ends.
    /// </summary>
    public void ResetFogColor() {
        SetFogColor(originalFogColor);
    }

    #endregion
}
