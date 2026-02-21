using UnityEngine;

/// <summary>
/// Manages the wave-based enemy spawning system in the game.
/// Controls wave progression and handles the logic for starting new waves.
/// </summary>
public class WaveManager : MonoBehaviour {
    public static WaveManager Instance { get; private set; }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initiates the next wave of enemies.
    /// </summary>
    public void StartNextWave() {
        Debug.Log("WaveManager: Next Wave Called");
    }
}
