using UnityEngine;

/// <summary>
/// Initializes a Barricade with health from BuildableDataSO.
/// Must be attached to the barricade prefab that gets instantiated.
/// </summary>
public class BarricadeInitializer : MonoBehaviour {

    private Barricade barricade;
    private bool initialized = false;

    private void Awake() {
        barricade = GetComponent<Barricade>();
    }

    private void Start() {
        if (!initialized) {
            InitializeWithDefaultHealth();
        }
    }

    public void Initialize(float health) {
        if (barricade != null) {
            barricade.Initialize(health);
            initialized = true;
        }
    }

    private void InitializeWithDefaultHealth() {
        if (barricade != null) {
            barricade.Initialize(100f);
            initialized = true;
        }
    }
}