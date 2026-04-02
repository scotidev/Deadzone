using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager responsible for the entire wave lifecycle.
/// </summary>
public class WaveManager : MonoBehaviour {
    /// <summary>Global access point to the single <see cref="WaveManager"/> instance.</summary>
    public static WaveManager Instance { get; private set; }

    [Header("Enemy Types")]
    [Tooltip("List of all enemy types.")]
    [SerializeField] private List<EnemySpawnConfig> enemyTypes;

    [Header("Wave Start SFX")]
    [Tooltip("Sound played for early waves.")]
    [SerializeField] private AudioClip lightWaveClip;

    [Tooltip("Sound played after the initial waves.")]
    [SerializeField] private AudioClip mediumWaveClip;

    [Tooltip("Sound played for late-game waves.")]
    [SerializeField] private AudioClip hardWaveClip;

    [Tooltip("Sound played when the wave includes a boss enemy.")]
    [SerializeField] private AudioClip bossWaveClip;

    /// <summary>
    /// Sound played after the selected wave-start SFX finishes.
    /// </summary>
    [Tooltip("Sound played after the wave-start SFX finishes.")]
    [SerializeField] private AudioClip waveFadeOutClip;

    [Tooltip("Last wave that still counts as Light.")]
    [Min(1)]
    [SerializeField] private int lastLightWave = 3;

    [Tooltip("Last wave that still counts as Medium.")]
    [Min(1)]
    [SerializeField] private int lastMediumWave = 7;

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
        PlayWaveStartSound();

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

    /// <summary>
    /// Chooses the wave-start sound and decides whether it should be delayed.
    /// </summary>
    private void PlayWaveStartSound() {
        AudioClip clip = GetWaveStartClip();
        if (clip == null && waveFadeOutClip == null)
            return;

        StartCoroutine(PlayWaveStartSoundSequence(clip));
    }

    /// <summary>
    /// Plays the selected start clip, waits for it to finish, then plays the fade-out clip.
    /// </summary>
    private IEnumerator PlayWaveStartSoundSequence(AudioClip clip) {
        // Primeiro princípio: uma coroutine permite separar ações no tempo sem travar o jogo.
        // Aqui usamos isso para criar uma sequência sonora suave.
        if (clip != null) {
            // Waves médias e hard recebem um pequeno atraso antes do som principal.
            // A ideia é dar um pequeno respiro dramático antes do impacto sonoro.
            if (ShouldDelayWaveStartSound())
                yield return new WaitForSeconds(0.5f);

            // O AudioManager toca o som no canal de SFX persistente da scene Loader.
            AudioManager.Instance?.PlaySFX(clip);

            // Esperamos a duração real do áudio terminar.
            // Isso evita que o fade out entre cedo demais.
            yield return new WaitForSeconds(clip.length);
        }

        // Se houver um clip de fade out, ele é disparado após o som principal.
        if (waveFadeOutClip != null)
            AudioManager.Instance?.PlaySFX(waveFadeOutClip);
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
}
