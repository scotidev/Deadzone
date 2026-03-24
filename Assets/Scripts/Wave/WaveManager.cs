using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton Manager responsible for the entire wave lifecycle.
/// </summary>
/// 
public class WaveManager : MonoBehaviour {
    /// <summary>Global access point to the single <see cref="WaveManager"/> instance.</summary>
    public static WaveManager Instance { get; private set; }

    [Header("Enemy Types")]
    [Tooltip("List of all enemy types.")]
    [SerializeField] private List<EnemySpawnConfig> enemyTypes;

    [Header("Wave Scaling")]
    [Tooltip("Growth rate from wave 1 to 2.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float initialGrowthRate = 0.25f;

    [Tooltip("How much the rate decreases each wave.")]
    [Range(0f, 0.1f)]
    [SerializeField] private float growthDecrement = 0.02f;

    [Tooltip("Minimum growth rate.")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float minGrowthRate = 0.05f;

    [Tooltip("Maximum number of enemies that can exist in a wave.")]
    [SerializeField] private int maxEnemiesPerWave = 500;

    [Header("Spawners")]
    [Tooltip("Drag ALL GameObjects with EnemySpawner in the scene here.")]
    [SerializeField] private List<EnemySpawner> spawners;

    [Header("Simultaneous Enemies Limit")]
    [Tooltip("Maximum number of enemies alive at the same time in the scene.")]
    [SerializeField] private int maxEnemiesAliveAtOnce = 15;

    [Header("HUD")]
    [Tooltip("Reference to the WaveUI component in the scene.")]
    [SerializeField] private WaveUI waveUI;

    private int currentWave = 0;
    private int totalEnemiesForWave = 0;
    private int enemiesSpawned = 0;
    private int enemiesKilled = 0;
    private int lastWaveEnemyCount = 5;
    private bool isWaveActive = false;

    private List<EnemySpawnConfig> currentWaveEnemyTypes;

    public bool IsWaveActive => isWaveActive;
    public int CurrentWave => currentWave;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable() {
        EnemyBase.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable() {
        EnemyBase.OnAnyEnemyDied -= HandleEnemyDied;
    }

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

        StartCoroutine(SpawnInitialBatch());

        if (waveUI != null) {
            waveUI.UpdateWaveNumber(currentWave);
            waveUI.UpdateEnemiesRemaining(totalEnemiesForWave);
        }
    }

    /// <summary>
    /// Spawns the inital batch of enemies at the start of the wave.
    /// It's limited tomaxEnemiesAliveAtOnce to avoid overwhelming the scene, with a small 
    /// delay between each spawn.
    /// </summary>
    private IEnumerator SpawnInitialBatch() {
        int initialCount = Mathf.Min(maxEnemiesAliveAtOnce, totalEnemiesForWave);

        for (int i = 0; i < initialCount; i++) {
            SpawnOneEnemy();

            yield return new WaitForSeconds(0.15f);
        }
    }

    /// <summary>
    /// Spawns exactly ONE enemy at a random spawner in the scene.
    /// Increments enemiesSpawned and selects the type based on the configured weight.
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
    /// Called whenever an enemy dies, ensuring the scene
    /// remains populated without exceeding the simultaneous limit.
    /// </summary>
    private void TrySpawnNext() {
        int aliveNow = enemiesSpawned - enemiesKilled;

        if (enemiesSpawned < totalEnemiesForWave && aliveNow < maxEnemiesAliveAtOnce)
            SpawnOneEnemy();
    }

    /// <summary>
    /// Called automatically every time any Enemy dies in the scene.
    /// Subscribed to the Enemy.OnAnyEnemyDied event in OnEnable().
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
    /// Releases the WaveButton and updates the game state.
    /// </summary>
    private void OnWaveCompleted() {
        isWaveActive = false;

        GameManager.Instance?.SetState(GameState.Playing);
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
}
