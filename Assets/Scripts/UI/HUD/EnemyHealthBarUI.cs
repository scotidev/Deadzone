using UnityEngine;
using UnityEngine.UI;

// refatoração: Esse script deveria implementar Element.cs

/// <summary>
/// Manages a single reusable health bar that displays above the currently targeted enemy.
/// Shows only when the player's aim is on an enemy.
/// </summary>
public class EnemyHealthBarUI : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("UI References")]
    [Tooltip("The Image component that represents current health (green bar).")]
    [SerializeField] private Image healthFillImage;

    [Tooltip("CanvasGroup for controlling fade in/out alpha.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [Tooltip("Speed of health bar fill animation.")]
    [SerializeField] private float fillSpeed = 10f;

    [Tooltip("Speed of position follow smoothing.")]
    [SerializeField] private float positionSmoothSpeed = 15f;

    [Header("Positioning")]
    [Tooltip("Extra offset in screen space pixels above enemy head.")]
    [SerializeField] private float screenOffsetY = 20f;

    #endregion

    #region FIELDS

    private Camera mainCamera;
    private RectTransform rectTransform;
    private EnemyBase currentTargetEnemy;

    #endregion


    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        if (healthFillImage == null) {
            Debug.LogError("[EnemyHealthBarUI] HealthFillImage reference is missing!");
        }

        if (canvasGroup == null) {
            Debug.LogError("[EnemyHealthBarUI] CanvasGroup reference is missing!");
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void Update() {
        UpdatePosition();
        UpdateHealthFill();
    }

    /// <summary>
    /// Sets the enemy to display health for. Pass null to hide the bar.
    /// </summary>
    public void SetTargetEnemy(EnemyBase enemy) {
        currentTargetEnemy = enemy;

        if (currentTargetEnemy != null) {
            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            healthFillImage.fillAmount = currentTargetEnemy.GetHealthFraction();
        } else {
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the screen position to follow the enemy's world position.
    /// Calculates position based on enemy's top bound (head position).
    /// </summary>
    private void UpdatePosition() {
        if (currentTargetEnemy == null || mainCamera == null) return;

        Vector3 worldPos = GetEnemyHeadPosition();
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z > 0) {
            Vector2 targetPosition = new Vector2(screenPos.x, screenPos.y + screenOffsetY);
            rectTransform.position = Vector2.Lerp(rectTransform.position, targetPosition, Time.deltaTime * positionSmoothSpeed);
        } else {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gets the world position of the enemy's head (top of collider bounds).
    /// </summary>
    private Vector3 GetEnemyHeadPosition() {
        Renderer renderer = currentTargetEnemy.GetComponentInChildren<Renderer>();
        if (renderer != null) {
            return new Vector3(
                currentTargetEnemy.transform.position.x,
                renderer.bounds.max.y,
                currentTargetEnemy.transform.position.z
            );
        }

        Collider collider = currentTargetEnemy.GetComponent<Collider>();
        if (collider != null) {
            return new Vector3(
                currentTargetEnemy.transform.position.x,
                collider.bounds.max.y,
                currentTargetEnemy.transform.position.z
            );
        }

        return currentTargetEnemy.transform.position + Vector3.up * 2f;
    }

    /// <summary>
    /// Updates the health bar fill amount based on the enemy's current health.
    /// </summary>
    private void UpdateHealthFill() {
        if (currentTargetEnemy == null) return;

        float targetFillAmount = currentTargetEnemy.GetHealthFraction();
        healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
    }

    /// <summary>
    /// Returns true if the bar is currently visible (alpha > threshold).
    /// </summary>
    public bool IsVisible() {
        return canvasGroup.alpha > 0.1f;
    }
}
