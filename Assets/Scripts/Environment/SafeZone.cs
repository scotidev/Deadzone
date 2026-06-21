using UnityEngine;

/// <summary>
/// Trigger that defines the safe area of the house.
/// Poison damage logic is only active after the tutorial ends (when isPoisonEnabled = true).
/// </summary>
public class SafeZone : MonoBehaviour {

    #region FIELDS

    private PlayerHealth health;
    private bool isPoisonEnabled = false;

    #endregion

    #region UNITY

    private void Start() {
        if (PlayerCache.Transform != null) {
            health = PlayerCache.GameObject.GetComponentInParent<PlayerHealth>();
        }

        RegisterWithFogTrigger();
    }

    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        if (!isPoisonEnabled) return;

        if (health != null) {
            health.StopPoisonDamage();
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!other.CompareTag("Player")) return;

        if (!isPoisonEnabled) return;

        if (health != null) {
            if (!IsPlayerInsideAnySafeZone(other)) {
                health.StartPoisonDamage();
            }
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Checks if the player is currently inside any SafeZone other than this one.
    /// </summary>
    private bool IsPlayerInsideAnySafeZone(Collider playerCollider) {
        SafeZone[] allSafeZones = FindObjectsByType<SafeZone>(FindObjectsSortMode.None);
        foreach (SafeZone safeZone in allSafeZones) {
            if (safeZone == this) continue;

            BoxCollider safeZoneBox = safeZone.GetComponent<BoxCollider>();
            if (safeZoneBox != null && safeZoneBox.isTrigger) {
                if (safeZoneBox.bounds.Intersects(playerCollider.bounds)) {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Enables the poison system for this SafeZone.
    /// </summary>
    public void EnablePoisonSystem() {
        isPoisonEnabled = true;
    }

    /// <summary>
    /// Registers this SafeZone's BoxCollider with the Fog ParticleSystem Trigger Module.
    /// </summary>
    private void RegisterWithFogTrigger() {
        GameObject fogObj = GameObject.Find("Fog");
        if (fogObj == null) return;

        ParticleSystem fogParticles = fogObj.GetComponent<ParticleSystem>();
        if (fogParticles == null) return;

        var trigger = fogParticles.trigger;
        trigger.enabled = true;
        trigger.radiusScale = 0.3f;

        trigger.inside = ParticleSystemOverlapAction.Kill;
        trigger.enter = ParticleSystemOverlapAction.Kill;

        BoxCollider myCollider = GetComponent<BoxCollider>();
        if (myCollider == null) return;

        trigger.AddCollider(myCollider);
    }

    #endregion

    #region DEBUG

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
