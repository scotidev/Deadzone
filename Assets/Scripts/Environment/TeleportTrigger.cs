using UnityEngine;
using System.Collections;

/// <summary>
/// Simple proxy script to be placed ON the Cube triggers.
/// It forwards the collision event to the central Teleport manager.
/// </summary>
public class TeleportTrigger : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Visual Effects")]
    [Tooltip("VFX object to enable upon teleportation (usually a child).")]
    [SerializeField] private GameObject teleportVFX;
    [Tooltip("How long the VFX stays active before turning off.")]
    [SerializeField] private float vfxDuration = 2f;

    #endregion

    #region UNITY

    private void OnTriggerEnter(Collider other) {
        if (Teleport.Instance != null) {
            Teleport.Instance.NotifyTriggerEnter(gameObject, other);
        }
    }

    #endregion

    #region METHODS

    public void PlayVFX() {
        if (teleportVFX == null) return;

        StopAllCoroutines();
        StartCoroutine(VFXRoutine());
    }

    private IEnumerator VFXRoutine() {
        teleportVFX.SetActive(true);
        yield return new WaitForSeconds(vfxDuration);
        teleportVFX.SetActive(false);
    }

    #endregion
}
