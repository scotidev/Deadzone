using UnityEngine;
using UnityEngine.UI;

// REFATORAÇÃO: esse script deveria herdar de Element.cs? ANALISE NECESSARIA.
//REFATORAÇÃO: esse script poderia ser unido ao script PlayerHealthUI, já que ambos são barras de status? ANALISE NECESSARIA.
// REFATORAÇÃO: quando a armadura acabar o objeto UI pode ser destruído ou desativado simplismente, até que o colete seja novamente comprado na loja.

/// <summary>
/// Manages the player armor bar UI. Subscribes to PlayerArmor events and updates
/// the blue-gray bar fill amount in real-time when the player takes damage or adds armor.
/// Works similarly to PlayerHealthUI but displays armor instead of health.
/// </summary>
public class PlayerArmorUI : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Armor References")]
    [SerializeField] private PlayerArmor playerArmor;
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
        playerArmor.OnArmorChanged += OnArmorChanged;
        playerArmor.OnArmorDepleted += OnArmorDepleted;

        armorBar.fillAmount = 1f;
        armorBackground.fillAmount = 1f;
        targetFillAmount = 1f;
    }

    private void Start() {
        if (!hasInitialized) {
            float initialArmor = playerArmor.GetArmorFraction();
            SetArmorInstant(initialArmor);
            hasInitialized = true;
        }
    }

    private void Update() {
        if (!useSmoothTransition) return;

        UpdateArmorFill();
    }

    private void OnDestroy() {
        if (playerArmor != null) {
            playerArmor.OnArmorChanged -= OnArmorChanged;
            playerArmor.OnArmorDepleted -= OnArmorDepleted;
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Called whenever PlayerArmor.OnArmorChanged is invoked.
    /// This method receives the armor fraction (0.0 to 1.0) and updates the bar accordingly.
    /// </summary>
    private void OnArmorChanged(float armorFraction) {
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
    /// Smoothly updates the armor bar fill amount toward the target value.
    /// This method is called every frame only when useSmoothTransition is enabled.
    /// It uses Mathf.Lerp to create a smooth animation between the current and target values.
    /// </summary>
    private void UpdateArmorFill() {
        float currentFill = armorBar.fillAmount;

        float newFill = Mathf.Lerp(currentFill, targetFillAmount, Time.deltaTime * lerpSpeed);

        armorBar.fillAmount = newFill;
    }

    /// <summary>
    /// Sets the armor bar to a specific fill amount instantly, bypassing any lerping.
    /// Useful for initialization at startup or when you need immediate visual feedback.
    /// </summary>
    public void SetArmorInstant(float armorFraction) {
        float clampedFraction = Mathf.Clamp01(armorFraction);

        armorBar.fillAmount = clampedFraction;
        targetFillAmount = clampedFraction;

        armorBackground.fillAmount = 1f;
    }

    /// <summary>
    /// Called when the armor is completely depleted. Can be extended to add visual effects
    /// like fading, color changes, or special animations.
    /// </summary>
    private void OnArmorDepleted() {
        if (shieldIcon != null) {
            Color depletedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            shieldIcon.color = depletedColor;
        }
    }

    /// <summary>
    /// Public method to get the current armor fill amount (0.0 to 1.0).
    /// Useful for other systems that need to know the armor bar state.
    /// </summary>
    public float GetCurrentFillAmount() {
        return armorBar.fillAmount;
    }

    /// <summary>
    /// Public method to get the target fill amount (0.0 to 1.0).
    /// Useful for debugging or checking where the armor bar is animating toward.
    /// </summary>
    public float GetTargetFillAmount() {
        return targetFillAmount;
    }

    #endregion
}
