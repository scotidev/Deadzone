using System.Collections;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

/// <summary>
/// Attach to the photo frame GameObject in the scene.
/// Detects when the player shoots it with a pistol and counts consecutive hits.
/// After 7 consecutive hits (without missing), activates the Penguin easter egg:
/// transforms all alive enemies into penguins, changes fog to blue,
/// plays a sound, and shows the "PENGUIN WAVE" announcement.
/// Only activates once per game.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EasterEggTarget : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Easter Egg Settings")]
    [SerializeField] private int requiredHits = 7;

    [Header("References")]
    [SerializeField] private GameObject penguinPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;

    #endregion

    #region FIELDS

    private int consecutiveHits = 0;
    private bool isActivated = false;
    private Collider targetCollider;
    private Coroutine missCheckCoroutine;

    #endregion

    #region UNITY

    private void Awake() {
        targetCollider = GetComponent<Collider>();
        if (targetCollider != null)
            targetCollider.isTrigger = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnEnable() {
        Weapon.OnWeaponFired += HandleWeaponFired;
    }

    private void OnDisable() {
        Weapon.OnWeaponFired -= HandleWeaponFired;
    }

    /// <summary>
    /// Detects projectile collisions. When hit by a bullet,
    /// registers the hit and checks if we've reached the required count.
    /// The projectile must have a Projectile component.
    /// </summary>
    private void OnCollisionEnter(Collision collision) {
        if (isActivated) return;

        // Only count hits from projectiles (bullets)
        if (collision.gameObject.GetComponent<Projectile>() == null) return;

        RegisterHit();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Called whenever the player fires a weapon.
    /// If it's the pistol, starts a miss timeout.
    /// If it's any other weapon, resets the counter.
    /// </summary>
    private void HandleWeaponFired(Weapon weapon) {
        if (isActivated) return;

        string weaponID = weapon.GetItemID();

        // Only the pistol (ID "1") counts
        if (weaponID != "1") {
            consecutiveHits = 0;
            CancelMissCheck();
            return;
        }

        // Cancel any previous miss check and start a new one
        CancelMissCheck();
        missCheckCoroutine = StartCoroutine(MissCheckRoutine());
    }

    /// <summary>
    /// Registers a hit on the painting.
    /// Increments the counter and cancels the miss timeout.
    /// If the required hits are reached, activates the easter egg.
    /// </summary>
    private void RegisterHit() {
        consecutiveHits++;
        CancelMissCheck();

        if (consecutiveHits >= requiredHits) {
            ActivateEasterEgg();
        }
    }

    /// <summary>
    /// Waits for a short window after a pistol shot.
    /// If the painting is not hit within this window, the shot is counted as a miss
    /// and the consecutive hit counter resets.
    /// The delay (0.3s) is long enough for the projectile to travel any distance
    /// in the scene at its speed of 400 units/s.
    /// </summary>
    private IEnumerator MissCheckRoutine() {
        // Wait for the projectile to travel and potentially collide
        yield return new WaitForSeconds(0.3f);

        // If we reach here without being interrupted by RegisterHit(),
        // the shot missed the painting
        consecutiveHits = 0;
        missCheckCoroutine = null;
    }

    /// <summary>
    /// Cancels the current miss timeout if one is running.
    /// </summary>
    private void CancelMissCheck() {
        if (missCheckCoroutine != null) {
            StopCoroutine(missCheckCoroutine);
            missCheckCoroutine = null;
        }
    }

    /// <summary>
    /// Activates the Penguin Easter egg:
    /// 1. Marks PenguinMode as active
    /// 2. Changes fog to blue
    /// 3. Plays activation sound
    /// 4. Transforms all alive enemies into penguins
    /// 5. Shows the "PENGUIN WAVE" announcement
    /// 6. Disables the painting's collider to prevent re-activation
    /// </summary>
    private void ActivateEasterEgg() {
        if (isActivated) return;
        isActivated = true;

        int currentWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;
        PenguinMode.Activate(currentWave);

        // Change fog color (configured in WaveManager Inspector)
        FogController fog = FindObjectOfType<FogController>();
        if (fog != null) {
            Color fogColor = WaveManager.Instance != null
                ? WaveManager.Instance.PenguinWaveFogColor
                : Color.blue;
            fog.SetFogColor(fogColor);
        }

        // Play activation sound
        if (activationSound != null) {
            AudioSource.PlayClipAtPoint(activationSound, transform.position, 1f);
        }

        // Transform all alive enemies into penguins
        TransformAllEnemiesToPenguins();

        // Show the announcement
        WaveUI waveUI = FindObjectOfType<WaveUI>();
        if (waveUI != null) {
            waveUI.ShowPenguinWaveAnnouncement();
        }

        // Disable the collider so this can't be triggered again
        if (targetCollider != null) {
            targetCollider.enabled = false;
        }
    }

    /// <summary>
    /// Finds all active enemies in the scene, reads their reward value,
    /// destroys them, and spawns a penguin in their place with the same reward.
    /// </summary>
    private void TransformAllEnemiesToPenguins() {
        if (penguinPrefab == null) {
            Debug.LogWarning("[EasterEggTarget] No penguinPrefab assigned! Cannot transform enemies.");
            return;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies) {
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase == null) continue;

            // Skip tutorial enemies
            if (enemyBase.IsTutorialEnemy) continue;

            int reward = enemyBase.RewardCurrency;
            Vector3 position = enemy.transform.position;
            Quaternion rotation = enemy.transform.rotation;
            Transform parent = enemy.transform.parent;

            Destroy(enemy);

            GameObject penguin = Instantiate(penguinPrefab, position, rotation, parent);
            PenguinEnemy penguinScript = penguin.GetComponent<PenguinEnemy>();
            if (penguinScript != null) {
                penguinScript.SetReward(reward);
            }
        }

        Debug.Log($"[EasterEggTarget] Transformed all alive enemies into penguins!");
    }

    #endregion
}
