using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy Spawn Point. Placed in the scene by the designer.
/// Receives from the WaveManager the list of available prefabs and the
/// quantity to spawn, then instantiates the enemies in valid NavMesh positions
/// near itself.
/// </summary>
public class EnemySpawner : MonoBehaviour {

    [Header("Spawn Settings")]
    [Tooltip("Maximum distance from the spawner center where enemies can appear.")]
    [SerializeField] private float spawnRadius = 3f;

    [Tooltip("Seconds of interval between each enemy instantiated in the batch.")]
    [SerializeField] private float spawnDelay = 0.3f;

    /// <summary>
    /// Spawns EXACTLY ONE enemy immediately near this spawner.
    /// Called by the WaveManager in the throttle system: each time
    /// an enemy dies and opens a slot, the WaveManager calls this method
    /// on a randomly chosen spawner.
    /// </summary>
    public void SpawnEnemies(List<EnemySpawnConfig> availableTypes) {
        if (availableTypes == null || availableTypes.Count == 0) return;

        GameObject prefab = PickWeightedRandom(availableTypes);
        Vector3 spawnPos = GetValidSpawnPosition();
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    private IEnumerator SpawnRoutine(List<EnemySpawnConfig> availableTypes, int count) {
        for (int i = 0; i < count; i++) {
            GameObject prefab = PickWeightedRandom(availableTypes);
            Vector3 spawnPos = GetValidSpawnPosition();

            Instantiate(prefab, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private GameObject PickWeightedRandom(List<EnemySpawnConfig> configs) {
        float totalWeight = 0f;
        foreach (var config in configs)
            totalWeight += config.spawnWeight;

        float roll = Random.Range(0f, totalWeight);

        float cumulative = 0f;
        foreach (var config in configs) {
            cumulative += config.spawnWeight;
            if (roll <= cumulative)
                return config.prefab;
        }

        return configs[configs.Count - 1].prefab;
    }

    /// <summary>
    /// Tries to find a valid position on the NavMesh within the spawn radius.
    /// If it fails after 10 attempts, it falls back to the spawner's own position.
    /// </summary>
    private Vector3 GetValidSpawnPosition() {
        for (int attempt = 0; attempt < 10; attempt++) {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }

        return transform.position;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
