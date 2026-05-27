using UnityEngine;

/// <summary>
/// Simple proxy script to be placed ON the Cube triggers.
/// It forwards the collision event to the central Teleport manager.
/// </summary>
public class TeleportTrigger : MonoBehaviour {
    
    private void OnTriggerEnter(Collider other) {
        if (Teleport.Instance != null) {
            // Envia o próprio GameObject para o manager comparar
            Teleport.Instance.NotifyTriggerEnter(gameObject, other);
        }
    }
}
