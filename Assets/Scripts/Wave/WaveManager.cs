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
    
    // Round-robin spawning (sempre ativo - padrão)
    private int currentSpawnerIndex = 0;
    
    // Boss wave tracking - counts how many bosses have been spawned in this wave
    private int bossForcedCount = 0;

    public event System.Action OnWaveStarted;
    public event System.Action OnWaveCompleted;

    private List<EnemySpawnConfig> currentWaveEnemyTypes;
    private IAudioManagerService audioService;
    private FogController fogController;

    #endregion

    #region PROPERTIES
    public bool IsWaveActive => isWaveActive;
    public int CurrentWave => currentWave;
    public float WaveTimer => waveTimer;
    public bool IsCountdownActive => isCountdownActive;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        fogController = FindObjectOfType<FogController>();
    }

    private void Start() {
        // audioService?.PlayBGM(ambientBGM, true, 1.5f, ambientBGMVolume);
        // StartInitialCountdown(); // Removido para que a primeira wave seja engatilhada pelo TutorialEndTrigger
    }

    private void Update() {
        HandleTimers();
    }

    /// <summary>
    /// Manages the logic for both the countdown between waves and the duration of an active wave.
    /// </summary>
    private void HandleTimers() {
        if (isWaveActive) {
            // Durante a wave, o timer conta de forma crescente (tempo de duração da wave)
            waveTimer += Time.deltaTime;
        } else if (isCountdownActive) {
            // Entre waves, o timer conta de forma decrescente (tempo para a próxima wave)
            waveTimer -= Time.deltaTime;

            // Toca o som de tick nos últimos 10 segundos
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
        // Ajustado para 10.0f para evitar tocar 2x no início
        if (waveTimer <= 10.0f && waveTimer > 0) {
            int currentSecond = Mathf.CeilToInt(waveTimer);
            
            // Só toca se mudamos de segundo e o clip existe
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
        lastTickSecond = -1; // Reseta o rastreador de áudio
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

        // Se o player apertar o botão antes do timer zerar, paramos o countdown
        isCountdownActive = false;
        waveTimer = 0f; // Reseta para começar a contar o tempo da wave de forma crescente

        currentWave++;

        totalEnemiesForWave = GetEnemyCountForWave(currentWave);
        
        // Reset boss counter for new wave
        bossForcedCount = 0;
        lastWaveEnemyCount = totalEnemiesForWave;
        enemiesSpawned = 0;
        enemiesKilled = 0;
        currentSpawnerIndex = 0;  // Reset for round-robin spawning
        isWaveActive = true;

        GameManager.Instance?.SetState(GameState.InWave);

        currentWaveEnemyTypes = GetAvailableEnemyTypes(currentWave);
        PlayWaveStartSound();

        // Handle boss wave special effects
        if (IsBossWave(currentWave)) {
            StartCoroutine(PlayBossWaveEffects());
        }

        OnWaveStarted?.Invoke();

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
    /// Spawns exactly one enemy at a spawner using round-robin distribution.
    /// In boss waves, spawns multiple bosses based on wave progression (1 at wave 5, 2 at wave 10, etc).
    /// After all bosses are spawned, spawns remaining regular enemies.
    /// </summary>
    private void SpawnOneEnemy() {
        if (enemiesSpawned >= totalEnemiesForWave) return;
        if (currentWaveEnemyTypes == null || currentWaveEnemyTypes.Count == 0) return;

        // Round-robin: cycle through spawners using modulo
        EnemySpawner spawner = spawners[currentSpawnerIndex % spawners.Count];
        currentSpawnerIndex++;

        List<EnemySpawnConfig> spawnConfig = new List<EnemySpawnConfig>();

        // If this is a boss wave, check if we need to spawn more bosses
        int bossMissionsNeeded = IsBossWave(currentWave) ? GetBossCountForWave(currentWave) : 0;
        
        if (bossMissionsNeeded > 0 && bossForcedCount < bossMissionsNeeded) {
            // Still need to spawn more bosses
            EnemySpawnConfig bossConfig = currentWaveEnemyTypes.Find(config => config.isBoss);
            if (bossConfig != null) {
                spawnConfig.Add(bossConfig);
                bossForcedCount++;
            }
        }
        // All bosses spawned or not a boss wave - spawn regular enemies
        else {
            foreach (var config in currentWaveEnemyTypes) {
                if (!config.isBoss) {
                    spawnConfig.Add(config);
                }
            }
        }

        // Spawn with the filtered config
        if (spawnConfig.Count > 0) {
            spawner.SpawnEnemies(spawnConfig);
            enemiesSpawned++;
        }
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
            CompleteWave();
    }

    /// <summary>
    /// Called when the last enemy of the wave dies.
    /// Awards currency to the player based on wave completion.
    /// Formula: 1000 for wave 1, +500 for each additional wave (1500 for wave 2, 2000 for wave 3, etc.)
    /// </summary>
    private void CompleteWave() {
        isWaveActive = false;
        
        // Reset boss wave effects if this was a boss wave
        if (IsBossWave(currentWave) && fogController != null) {
            fogController.ResetFogColor();
        }

        OnWaveCompleted?.Invoke();

        GameManager.Instance?.SetState(GameState.Playing);

        if (waveUI != null)
            waveUI.ShowWaveClearAnnouncement();

        if (EconomyManager.Instance != null) {
            // Recompensa base pela wave
            int waveReward = 1000 + (500 * (currentWave - 1));
            
            // Recompensa bônus por velocidade (quanto mais rápido, mais ganha)
            // A lógica é: bonusBase - (tempo gasto * multiplicador). Se demorar demais, o bônus zera.
            int speedBonus = Mathf.Max(0, bonusBaseAmount - Mathf.FloorToInt(waveTimer * bonusTimeMultiplier));
            
            EconomyManager.Instance.AddCurrency(waveReward + speedBonus);
            
            Debug.Log($"[WaveManager] Wave {currentWave} completed! Base: {waveReward} | Bonus: {speedBonus} (Time: {waveTimer:F1}s)");
        }

        // Ativa o botão de pular timer após a primeira wave
        if (currentWave == 1 && waveButtonObject != null) {
            waveButtonObject.SetActive(true);
        }

        // Prepara o countdown para a próxima wave
        waveTimer = timeBetweenWaves;
        isCountdownActive = true;
        lastTickSecond = -1; // Reseta para a próxima wave

        audioService?.PlayBGM(ambientBGM, true, 2.0f, ambientBGMVolume);
        audioService?.PlaySFX2D(waveClearClip, waveClearSFXVolume);
    }

    /// <summary>
    /// Returns the amount of enemies for the given wave based on a progressive growth formula.
    /// </summary>
    private int GetEnemyCountForWave(int wave) {
        if (wave == 1) return 5;

        int count = Mathf.CeilToInt(lastWaveEnemyCount * (1f + growthRate));

        return Mathf.Min(count, maxEnemiesPerWave);
    }

    /// <summary>
    /// Returns the list of enemy types allowed for the current wave.
    /// - If NOT a boss wave: excludes enemies marked as boss
    /// - If IS a boss wave: includes all enemies, with boss guaranteed to appear at least once
    /// </summary>
    private List<EnemySpawnConfig> GetAvailableEnemyTypes(int wave) {
        var available = new List<EnemySpawnConfig>();
        bool isBossWave = IsBossWave(wave);

        foreach (var config in enemyTypes) {
            if (config.prefab == null || config.minimumWave > wave)
                continue;

            // If NOT a boss wave, skip enemies marked as boss
            if (!isBossWave && config.isBoss)
                continue;

            available.Add(config);
        }

        // Debug log to verify filtering
        Debug.Log($"[WaveManager] Wave {wave} - Boss Wave: {isBossWave} - Available types: {available.Count}");
        foreach (var config in available) {
            Debug.Log($"  - {config.prefab.name} (isBoss: {config.isBoss})");
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
    /// Boss waves (every 5 waves) should not be delayed.
    /// </summary>
    private bool ShouldDelayWaveStartSound() {
        return currentWave > lastLightWave && !IsBossWave(currentWave);
    }

    /// <summary>
    /// Returns the number of bosses that should spawn in a boss wave.
    /// Formula: (wave / 5), so wave 5 = 1 boss, wave 10 = 2 bosses, wave 15 = 3 bosses, etc.
    /// </summary>
    private int GetBossCountForWave(int wave) {
        return wave / 5;
    }

    /// <summary>
    /// Returns the correct SFX for the current wave based on progression and boss presence.
    /// Uses IsBossWave() (every 5 waves) instead of checking available enemy types.
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

    /// <summary>
    /// Checks if the given wave number is a boss wave (every 5 waves).
    /// </summary>
    private bool IsBossWave(int wave) {
        return wave > 0 && wave % 5 == 0;
    }

    /// <summary>
    /// Plays boss wave effects: extra scream SFX after a delay and changes fog color to red.
    /// </summary>
    private IEnumerator PlayBossWaveEffects() {
        // Wait for the specified delay before playing the scream
        yield return new WaitForSeconds(bossWaveScreamDelaySeconds);

        // Play the extra scream sound using IAudioManagerService
        if (bossWaveExtraScream != null && audioService != null) {
            audioService.PlaySFX2D(bossWaveExtraScream, bossWaveScreamVolume);
        }

        // Change fog color to red
        if (fogController != null) {
            fogController.SetFogColor(bossWaveFogColor);
        }
    }

    #endregion
}
