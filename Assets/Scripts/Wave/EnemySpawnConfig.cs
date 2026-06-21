using UnityEngine;

/// <summary>
/// Defines an enemy type that can be spawned during waves.
/// Configure in the WaveManager inspector: prefab, minimum wave, spawn weight, and boss flag.
/// </summary>
[System.Serializable]
public class EnemySpawnConfig {

    #region SERIALIZED FIELDS

    public GameObject prefab;

    [Tooltip("The minimum wave this enemy can appear.")]
    public int minimumWave = 1;

    [Tooltip("Spawn weight is relative. Higher = more frequent.")]
    [Range(0.01f, 20f)]
    public float spawnWeight = 1f;

    [Tooltip("Mark this enemy as a boss. If it is available in the wave, the boss SFX will play.")]
    public bool isBoss;

    #endregion
}
