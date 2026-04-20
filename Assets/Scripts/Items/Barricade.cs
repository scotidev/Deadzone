using UnityEngine;

/// <summary>
/// Represents a barricade that blocks enemy path to the player.
/// Changes color based on health: Green (100-66%), Yellow (66-33%), Red (33-1%).
/// </summary>
public class Barricade : MonoBehaviour, IDamageable {

    [Header("Barricade Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer barricadeRenderer;
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color redColor = Color.red;

    private Color currentColor;

    public float HealthFraction => currentHealth / maxHealth;
    public bool IsDestroyed => currentHealth <= 0f;

    private void Awake() {
        currentHealth = maxHealth;
        
        if (barricadeRenderer == null)
            barricadeRenderer = GetComponent<Renderer>();
    }

    private void Start() {
        UpdateBarricadeColor();
    }

    public void Initialize(float health) {
        maxHealth = health;
        currentHealth = maxHealth;
        UpdateBarricadeColor();
    }

    public void TakeDamage(float amount) {
        if (currentHealth <= 0f) return;

        currentHealth -= amount;
        UpdateBarricadeColor();

        if (currentHealth <= 0f) {
            DestroyBarricade();
        }
    }

    private void UpdateBarricadeColor() {
        if (barricadeRenderer == null) return;

        float healthPercent = currentHealth / maxHealth;

        if (healthPercent > 0.66f) {
            currentColor = greenColor;
        } else if (healthPercent > 0.33f) {
            currentColor = yellowColor;
        } else {
            currentColor = redColor;
        }

        barricadeRenderer.material.color = currentColor;
    }

    private void DestroyBarricade() {
        Destroy(gameObject);
    }
}