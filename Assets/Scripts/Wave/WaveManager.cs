using InfimaGames.LowPolyShooterPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager responsible for the entire wave lifecycle.
/// </summary>
public class WaveManager : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="WaveManager"/> instance.</summary>
    public static WaveManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Enemy Types")]

    [SerializeField] private List<EnemySpawnConfig> enemyTypes;

    [Header("Wave Start SFX")]

    [SerializeField] private AudioClip lightWaveClip;
    [SerializeField] private AudioClip mediumWaveClip;
    [SerializeField] private AudioClip hardWaveClip;
    [SerializeField] private AudioClip bossWaveClip;

    [SerializeField] private int lastLightWave = 3;
    [SerializeField] private int lastMediumWave = 7;

    [Header("Wave Clear SFX")]
    [SerializeField] private AudioClip waveClearClip;

    [Header("Wave")]

    [Tooltip("Growth rate from wave 1 to 2.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float initialGrowthRate = 0.25f;

    [Tooltip("How much the rate decreases each wave.")]
    [Range(0f, 0.1f)]
    [SerializeField] private float growthDecrement = 0.02f;

    [Tooltip("Minimum growth rate.")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float minGrowthRate = 0.05f;

    [SerializeField] private int maxEnemiesPerWave = 500;
    [SerializeField] private int maxEnemiesAliveAtOnce = 15;

    [Header("Spawners")]

    [SerializeField] private List<EnemySpawner> spawners;

    [Header("HUD")]
    [SerializeField] private WaveUI waveUI;

    [Header("Music Settings")]
    [SerializeField] private AudioClip ambientBGM;
    [Range(0f, 1f)]
    [SerializeField] private float ambientBGMVolume = 1f;
    [SerializeField] private AudioClip combatBGM;
    [Range(0f, 1f)]
    [SerializeField] private float combatBGMVolume = 1f;

    #endregion

    #region FIELDS

    private int currentWave = 0;
    private int totalEnemiesForWave = 0;
    private int enemiesSpawned = 0;
    private int enemiesKilled = 0;
    private int lastWaveEnemyCount = 5;
    private bool isWaveActive = false;

    private List<EnemySpawnConfig> currentWaveEnemyTypes;
    private IAudioManagerService audioService;

    #endregion

    #region PROPERTIES
    public bool IsWaveActive => isWaveActive;
    public int CurrentWave => currentWave;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
    }

    private void Start() {
        audioService?.PlayBGM(ambientBGM, true, 1.5f, ambientBGMVolume);
    }

    private void OnEnable() {
        EnemyBase.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable() {
        EnemyBase.OnAnyEnemyDied -= HandleEnemyDied;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Starts the next enemy wave.
    /// Calls WaveButton.Interact() when the player presses E.
    /// Blocks if wave is active.
    /// </summary>
    public void StartNextWave() {
        if (isWaveActive) {
            return;
        }

        if (spawners == null || spawners.Count == 0) {
            return;
        }

        currentWave++;

        totalEnemiesForWave = GetEnemyCountForWave(currentWave);
        lastWaveEnemyCount = totalEnemiesForWave;
        enemiesSpawned = 0;
        enemiesKilled = 0;
        isWaveActive = true;

        GameManager.Instance?.SetState(GameState.InWave);

        currentWaveEnemyTypes = GetAvailableEnemyTypes(currentWave);
        PlayWaveStartSound();

        audioService?.PlayBGM(combatBGM, true, 1.0f, combatBGMVolume);

        StartCoroutine(SpawnInitialBatch());

        if (waveUI != null) {
            waveUI.UpdateWaveNumber(currentWave);
            waveUI.UpdateEnemiesRemaining(totalEnemiesForWave);
        }
    }

    /// <summary>
    /// Spawns the initial batch of enemies at the start of the wave.
    /// It is limited by <see cref="maxEnemiesAliveAtOnce"/> to avoid overwhelming the scene.
    /// </summary>
    private IEnumerator SpawnInitialBatch() {
        int initialCount = Mathf.Min(maxEnemiesAliveAtOnce, totalEnemiesForWave);

        for (int i = 0; i < initialCount; i++) {
            SpawnOneEnemy();

            yield return new WaitForSeconds(0.15f);
        }
    }

    /// <summary>
    /// Spawns exactly one enemy at a random spawner in the scene.
    /// Increments <c>enemiesSpawned</c> and selects the type based on the configured weight.
    /// </summary>
    private void SpawnOneEnemy() {
        if (enemiesSpawned >= totalEnemiesForWave) return;
        if (currentWaveEnemyTypes == null || currentWaveEnemyTypes.Count == 0) return;

        EnemySpawner spawner = spawners[Random.Range(0, spawners.Count)];
        spawner.SpawnEnemies(currentWaveEnemyTypes);
        enemiesSpawned++;
    }

    /// <summary>
    /// Attempts to spawn the next enemy.
    /// Called whenever an enemy dies, ensuring the scene remains populated.
    /// </summary>
    private void TrySpawnNext() {
        int aliveNow = enemiesSpawned - enemiesKilled;

        if (enemiesSpawned < totalEnemiesForWave && aliveNow < maxEnemiesAliveAtOnce)
            SpawnOneEnemy();
    }

    /// <summary>
    /// Called automatically every time any enemy dies in the scene.
    /// </summary>
    private void HandleEnemyDied() {
        enemiesKilled++;

        int totalRemaining = Mathf.Max(0, totalEnemiesForWave - enemiesKilled);
        if (waveUI != null)
            waveUI.UpdateEnemiesRemaining(totalRemaining);

        TrySpawnNext();

        if (enemiesKilled >= totalEnemiesForWave)
            OnWaveCompleted();
    }

    /// <summary>
    /// Called when the last enemy of the wave dies.
    /// Awards currency to the player based on wave completion.
    /// Formula: 1000 for wave 1, +500 for each additional wave (1500 for wave 2, 2000 for wave 3, etc.)
    /// </summary>
    private void OnWaveCompleted() {
        isWaveActive = false;

        GameManager.Instance?.SetState(GameState.Playing);

        if (waveUI != null)
            waveUI.ShowWaveClearAnnouncement();

        if (EconomyManager.Instance != null) {
            int waveReward = 1000 + (500 * (currentWave - 1));
            EconomyManager.Instance.AddCurrency(waveReward);
        }

        audioService?.PlayBGM(ambientBGM, true, 2.0f, ambientBGMVolume);
        audioService?.PlaySFX2D(waveClearClip);
    }

    /// <summary>
    /// Returns the amount of enemies for the given wave based on a progressive growth formula.
    /// </summary>
    private int GetEnemyCountForWave(int wave) {
        if (wave == 1) return 5;

        float rawGrowth = initialGrowthRate - (wave - 2) * growthDecrement;
        float growth = Mathf.Max(minGrowthRate, rawGrowth);

        int count = Mathf.CeilToInt(lastWaveEnemyCount * (1f + growth));

        return Mathf.Min(count, maxEnemiesPerWave);
    }

    /// <summary>
    /// Returns the list of enemy types allowed for the current wave.
    /// </summary>
    private List<EnemySpawnConfig> GetAvailableEnemyTypes(int wave) {
        var available = new List<EnemySpawnConfig>();

        foreach (var config in enemyTypes) {
            if (config.prefab != null && config.minimumWave <= wave)
                available.Add(config);
        }

        return available;
    }

    /// <summary>
    /// Chooses the wave-start sound and plays it immediately or with the existing delay rule.
    /// </summary>
    private void PlayWaveStartSound() {
        AudioClip clip = GetWaveStartClip();
        if (clip == null)
            return;

        if (ShouldDelayWaveStartSound())
            StartCoroutine(PlayWaveStartSoundDelayed(clip));
        else
            audioService?.PlaySFX2D(clip);
    }

    /// <summary>
    /// Plays the selected start clip after a short delay.
    /// </summary>
    private IEnumerator PlayWaveStartSoundDelayed(AudioClip clip) {
        yield return new WaitForSeconds(0.5f);

        audioService?.PlaySFX2D(clip);
    }

    /// <summary>
    /// Returns true when the wave-start SFX should be delayed by 0.5 seconds.
    /// </summary>
    private bool ShouldDelayWaveStartSound() {
        return currentWave > lastLightWave && !HasBossEnemyAvailable();
    }

    /// <summary>
    /// Returns the correct SFX for the current wave based on progression and boss presence.
    /// </summary>
    private AudioClip GetWaveStartClip() {
        if (HasBossEnemyAvailable())
            return bossWaveClip != null ? bossWaveClip : hardWaveClip ?? mediumWaveClip ?? lightWaveClip;

        if (currentWave <= lastLightWave)
            return lightWaveClip != null ? lightWaveClip : mediumWaveClip ?? hardWaveClip ?? bossWaveClip;

        if (currentWave <= lastMediumWave)
            return mediumWaveClip != null ? mediumWaveClip : hardWaveClip ?? lightWaveClip ?? bossWaveClip;

        return hardWaveClip != null ? hardWaveClip : mediumWaveClip ?? lightWaveClip ?? bossWaveClip;
    }

    /// <summary>
    /// Checks whether the current wave contains at least one enemy marked as boss.
    /// </summary>
    private bool HasBossEnemyAvailable() {
        if (currentWaveEnemyTypes == null)
            return false;

        for (int i = 0; i < currentWaveEnemyTypes.Count; i++) {
            if (currentWaveEnemyTypes[i] != null && currentWaveEnemyTypes[i].isBoss)
                return true;
        }

        return false;
    }

    #endregion
}
