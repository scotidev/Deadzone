using TMPro;
using UnityEngine;

/// <summary>
/// Persistent HUD panel that displays wave information during gameplay.
/// </summary>
public class WaveUI : BaseUI {

    [Header("Wave Information Texts")]
    [Tooltip("Displays the current wave number. Example: 'Wave 3'")]
    [SerializeField] private TMP_Text waveNumberText;

    [Tooltip("Displays how many enemies are still alive in this wave.")]
    [SerializeField] private TMP_Text enemiesRemainingText;

    protected override void Start() {
        base.Start();
        Show();

        if (waveNumberText == null)
            Debug.LogError("[WaveUI] 'Wave Number Text' not assigned in the Inspector!");

        if (enemiesRemainingText == null)
            Debug.LogError("[WaveUI] 'Enemies Remaining Text' not assigned in the Inspector!");

        UpdateWaveNumber(0);
        UpdateEnemiesRemaining(0);
    }

    /// <summary>
    /// Updates the wave number label.
    /// Wave 0 (initial state) displays "Wave —".
    /// </summary>
    public void UpdateWaveNumber(int wave) {
        if (waveNumberText != null)
            waveNumberText.text = wave == 0 ? "Wave " : $"Wave {wave}";
    }

    /// <summary>
    /// Updates the enemies remaining label.
    /// Called by the WaveManager each time an enemy dies.
    /// </summary>
    public void UpdateEnemiesRemaining(int count) {
        if (enemiesRemainingText != null)
            enemiesRemainingText.text = $"Enemies {count}";
    }
}
