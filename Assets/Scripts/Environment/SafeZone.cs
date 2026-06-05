using UnityEngine;

/// <summary>
/// Trigger that defines the safe area of the house.
/// Poison damage logic is only active after the tutorial ends (when isPoisonEnabled = true).
/// </summary>
public class SafeZone : MonoBehaviour {

    #region FIELDS

    private PlayerHealth health;
    // Flag that controls whether this SafeZone should react to poison damage.
    // During tutorial, this is false. After TutorialEndTrigger, this becomes true.
    private bool isPoisonEnabled = false;

    #endregion

    #region UNITY

    private void Start() {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) {
            health = player.GetComponentInParent<PlayerHealth>();
        }

        // Register this SafeZone's collider with the Fog ParticleSystem Trigger Module.
        // This automatically kills any fog particle that enters or is inside the safe zone.
        RegisterWithFogTrigger();
    }

    /// <summary>
    /// Called when a Collider enters this area.
    /// If it's the Player and poison is enabled, stops the poison damage.
    /// </summary>
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        // Only activate poison logic if poison system has been enabled.
        if (!isPoisonEnabled) return;

        if (health != null) {
            health.StopPoisonDamage();
        }
    }

    /// <summary>
    /// Called when a Collider exits this area.
    /// If it's the Player and poison is enabled, checks if player is still inside another SafeZone.
    /// Only starts poison damage if player is truly outside ALL SafeZones.
    /// </summary>
    private void OnTriggerExit(Collider other) {
        if (!other.CompareTag("Player")) return;

        // Only activate poison logic if poison system has been enabled.
        if (!isPoisonEnabled) return;

        if (health != null) {
            // Before starting poison damage, check if player is inside another SafeZone.
            // This handles overlapping SafeZones correctly.
            if (!IsPlayerInsideAnySafeZone(other)) {
                health.StartPoisonDamage();
            }
        }
    }

    /// <summary>
    /// Checks if the player (via collider) is currently inside ANY SafeZone (except this one).
    /// Used to handle overlapping SafeZones correctly.
    /// </summary>
    private bool IsPlayerInsideAnySafeZone(Collider playerCollider) {
        SafeZone[] allSafeZones = FindObjectsByType<SafeZone>(FindObjectsSortMode.None);
        foreach (SafeZone safeZone in allSafeZones) {
            if (safeZone == this) continue; // Skip self

            BoxCollider safeZoneBox = safeZone.GetComponent<BoxCollider>();
            if (safeZoneBox != null && safeZoneBox.isTrigger) {
                // Check if player bounds overlap with this SafeZone's bounds.
                if (safeZoneBox.bounds.Intersects(playerCollider.bounds)) {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Enables the poison system for this SafeZone.
    /// Called by TutorialEndTrigger when the tutorial ends.
    /// </summary>
    public void EnablePoisonSystem() {
        isPoisonEnabled = true;
    }

    /// <summary>
    /// Registers this SafeZone's BoxCollider with the Fog ParticleSystem Trigger Module.
    /// Particles that enter or are inside this collider are automatically killed by the ParticleSystem.
    /// This prevents fog from appearing inside safe zones without affecting emission rates.
    /// </summary>
    private void RegisterWithFogTrigger() {
        GameObject fogObj = GameObject.Find("Fog");
        if (fogObj == null) return;

        ParticleSystem fogParticles = fogObj.GetComponent<ParticleSystem>();
        if (fogParticles == null) return;

        var trigger = fogParticles.trigger;
        trigger.enabled = true;
        trigger.radiusScale = 0.3f;

        // Set global overlap actions: kill particles entering or inside ANY assigned collider
        trigger.inside = ParticleSystemOverlapAction.Kill;
        trigger.enter = ParticleSystemOverlapAction.Kill;

        BoxCollider myCollider = GetComponent<BoxCollider>();
        if (myCollider == null) return;

        trigger.AddCollider(myCollider);
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
