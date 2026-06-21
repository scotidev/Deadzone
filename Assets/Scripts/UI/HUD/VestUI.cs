using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Manages the vest armor bar UI. Subscribes to Vest events and updates
/// the blue-gray bar fill amount in real-time when the player takes damage or adds armor.
/// </summary>
public class VestUI : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Armor References")]
    [SerializeField] private Vest vest;
    [SerializeField] private Image armorBar;
    [SerializeField] private Image armorBackground;
    [SerializeField] private Image shieldIcon;

    [Header("Animation Settings")]
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private bool useSmoothTransition = true;

    #endregion

    #region FIELDS

    private float targetFillAmount;
    private bool hasInitialized;

    #endregion

    #region UNITY

    private void Awake() {
        if (vest != null) {
            vest.OnArmorChanged += OnArmorChanged;
            vest.OnArmorDepleted += OnArmorDepleted;
        }
        
        Vest.OnVestDestroyed += OnVestDestroyed;

        armorBar.fillAmount = 1f;
        armorBackground.fillAmount = 1f;
        targetFillAmount = 1f;
    }

    private void Start() {
        if (!hasInitialized && vest != null) {
            float initialArmor = vest.GetArmorFraction();
            SetArmorInstant(initialArmor);
            
            if (initialArmor <= 0f) {
                HideArmorUI();
            } else {
                ShowArmorUI();
            }
            
            hasInitialized = true;
        }
    }

    private void Update() {
        if (!useSmoothTransition) return;
        UpdateArmorFill();
    }

    private void OnDestroy() {
        if (vest != null) {
            vest.OnArmorChanged -= OnArmorChanged;
            vest.OnArmorDepleted -= OnArmorDepleted;
        }
        
        Vest.OnVestDestroyed -= OnVestDestroyed;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Called when the vest armor value changes. Updates the armor bar UI.
    /// </summary>
    private void OnArmorChanged(float armorFraction) {
        if (armorFraction > 0f && !gameObject.activeSelf) {
            ShowArmorUI();
        }
        
        if (useSmoothTransition) {
            targetFillAmount = armorFraction;
            if (Mathf.Abs(armorBar.fillAmount - targetFillAmount) > 0.1f) {
                armorBar.fillAmount = armorFraction;
            }
        } else {
            armorBar.fillAmount = armorFraction;
        }
    }

    /// <summary>
    /// Smoothly interpolates the armor bar fill toward the target value each frame.
    /// </summary>
    private void UpdateArmorFill() {
        float currentFill = armorBar.fillAmount;
        float newFill = Mathf.Lerp(currentFill, targetFillAmount, Time.deltaTime * lerpSpeed);
        armorBar.fillAmount = newFill;
    }

    /// <summary>
    /// Sets the armor bar to a specific fill amount instantly.
    /// </summary>
    public void SetArmorInstant(float armorFraction) {
        float clampedFraction = Mathf.Clamp01(armorFraction);
        armorBar.fillAmount = clampedFraction;
        targetFillAmount = clampedFraction;
        armorBackground.fillAmount = 1f;
    }

    /// <summary>
    /// Called when the vest armor is fully depleted. Grays out the shield icon and hides the UI.
    /// </summary>
    private void OnArmorDepleted() {
        if (shieldIcon != null) {
            Color depletedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            shieldIcon.color = depletedColor;
        }
        HideArmorUI();
    }

    /// <summary>
    /// Called when the vest object is destroyed. Hides the armor UI.
    /// </summary>
    private void OnVestDestroyed() {
        HideArmorUI();
    }

    /// <summary>
    /// Shows the armor UI and resets the shield icon color.
    /// </summary>
    public void ShowArmorUI() {
        gameObject.SetActive(true);
        if (shieldIcon != null) {
            shieldIcon.color = Color.white;
        }
    }

    /// <summary>
    /// Hides the armor UI.
    /// </summary>
    public void HideArmorUI() {
        gameObject.SetActive(false);
    }

    public float GetCurrentFillAmount() {
        return armorBar.fillAmount;
    }

    public float GetTargetFillAmount() {
        return targetFillAmount;
    }

    #endregion
}
