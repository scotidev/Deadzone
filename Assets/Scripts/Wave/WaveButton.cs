using UnityEngine;

/// <summary>
/// Interactive button in the 3D world that starts the next wave of enemies.
/// Interation is blocked if a wave is already active, with a log message for feedback.
/// </summary>
public class WaveButton : Interactable {

    #region METHODS

    public override void Interact() {
        if (WaveManager.Instance == null) return;

        if (WaveManager.Instance.IsWaveActive) {
            return;
        }

        WaveManager.Instance.StartNextWave();
    }

    #endregion
}
