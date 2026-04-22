using UnityEngine;

// REFATORAÇÃO: as cores na verdade vão ser texturas, ou pinturas diferentes, precisamos de rachaduras progressivas.

/// <summary>
/// Represents a barricade that blocks enemy path to the player.
/// </summary>
public class Barricade : MonoBehaviour, IDamageable {

    #region SERIALIZED FIELDS

    [Header("Barricade Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer barricadeRenderer;
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color redColor = Color.red;

    #endregion

    #region FIELDS

    private Color currentColor;

    #endregion

    #region PROPERTIES

    public float HealthFraction => currentHealth / maxHealth;
    public bool IsDestroyed => currentHealth <= 0f;

    #endregion

    #region UNITY

    private void Awake() {
        currentHealth = maxHealth;

        if (barricadeRenderer == null)
            barricadeRenderer = GetComponent<Renderer>();
    }

    private void Start() {
        UpdateBarricadeColor();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Initializes the barricade's health and visual state. Should be called after instantiating the barricade.
    /// </summary>
    /// <param name="health"></param>
    public void Initialize(float health) {
        maxHealth = health;
        currentHealth = maxHealth;
        UpdateBarricadeColor();
    }

    /// <summary>
    /// Reduces the barricade's current health by the specified amount and triggers destruction if health reaches zero.
    /// </summary>
    /// <remarks>If the barricade's health is already zero or less, this method has no effect. When health
    /// drops to zero or below, the barricade is destroyed.</remarks>
    /// <param name="amount">The amount of damage to apply to the barricade. Must be a non-negative value.</param>
    public void TakeDamage(float amount) {
        if (currentHealth <= 0f) return;

        currentHealth -= amount;
        UpdateBarricadeColor();

        if (currentHealth <= 0f) {
            DestroyBarricade();
        }
    }

    /// <summary>
    /// Updates the barricade's color based on its current health percentage.
    /// </summary>
    private void UpdateBarricadeColor() {
        if (barricadeRenderer == null) return;

        float healthPercent = currentHealth / maxHealth;

        if (healthPercent > 0.66f) {
            currentColor = greenColor;
        }
        else if (healthPercent > 0.33f) {
            currentColor = yellowColor;
        }
        else {
            currentColor = redColor;
        }

        barricadeRenderer.material.color = currentColor;
    }

    /// <summary>
    /// Destroys the barricade object and removes it from the scene.
    /// </summary>
    private void DestroyBarricade() {
        Destroy(gameObject);
    }

    #endregion
}