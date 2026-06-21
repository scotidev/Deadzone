using UnityEngine;
using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Central manager for teleportation logic. 
/// Put this on a 'TeleportManager' object in your scene.
/// </summary>
public class Teleport : MonoBehaviour {

    #region STATIC

    public static Teleport Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Point A (House)")]
    [Tooltip("The trigger GameObject located in the House.")]
    public GameObject triggerA;
    [Tooltip("Where the player arrives when coming FROM the Shop.")]
    public Transform arrivalPointA;

    [Header("Point B (Shop)")]
    [Tooltip("The trigger GameObject located in the Shop.")]
    public GameObject triggerB;
    [Tooltip("Where the player arrives when coming FROM the House.")]
    public Transform arrivalPointB;

    [Header("Audio")]
    [SerializeField] private AudioClip teleportClip;
    [Range(0f, 1f)]
    [SerializeField] private float teleportVolume = 1f;

    [Header("Settings")]
    [SerializeField] private float cooldownTime = 1.5f;
    [SerializeField] private LayerMask characterLayer;

    #endregion

    #region FIELDS

    private IAudioManagerService audioService;
    private float lastTeleportTime = -10f;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
    }

    private void OnEnable() {
        SubscribeToWaveEvents();

        SyncWithWaveState();
    }

    private void Start() {
        SubscribeToWaveEvents();
        SyncWithWaveState();
    }

    private void OnDisable() {
        UnsubscribeFromWaveEvents();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Checks the current state of the WaveManager and toggles teleports accordingly.
    /// </summary>
    private void SyncWithWaveState() {
        if (WaveManager.Instance != null) {
            if (WaveManager.Instance.IsWaveActive) {
                HandleWaveStarted();
            } else {
                HandleWaveCompleted();
            }
        }
    }

    /// <summary>
    /// Subscribes to WaveManager wave lifecycle events.
    /// </summary>
    private void SubscribeToWaveEvents() {
        if (WaveManager.Instance != null) {
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
            WaveManager.Instance.OnWaveCompleted -= HandleWaveCompleted;

            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
            WaveManager.Instance.OnWaveCompleted += HandleWaveCompleted;
        }
    }

    /// <summary>
    /// Unsubscribes from WaveManager wave lifecycle events.
    /// </summary>
    private void UnsubscribeFromWaveEvents() {
        if (WaveManager.Instance != null) {
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
            WaveManager.Instance.OnWaveCompleted -= HandleWaveCompleted;
        }
    }

    /// <summary>
    /// Handles the wave started event by disabling teleports.
    /// </summary>
    private void HandleWaveStarted() {
        ToggleTeleports(false);
    }

    /// <summary>
    /// Handles the wave completed event by enabling teleports if past wave 1.
    /// </summary>
    private void HandleWaveCompleted() {
        if (WaveManager.Instance != null && WaveManager.Instance.CurrentWave >= 1) {
            ToggleTeleports(true);
        } else {
            ToggleTeleports(false);
        }
    }

    /// <summary>
    /// Enables or disables teleport trigger GameObjects.
    /// </summary>
    private void ToggleTeleports(bool state) {
        if (triggerA != null) {
            triggerA.SetActive(state);
        }
        if (triggerB != null) {
            triggerB.SetActive(state);
        }
    }

    /// <summary>
    /// Called by TeleportTrigger components when the player enters them.
    /// </summary>
    public void NotifyTriggerEnter(GameObject triggerHit, Collider playerCollider) {
        if (Time.time < lastTeleportTime + cooldownTime) return;

        if (!playerCollider.CompareTag("Player") && ((1 << playerCollider.gameObject.layer) & characterLayer) == 0) return;

        if (triggerHit == triggerA) {
            PlayTriggerVFX(triggerA);
            ExecuteTeleport(playerCollider.transform, arrivalPointB);
        } else if (triggerHit == triggerB) {
            PlayTriggerVFX(triggerB);
            ExecuteTeleport(playerCollider.transform, arrivalPointA);
        }
    }

    /// <summary>
    /// Finds the TeleportTrigger component and plays its VFX.
    /// </summary>
    private void PlayTriggerVFX(GameObject triggerObj) {
        if (triggerObj == null) return;

        TeleportTrigger triggerScript = triggerObj.GetComponent<TeleportTrigger>();
        if (triggerScript != null) {
            triggerScript.PlayVFX();
        }
    }

    /// <summary>
    /// Executes the teleport by moving the player and resetting physics.
    /// </summary>
    private void ExecuteTeleport(Transform player, Transform destination) {
        if (destination == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.position = destination.position;
        lastTeleportTime = Time.time;

        if (teleportClip != null) {
            audioService?.PlaySFX2D(teleportClip, teleportVolume);
        }
    }

    #endregion
}
