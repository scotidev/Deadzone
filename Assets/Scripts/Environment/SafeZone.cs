using UnityEngine;

/// <summary>
/// Trigger that defines the safe area of the house.
/// </summary>
public class SafeZone : MonoBehaviour {

    [Header("Settings")]
    [Tooltip("Check TRUE if the player starts outside the safezone at the beginning of the scene.")]
    [SerializeField] private bool playerStartsOutside = false;

    private PlayerHealth health;

    private void Start() {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) {
            health = player.GetComponentInParent<PlayerHealth>();
        }
        else {
            Debug.LogWarning("[SafeZone] Player not found in the scene!");
        }

        if (playerStartsOutside && health != null) {
            health.StartPoisonDamage();
        }
    }

    /// <summary>
    /// Called when a Collider enters this area.
    /// If it's the Player, stops the poison damage.
    /// </summary>
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        if (health != null) {
            health.StopPoisonDamage();
        }
        else {
            Debug.LogWarning("[SafeZone] Player entered safezone but health not found.");
        }
    }

    /// <summary>
    /// Called when a Collider exits this area.
    /// If it's the Player, starts the poison damage.
    /// </summary>
    private void OnTriggerExit(Collider other) {
        if (!other.CompareTag("Player")) return;

        if (health != null) {
            health.StartPoisonDamage();
        }
        else {
            Debug.LogWarning("[SafeZone] Player left the safezone but health not found.");
        }
    }

    private void OnDrawGizmos() {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = new Color(0f, 0f, 1f, 0.6f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
