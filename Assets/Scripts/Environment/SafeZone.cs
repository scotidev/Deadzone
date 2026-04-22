using UnityEngine;

/// <summary>
/// Trigger that defines the safe area of the house.
/// </summary>
public class SafeZone : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Settings")]
    [SerializeField] private bool playerStartsOutside = false;

    #endregion

    #region FIELDS

    private PlayerHealth health;

    #endregion

    #region UNITY

    private void Start() {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) {
            health = player.GetComponentInParent<PlayerHealth>();
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

    #endregion
}
