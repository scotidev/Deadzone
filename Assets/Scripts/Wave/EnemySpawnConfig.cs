using UnityEngine;

/// <summary>
/// Define a type of enemy that can be spawned during waves.
/// Configure in the WaveManager Inspector: prefab, minimum wave, and spawn weight.
/// </summary>
[System.Serializable]
public class EnemySpawnConfig {

    [Tooltip("Enemy Prefab. Must have the components Enemy, EnemyFollow, EnemyAttack, and NavMeshAgent.")]
    public GameObject prefab;

    [Tooltip("The minimum wave this enemy can appear.")]
    [Min(1)]
    public int minimumWave = 1;

    [Tooltip("Spawn weight is relative. Higher = more frequente.")]
    [Range(0.01f, 20f)]
    public float spawnWeight = 1f;
}
