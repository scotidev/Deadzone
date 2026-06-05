using System;
using UnityEngine;

/// <summary>
/// Static manager that tracks whether the Penguin Easter egg has been activated.
/// Only one activation per game session. The effect only lasts for the wave
/// in which it was activated — subsequent waves return to normal enemies.
/// </summary>
public static class PenguinMode {

    /// <summary>Whether the easter egg has been activated this game (prevents re-activation).</summary>
    public static bool Active { get; private set; }

    /// <summary>The wave number in which the easter egg was activated.</summary>
    public static int ActivatedAtWave { get; private set; }

    /// <summary>
    /// True only if the easter egg is active AND the current wave matches the activation wave.
    /// This ensures only one wave is affected.
    /// </summary>
    public static bool IsCurrentWavePenguinWave =>
        Active && WaveManager.Instance != null && WaveManager.Instance.CurrentWave == ActivatedAtWave;

    /// <summary>Fired when the easter egg is first activated.</summary>
    public static event Action OnActivated;

    /// <summary>
    /// Tries to activate penguin mode. Only succeeds once per game.
    /// </summary>
    public static void Activate(int currentWave) {
        if (Active) return;

        Active = true;
        ActivatedAtWave = currentWave;
        Debug.Log($"[PenguinMode] ACTIVATED at wave {currentWave}!");

        OnActivated?.Invoke();
    }

    /// <summary>
    /// Resets the state. Call when starting a new game.
    /// </summary>
    public static void Reset() {
        Active = false;
        ActivatedAtWave = 0;
    }
}
