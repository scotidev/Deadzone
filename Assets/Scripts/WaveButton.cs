using UnityEngine;

/// <summary>
/// Represents an interactable button that triggers the next wave of enemies.
/// </summary>
public class WaveButton : Interactable {
    /// <summary>
    /// Implements the interaction behavior inherited from Interactable for wave buttons.
    /// Calls the WaveManager to start the next wave when the player interacts.
    /// </summary>
    public override void Interact() {
        if (WaveManager.Instance != null) {
            WaveManager.Instance.StartNextWave();
        }
    }
}
