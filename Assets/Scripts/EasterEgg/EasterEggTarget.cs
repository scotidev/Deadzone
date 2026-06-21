using System.Collections;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

/// <summary>
/// Attach to the photo frame GameObject. Detects consecutive pistol shots and
/// after the required number of hits without missing, activates the Penguin easter egg:
/// transforms all alive enemies into penguins, changes fog to blue,
/// plays a sound, and shows the "PENGUIN WAVE" announcement. Only activates once per game.
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
    private IAudioManagerService audioService;

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

        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
    }

    private void OnEnable() {
        Weapon.OnWeaponFired += HandleWeaponFired;
    }

    private void OnDisable() {
        Weapon.OnWeaponFired -= HandleWeaponFired;
    }

    /// <summary>
    /// Detects projectile collisions. When hit by a bullet, registers the hit and
    /// checks if the required count has been reached. The projectile must have a Projectile component.
    /// </summary>
    private void OnCollisionEnter(Collision collision) {
        if (isActivated) return;

        if (collision.gameObject.GetComponent<Projectile>() == null) return;

        RegisterHit();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Called whenever the player fires a weapon. Resets the counter if a non-pistol is fired,
    /// or starts a miss timeout if the pistol is fired.
    /// </summary>
    private void HandleWeaponFired(Weapon weapon) {
        if (isActivated) return;

        string weaponID = weapon.GetItemID();

        if (weaponID != "1") {
            consecutiveHits = 0;
            CancelMissCheck();
            return;
        }

        CancelMissCheck();
        missCheckCoroutine = StartCoroutine(MissCheckRoutine());
    }

    /// <summary>
    /// Increments the hit counter and cancels the miss timeout. Activates the easter egg
    /// when the required hits are reached.
    /// </summary>
    private void RegisterHit() {
        consecutiveHits++;
        CancelMissCheck();

        if (consecutiveHits >= requiredHits) {
            ActivateEasterEgg();
        }
    }

    /// <summary>
    /// Waits a short window after a pistol shot. If the painting is not hit within that time,
    /// the shot counts as a miss and the consecutive hit counter resets.
    /// </summary>
    private IEnumerator MissCheckRoutine() {
        yield return new WaitForSeconds(0.3f);

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
    /// Activates the Penguin Easter egg: marks PenguinMode, changes fog to blue,
    /// plays the activation sound, transforms all alive enemies into penguins,
    /// shows the announcement, and disables this collider.
    /// </summary>
    private void ActivateEasterEgg() {
        if (isActivated) return;
        isActivated = true;

        int currentWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;
        PenguinMode.Activate(currentWave);

        FogController fog = FindFirstObjectByType<FogController>();
        if (fog != null) {
            Color fogColor = WaveManager.Instance != null
                ? WaveManager.Instance.PenguinWaveFogColor
                : Color.blue;
            fog.SetFogColor(fogColor);
        }

        if (activationSound != null) {
            audioService?.PlaySFX3D(activationSound, transform.position, 1f);
        }

        TransformAllEnemiesToPenguins();

        WaveUI waveUI = FindFirstObjectByType<WaveUI>();
        if (waveUI != null) {
            waveUI.ShowPenguinWaveAnnouncement();
        }

        if (targetCollider != null) {
            targetCollider.enabled = false;
        }
    }

    /// <summary>
    /// Finds all active enemies, reads their reward values, destroys them,
    /// and spawns a penguin in their place with the same reward.
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
    }

    #endregion
}
