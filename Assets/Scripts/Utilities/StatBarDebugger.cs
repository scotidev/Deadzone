using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Debug utility for validating stat bar calculation and display.
/// Usage: Add this to a GameObject in the scene and check the debug output when the shop opens.
/// </summary>
public class StatBarDebugger : MonoBehaviour {

    [SerializeField] private bool logOnStart = true;
    [SerializeField] private bool logOnShopOpen = true;
    private bool hasLoggedOnce = false;

    private void Start() {
        if (logOnStart) {
            LogStats("START");
        }
    }

    private void OnEnable() {
        if (logOnShopOpen && !hasLoggedOnce) {
            Invoke(nameof(DelayedLog), 0.5f);
        }
    }

    private void DelayedLog() {
        hasLoggedOnce = true;
        LogStats("SHOP OPENED");
    }

    /// <summary>
    /// Main logging function that outputs all stat calculations
    /// </summary>
    public void LogStats(string context) {
        Debug.Log($"\n╔════════════════════════════════════════════════════════════╗");
        Debug.Log($"║  STAT BAR DEBUG LOG - {context.PadRight(39)}║");
        Debug.Log($"╚════════════════════════════════════════════════════════════╝");

        // Log global max values
        WeaponStatsCalculator.LogCurrentMaxValues();

        // Log each shop item configuration
        ShopUI shopUI = ShopUI.Instance;
        if (shopUI == null || shopUI.SelectedItemData == null) {
            Debug.LogWarning("[StatBarDebugger] ShopUI or SelectedItemData not found!");
            return;
        }

        ShopItemDataSO selectedItem = shopUI.SelectedItemData;
        if (selectedItem?.ItemData == null) {
            Debug.LogWarning("[StatBarDebugger] ItemData is null!");
            return;
        }

        string itemID = selectedItem.ItemID;
        string itemName = selectedItem.ItemName;
        ItemDataSO itemData = selectedItem.ItemData;
        int maxLevel = itemData.MaxUpgradeLevel;

        Debug.Log($"\n┌────────────────────────────────────────────────────────────┐");
        Debug.Log($"│  SELECTED ITEM: {itemName.PadRight(48)}│");
        Debug.Log($"│  ID: {itemID.PadRight(56)}│");
        Debug.Log($"└────────────────────────────────────────────────────────────┘");

        // Get current and max level values
        int currentLevel = PlayerProgress.Instance?.GetItemLevel(itemID) ?? 1;
        float[] maxLevelStats = itemData.GetStatValues(maxLevel);
        string[] labels = itemData.GetStatLabels();

        Debug.Log($"\n  Current Level: {currentLevel} / {maxLevel}");
        Debug.Log($"  Stat Count: {labels.Length}\n");

        for (int i = 0; i < labels.Length && i < maxLevelStats.Length && i < 3; i++) {
            string label = labels[i];
            float maxValue = WeaponStatsCalculator.GetMaxValueForStat(label);
            float statValueAtMax = maxLevelStats[i];
            float fillPercentage = (maxValue > 0) ? (statValueAtMax / maxValue) * 100f : 0f;

            Debug.Log($"  [{i + 1}] {label.PadRight(18)}: {statValueAtMax,6:F1} / {maxValue,6:F1} ({fillPercentage,5:F1}%)");
        }

        Debug.Log($"\n╔════════════════════════════════════════════════════════════╗\n");
    }

    /// <summary>
    /// Call this from console to reset and recalculate everything
    /// </summary>
    public void ResetAndRecalculate() {
        Debug.Log("[StatBarDebugger] Resetting calculation...");
        WeaponStatsCalculator.ResetCalculation();
        
        ShopUI shopUI = ShopUI.Instance;
        if (shopUI != null && shopUI.SelectedItemData != null) {
            Debug.Log("[StatBarDebugger] Recalculating with current shop items...");
            // This would need access to shopItems list - use LogStats instead
            LogStats("MANUAL RESET");
        } else {
            Debug.LogWarning("[StatBarDebugger] ShopUI not available!");
        }
    }
}
