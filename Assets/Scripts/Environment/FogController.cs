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

    public void EnableFog() {
        fogParticles.Play();
    }

    public void SetFogColor(Color newColor) {
        if (fogParticles == null)
            return;

        var main = fogParticles.main;
        main.startColor = newColor;
    }

    public void ResetFogColor() {
        SetFogColor(originalFogColor);
    }

    #endregion
}
