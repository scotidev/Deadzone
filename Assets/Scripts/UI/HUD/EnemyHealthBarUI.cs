using UnityEngine;
using UnityEngine.UI;

// refatoração: Esse script deveria implementar Element.cs?

/// <summary>
/// Manages a single reusable health bar that displays above the currently targeted enemy.
/// Shows only when the player's aim is on an enemy.
/// </summary>
public class EnemyHealthBarUI : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("UI References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float fillSpeed = 10f;
    [SerializeField] private float positionSmoothSpeed = 15f;

    [Header("Positioning")]
    [SerializeField] private float screenOffsetY = 20f;

    #endregion

    #region FIELDS

    private Camera mainCamera;
    private RectTransform rectTransform;
    private EnemyBase currentTargetEnemy;

    // Cache do renderer e collider do inimigo para evitar GetComponentInChildren toda vez
    // CONCEITO: Guardamos o Renderer e Collider do inimigo quando ele é definido como alvo.
    // Assim, não precisamos chamar GetComponentInChildren (que sobe/desce a hierarquia)
    // a cada frame durante o UpdatePosition().
    private Renderer cachedEnemyRenderer;
    private Collider cachedEnemyCollider;

    #endregion

    #region UNITY

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

    #endregion

    #region METHODS

    /// <summary>
    /// Sets the enemy to display health for. Pass null to hide the bar.
    /// </summary>
    public void SetTargetEnemy(EnemyBase enemy) {
        currentTargetEnemy = enemy;

        if (currentTargetEnemy != null) {
            // CONCEITO: Cache do Renderer e Collider quando o alvo é definido.
            // Isso elimina a necessidade de GetComponentInChildren / GetComponent
            // no Update() a cada frame — uma busca que percorre toda a hierarquia.
            cachedEnemyRenderer = currentTargetEnemy.GetComponentInChildren<Renderer>();
            cachedEnemyCollider = currentTargetEnemy.GetComponent<Collider>();

            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            healthFillImage.fillAmount = currentTargetEnemy.GetHealthFraction();
        } else {
            cachedEnemyRenderer = null;
            cachedEnemyCollider = null;
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
    /// Uses cached Renderer/Collider references instead of GetComponentInChildren every frame.
    /// CONCEITO: Como já cacheamos Renderer e Collider no SetTargetEnemy(),
    /// este método só acessa as referências guardadas — sem percorrer hierarquia.
    /// </summary>
    private Vector3 GetEnemyHeadPosition() {
        // CONCEITO: Usa o Renderer cacheado em vez de GetComponentInChildren.
        if (cachedEnemyRenderer != null)
        {
            return new Vector3(
                currentTargetEnemy.transform.position.x,
                cachedEnemyRenderer.bounds.max.y,
                currentTargetEnemy.transform.position.z
            );
        }

        // CONCEITO: Fallback usando Collider cacheado.
        if (cachedEnemyCollider != null)
        {
            return new Vector3(
                currentTargetEnemy.transform.position.x,
                cachedEnemyCollider.bounds.max.y,
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

    #endregion
}
