using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;
/// <summary>
/// Singleton manager responsible for the entire wave lifecycle.
/// 
/// Migrado para usar IAudioManagerService para sons de início de wave.
/// Mantém o singleton para o WaveManager em si (gerenciamento de waves),
/// mas usa o serviço para áudio.
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
    
    /// <summary>
    /// Referência ao serviço de áudio obtida do Service Locator.
    /// Usada para tocar sons de início de wave de forma consistente.
    /// </summary>
    private IAudioManagerService audioService;

    public bool IsWaveActive => isWaveActive;
    public int CurrentWave => currentWave;

    private void Awake() {
        // Padrão Singleton: garante que só existe uma instância do WaveManager
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        // Obtém o serviço de áudio do Service Locator
        // ServiceLocator é inicializado no Bootstraper antes de qualquer cena carregar
        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
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
    /// Chooses the wave-start sound and plays it immediately or with the existing delay rule.
    /// 
    /// Agora usa o serviço de áudio unificado para tocar sons 2D (UI/Feedback).
    /// Sons de início de wave são considerados feedback/UI, não sons posicionais.
    /// </summary>
    private void PlayWaveStartSound() {
        AudioClip clip = GetWaveStartClip();
        if (clip == null)
            return;

        if (ShouldDelayWaveStartSound())
            StartCoroutine(PlayWaveStartSoundDelayed(clip));
        else
            // Usa PlaySFX2D porque é um som de feedback/UI, não posicional no mundo 3D
            audioService?.PlaySFX2D(clip);
    }

    /// <summary>
    /// Plays the selected start clip after a short delay.
    /// 
    /// Cria tensão dramática com um pequeno delay antes do som de impacto.
    /// Coroutine permite executar código após um delay sem travar o jogo.
    /// </summary>
    private IEnumerator PlayWaveStartSoundDelayed(AudioClip clip) {
        // Waves médias e hard recebem um pequeno atraso antes do som principal.
        // A ideia é dar um pequeno respiro dramático antes do impacto sonoro.
        yield return new WaitForSeconds(0.5f);
        
        // ?. só chama o método se audioService não for null (null-conditional operator)
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
}
