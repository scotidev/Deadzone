using InfimaGames.LowPolyShooterPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager responsible for the entire wave lifecycle.
/// </summary>
public class WaveManager : MonoBehaviour {

    #region STATIC

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
    [Range(0f, 1f)]
    [SerializeField] private float waveStartSFXVolume = 1f;

    [SerializeField] private int lastLightWave = 3;
    [SerializeField] private int lastMediumWave = 7;

    [Header("Wave Clear SFX")]
    [SerializeField] private AudioClip waveClearClip;
    [Range(0f, 1f)]
    [SerializeField] private float waveClearSFXVolume = 1f;

    [Header("Countdown SFX")]
    [SerializeField] private AudioClip countdownTickClip;
    [Range(0f, 1f)]
    [SerializeField] private float countdownTickVolume = 0.8f;

    [Header("Wave")]

    [Tooltip("Growth rate applied every wave (0 = no growth, 1 = doubles each wave).")]
    [Range(0f, 1f)]
    [SerializeField] private float growthRate = 0.25f;

    [SerializeField] private int maxEnemiesPerWave = 500;
    [SerializeField] private int maxEnemiesAliveAtOnce = 15;

    [Header("Wave Progression")]

    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private int bonusBaseAmount = 1000;
    [SerializeField] private float bonusTimeMultiplier = 5f;

    [Header("Spawners")]

    [SerializeField] private List<EnemySpawner> spawners;

    [Header("HUD")]
    [SerializeField] private WaveUI waveUI;
    [SerializeField] private GameObject waveButtonObject;

    [Header("Music Settings")]
    [SerializeField] private AudioClip ambientBGM;
    [Range(0f, 1f)]
    [SerializeField] private float ambientBGMVolume = 1f;
    [SerializeField] private AudioClip combatBGM;
    [Range(0f, 1f)]
    [SerializeField] private float combatBGMVolume = 1f;

    [Header("Boss Wave Settings")]
    [SerializeField] private AudioClip bossWaveExtraScream;
    [Range(0f, 1f)]
    [SerializeField] private float bossWaveScreamVolume = 1f;
    [SerializeField] private Color bossWaveFogColor = Color.red;
    [Tooltip("Delay in seconds before playing the boss wave extra scream sound.")]
    [SerializeField] private float bossWaveScreamDelaySeconds = 2f;

    [Header("Penguin Easter Egg Settings")]
    [Tooltip("Fog color when the Penguin Easter egg is activated. Default is blue.")]
    [SerializeField] private Color penguinWaveFogColor = Color.blue;

    [Header("Enemy Stat Scaling")]
    [Tooltip("The wave at which enemy stat scaling reaches its maximum.")]
    [SerializeField] private int maxWaveScalingStats = 20;

    #endregion

    #region FIELDS

    private int currentWave = 0;
    private int totalEnemiesForWave = 0;
    private int enemiesSpawned = 0;
    private int enemiesKilled = 0;
    private int lastWaveEnemyCount = 5;
    private bool isWaveActive = false;
    private float waveTimer = 0f;
    private bool isCountdownActive = false;
    private int lastTickSecond = -1;

    private int currentSpawnerIndex = 0;

    private int bossForcedCount = 0;

    private List<EnemySpawnConfig> currentWaveEnemyTypes;
    private IAudioManagerService audioService;
    private FogController fogController;

    #endregion

    #region EVENTS

    public event System.Action OnWaveStarted;
    public event System.Action OnWaveCompleted;

    #endregion

    #region PROPERTIES
    public bool IsWaveActive => isWaveActive;
    public int CurrentWave => currentWave;
    public float WaveTimer => waveTimer;
    public bool IsCountdownActive => isCountdownActive;
    public int MaxWaveScalingStats => maxWaveScalingStats;

    public Color PenguinWaveFogColor => penguinWaveFogColor;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        fogController = FindFirstObjectByType<FogController>();
    }

    private void Start() {
        PenguinMode.Reset();
    }

    private void Update() {
        HandleTimers();
    }

    /// <summary>
    /// Manages the logic for both the countdown between waves and the duration of an active wave.
    /// </summary>
    private void HandleTimers() {
        if (isWaveActive) {
            waveTimer += Time.deltaTime;
        } else if (isCountdownActive) {
            waveTimer -= Time.deltaTime;

            HandleCountdownAudio();

            if (waveTimer <= 0) {
                waveTimer = 0;
                isCountdownActive = false;
                StartNextWave();
            }
        }
    }

    /// <summary>
    /// Plays a tick sound every second during the last 10 seconds of the countdown.
    /// </summary>
    private void HandleCountdownAudio() {
        if (waveTimer <= 10.0f && waveTimer > 0) {
            int currentSecond = Mathf.CeilToInt(waveTimer);

            if (currentSecond != lastTickSecond && countdownTickClip != null) {
                lastTickSecond = currentSecond;
                audioService?.PlaySFX2D(countdownTickClip, countdownTickVolume);
            }
        }
    }

    /// <summary>
    /// Starts the countdown for the very first wave of the game.
    /// </summary>
    private void StartInitialCountdown() {
        waveTimer = timeBetweenWaves;
        isCountdownActive = true;
        lastTickSecond = -1;
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
    /// </summary>
    public void StartNextWave() {
        if (isWaveActive) {
            return;
        }

        if (spawners == null || spawners.Count == 0) {
            return;
        }

        isCountdownActive = false;
        waveTimer = 0f;

        currentWave++;

        totalEnemiesForWave = GetEnemyCountForWave(currentWave);

        bossForcedCount = 0;
        lastWaveEnemyCount = totalEnemiesForWave;
        enemiesSpawned = 0;
        enemiesKilled = 0;
        currentSpawnerIndex = 0;
        isWaveActive = true;

        GameManager.Instance?.SetState(GameState.InWave);

        currentWaveEnemyTypes = GetAvailableEnemyTypes(currentWave);
        PlayWaveStartSound();

        if (IsBossWave(currentWave)) {
            StartCoroutine(PlayBossWaveEffects());
        }

        OnWaveStarted?.Invoke();

        audioService?.PlayBGM(combatBGM, true, 1.0f, combatBGMVolume);

        StartCoroutine(SpawnInitialBatch());

        if (waveUI != null) {
            if (PenguinMode.IsCurrentWavePenguinWave) {
                waveUI.ShowPenguinWaveAnnouncement();
            } else {
                waveUI.UpdateWaveNumber(currentWave);
            }
            waveUI.UpdateEnemiesRemaining(totalEnemiesForWave);
        }
    }

    /// <summary>
    /// Spawns the initial batch of enemies at the start of the wave.
    /// </summary>
    private IEnumerator SpawnInitialBatch() {
        int initialCount = Mathf.Min(maxEnemiesAliveAtOnce, totalEnemiesForWave);

        for (int i = 0; i < initialCount; i++) {
            SpawnOneEnemy();

            yield return new WaitForSeconds(0.15f);
        }
    }

    /// <summary>
    /// Spawns exactly one enemy at a spawner using round-robin distribution.
    /// </summary>
    private void SpawnOneEnemy() {
        if (enemiesSpawned >= totalEnemiesForWave) return;
        if (currentWaveEnemyTypes == null || currentWaveEnemyTypes.Count == 0) return;

        EnemySpawner spawner = spawners[currentSpawnerIndex % spawners.Count];
        currentSpawnerIndex++;

        List<EnemySpawnConfig> spawnConfig = new List<EnemySpawnConfig>();

        int bossMissionsNeeded = IsBossWave(currentWave) ? GetBossCountForWave(currentWave) : 0;

        if (bossMissionsNeeded > 0 && bossForcedCount < bossMissionsNeeded) {
            EnemySpawnConfig bossConfig = currentWaveEnemyTypes.Find(config => config.isBoss);
            if (bossConfig != null) {
                spawnConfig.Add(bossConfig);
                bossForcedCount++;
            }
        }
        else {
            foreach (var config in currentWaveEnemyTypes) {
                if (!config.isBoss) {
                    spawnConfig.Add(config);
                }
            }
        }

        if (spawnConfig.Count > 0) {
            spawner.SpawnEnemies(spawnConfig);
            enemiesSpawned++;
        }
    }

    /// <summary>
    /// Attempts to spawn the next enemy when an enemy dies.
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
            CompleteWave();
    }

    /// <summary>
    /// Called when the last enemy of the wave dies.
    /// Awards currency based on wave completion and speed bonus.
    /// </summary>
    private void CompleteWave() {
        isWaveActive = false;

        if (IsBossWave(currentWave) && fogController != null) {
            fogController.ResetFogColor();
        }

        if (PenguinMode.IsCurrentWavePenguinWave && fogController != null) {
            fogController.ResetFogColor();
        }

        OnWaveCompleted?.Invoke();

        GameManager.Instance?.SetState(GameState.Playing);

        if (waveUI != null)
            waveUI.ShowWaveClearAnnouncement();

        if (EconomyManager.Instance != null) {
            int waveReward = 1000 + (500 * (currentWave - 1));

            int speedBonus = Mathf.Max(0, bonusBaseAmount - Mathf.FloorToInt(waveTimer * bonusTimeMultiplier));

            EconomyManager.Instance.AddCurrency(waveReward + speedBonus);
        }

        if (currentWave == 1 && waveButtonObject != null) {
            waveButtonObject.SetActive(true);
        }

        waveTimer = timeBetweenWaves;
        isCountdownActive = true;
        lastTickSecond = -1;

        audioService?.PlayBGM(ambientBGM, true, 2.0f, ambientBGMVolume);
        audioService?.PlaySFX2D(waveClearClip, waveClearSFXVolume);
    }

    /// <summary>
    /// Returns the enemy count for a wave based on progressive growth formula.
    /// </summary>
    private int GetEnemyCountForWave(int wave) {
        if (wave == 1) return 5;

        int count = Mathf.CeilToInt(lastWaveEnemyCount * (1f + growthRate));

        return Mathf.Min(count, maxEnemiesPerWave);
    }

    /// <summary>
    /// Returns the list of enemy types allowed for the current wave.
    /// </summary>
    private List<EnemySpawnConfig> GetAvailableEnemyTypes(int wave) {
        var available = new List<EnemySpawnConfig>();
        bool isBossWave = IsBossWave(wave);

        foreach (var config in enemyTypes) {
            if (config.prefab == null || config.minimumWave > wave)
                continue;

            if (!isBossWave && config.isBoss)
                continue;

            available.Add(config);
        }

        return available;
    }

    /// <summary>
    /// Chooses the wave-start sound and plays it.
    /// </summary>
    private void PlayWaveStartSound() {
        AudioClip clip = GetWaveStartClip();
        if (clip == null)
            return;

        if (ShouldDelayWaveStartSound())
            StartCoroutine(PlayWaveStartSoundDelayed(clip));
        else
            audioService?.PlaySFX2D(clip, waveStartSFXVolume);
    }

    /// <summary>
    /// Plays the selected start clip after a short delay.
    /// </summary>
    private IEnumerator PlayWaveStartSoundDelayed(AudioClip clip) {
        yield return new WaitForSeconds(0.5f);

        audioService?.PlaySFX2D(clip, waveStartSFXVolume);
    }

    /// <summary>
    /// Returns true when the wave-start SFX should be delayed by 0.5 seconds.
    /// </summary>
    private bool ShouldDelayWaveStartSound() {
        return currentWave > lastLightWave && !IsBossWave(currentWave);
    }

    /// <summary>
    /// Returns the number of bosses to spawn in a boss wave (wave / 5).
    /// </summary>
    private int GetBossCountForWave(int wave) {
        return wave / 5;
    }

    /// <summary>
    /// Returns the correct SFX for the current wave based on progression.
    /// </summary>
    private AudioClip GetWaveStartClip() {
        if (IsBossWave(currentWave))
            return bossWaveClip != null ? bossWaveClip : hardWaveClip ?? mediumWaveClip ?? lightWaveClip;

        if (currentWave <= lastLightWave)
            return lightWaveClip != null ? lightWaveClip : mediumWaveClip ?? hardWaveClip ?? bossWaveClip;

        if (currentWave <= lastMediumWave)
            return mediumWaveClip != null ? mediumWaveClip : hardWaveClip ?? lightWaveClip ?? bossWaveClip;

        return hardWaveClip != null ? hardWaveClip : mediumWaveClip ?? lightWaveClip ?? bossWaveClip;
    }

    /// <summary>
    /// Checks if the current wave has at least one boss enemy available.
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

    /// <summary>
    /// Checks if the given wave number is a boss wave (every 5 waves).
    /// </summary>
    private bool IsBossWave(int wave) {
        return wave > 0 && wave % 5 == 0;
    }

    /// <summary>
    /// Plays boss wave effects: extra scream SFX and fog color change.
    /// </summary>
    private IEnumerator PlayBossWaveEffects() {
        yield return new WaitForSeconds(bossWaveScreamDelaySeconds);

        if (bossWaveExtraScream != null && audioService != null) {
            audioService.PlaySFX2D(bossWaveExtraScream, bossWaveScreamVolume);
        }

        if (fogController != null) {
            fogController.SetFogColor(bossWaveFogColor);
        }
    }

    #endregion
}
