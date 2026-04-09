using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual stat display using 5 fillable blocks (bars).
/// Each block can be partially filled. Shows current value in cyan and upgrade preview in green.
/// </summary>
public class StatBlockDisplay : MonoBehaviour {
    [Header("Block Images (5 total)")]
    [SerializeField] private Image[] statBlocks = new Image[5];

    [Header("Colors")]
    [SerializeField] private Color currentColor = new Color(0f, 1f, 1f, 1f); // Cyan for current
    [SerializeField] private Color upgradeColor = new Color(0f, 1f, 0f, 1f); // Green for upgrade preview
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Gray for empty

    [Header("Settings")]
    [SerializeField] private float maxStatValue = 100f;
    [SerializeField] private bool useImageFillAmount = true;

    private float currentStat = 0f;
    private float upgradedStat = 0f;

    /// <summary>
    /// Updates the stat display with current and upgrade-preview values.
    /// Automatically fills blocks proportionally to maxStatValue.
    /// </summary>
    /// <param name="current">Current stat value (shown in cyan).</param>
    /// <param name="upgraded">Stat value after upgrade (preview shown in green).</param>
    public void SetStatValues(float current, float upgraded = -1f) {
        currentStat = Mathf.Max(0f, current);
        upgradedStat = upgraded >= 0f ? Mathf.Max(0f, upgraded) : currentStat;

        RefreshDisplay();
    }

    /// <summary>
    /// Refreshes the visual representation of blocks based on current and upgrade values.
    /// </summary>
    private void RefreshDisplay() {
        if (statBlocks == null || statBlocks.Length != 5) {
            Debug.LogWarning("[StatBlockDisplay] Must have exactly 5 block images assigned!", this);
            return;
        }

        // Convert stat values to "blocks out of 5"
        // Example: if maxStatValue=100, current=75, then 3.75 blocks filled
        float currentBlocksNeeded = (currentStat / Mathf.Max(1f, maxStatValue)) * 5f;
        float upgradedBlocksNeeded = (upgradedStat / Mathf.Max(1f, maxStatValue)) * 5f;

        for (int i = 0; i < 5; i++) {
            if (statBlocks[i] == null) continue;

            // Determine how much this block should be filled
            float blockPosition = i + 1f; // Block 1 is at position 1, block 2 at position 2, etc
            float blockStartPosition = i; // Block starts filling at position i

            if (upgradedBlocksNeeded >= blockPosition) {
                // This entire block is filled in the upgraded state
                SetBlockFill(statBlocks[i], 1f, upgradeColor);
            } else if (upgradedBlocksNeeded > blockStartPosition) {
                // This block is partially filled in the upgraded state (the "new" part)
                float fillAmount = upgradedBlocksNeeded - blockStartPosition;
                SetBlockFill(statBlocks[i], fillAmount, upgradeColor);
            } else if (currentBlocksNeeded >= blockPosition) {
                // This entire block is filled in the current state (cyan)
                SetBlockFill(statBlocks[i], 1f, currentColor);
            } else if (currentBlocksNeeded > blockStartPosition) {
                // This block is partially filled in the current state (cyan)
                float fillAmount = currentBlocksNeeded - blockStartPosition;
                SetBlockFill(statBlocks[i], fillAmount, currentColor);
            } else {
                // Empty block (gray)
                SetBlockFill(statBlocks[i], 0f, emptyColor);
            }
        }
    }

    /// <summary>
    /// Sets the fill amount and color of a single block.
    /// </summary>
    private void SetBlockFill(Image blockImage, float fillAmount, Color color) {
        fillAmount = Mathf.Clamp01(fillAmount);

        if (useImageFillAmount) {
            // Use Image.fillAmount for smooth partial filling
            blockImage.fillAmount = fillAmount;
            blockImage.color = color;
        } else {
            // Alternative: scale or adjust based on custom logic
            blockImage.fillAmount = fillAmount;
            blockImage.color = color;
        }
    }

    /// <summary>
    /// Sets the maximum stat value used for scaling calculations.
    /// Useful when you want to normalize different stats to the same scale.
    /// </summary>
    public void SetMaxStatValue(float maxValue) {
        maxStatValue = Mathf.Max(1f, maxValue);
        RefreshDisplay();
    }

    /// <summary>
    /// Gets the current display value in "blocks" (0-5 range).
    /// </summary>
    public float GetCurrentBlocksCount() {
        return (currentStat / Mathf.Max(1f, maxStatValue)) * 5f;
    }

    /// <summary>
    /// Gets the upgraded display value in "blocks" (0-5 range).
    /// </summary>
    public float GetUpgradedBlocksCount() {
        return (upgradedStat / Mathf.Max(1f, maxStatValue)) * 5f;
    }
}
