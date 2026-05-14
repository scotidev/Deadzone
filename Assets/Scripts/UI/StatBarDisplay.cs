using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a stat with label, icon, and three-layer bar (background, upgrade, current).
/// </summary>
public class StatBarDisplay : MonoBehaviour {

    [Header("Elements")]
    [SerializeField] private TextMeshProUGUI statLabel;
    [SerializeField] private Image statIcon;
    [SerializeField] private Image backgroundBar;
    [SerializeField] private Image currentBar;
    [SerializeField] private Image upgradeBar;

    [Header("Colors")]
    [SerializeField] private Color currentColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private Color upgradeColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Stat Icons")]
    [SerializeField] private Sprite iconDamage;
    [SerializeField] private Sprite iconFireRate;
    [SerializeField] private Sprite iconAmmo;
    [SerializeField] private Sprite iconHeal;
    [SerializeField] private Sprite iconResistance;
    [SerializeField] private Sprite iconRadius;
    [SerializeField] private Sprite iconDefault;

    private float maxStatValue = 100f;

    /// <summary>
    /// Sets up the stat bar with label, max value, and slot index.
    /// </summary>
    public void Setup(string label, float maxValue, int slotIndex) {
        maxStatValue = maxValue;

        if (statLabel != null) {
            statLabel.text = label;
        }

        if (statIcon != null) {
            Sprite icon = GetIconForStat(label);
            statIcon.sprite = icon != null ? icon : iconDefault;
            statIcon.enabled = icon != null || iconDefault != null;
        }

        UpdateBarColors();
    }

    /// <summary>
    /// Sets the current and upgrade values for the stat bar.
    /// </summary>
    /// <param name="current">Current stat value.</param>
    /// <param name="upgrade">Upgrade stat value (preview).</param>
    /// <param name="showUpgrade">Whether to show the upgrade bar.</param>
    public void SetValues(float current, float upgrade, bool showUpgrade = true) {
        float currentFill = maxStatValue > 0 ? current / maxStatValue : 0f;
        float upgradeFill = maxStatValue > 0 ? upgrade / maxStatValue : 0f;

        if (backgroundBar != null) {
            backgroundBar.fillAmount = 1f;
        }

        if (upgradeBar != null) {
            upgradeBar.fillAmount = showUpgrade ? Mathf.Clamp01(upgradeFill) : 0f;
            upgradeBar.enabled = showUpgrade;
        }

        if (currentBar != null) {
            currentBar.fillAmount = Mathf.Clamp01(currentFill);
        }
    }

    private void UpdateBarColors() {
        if (backgroundBar != null) backgroundBar.color = backgroundColor;
        if (currentBar != null) currentBar.color = currentColor;
        if (upgradeBar != null) upgradeBar.color = upgradeColor;
    }

    private Sprite GetIconForStat(string statName) {
        if (string.IsNullOrEmpty(statName)) return iconDefault;

        string lower = statName.ToLower();

        if (lower.Contains("damage")) return iconDamage;
        if (lower.Contains("fire rate") || lower.Contains("firerate")) return iconFireRate;
        if (lower.Contains("ammo") || lower.Contains("magazine")) return iconAmmo;
        if (lower.Contains("heal")) return iconHeal;
        if (lower.Contains("resistance") || lower.Contains("resist")) return iconResistance;
        if (lower.Contains("radius")) return iconRadius;

        return iconDefault;
    }

    public void SetBarColors(Color current, Color upgrade, Color background) {
        currentColor = current;
        upgradeColor = upgrade;
        backgroundColor = background;
        UpdateBarColors();
    }
}
