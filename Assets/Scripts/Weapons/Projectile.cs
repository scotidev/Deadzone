using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Projectile. Handles projectile movement, collision detection, damage application,
/// and object pool recycling for optimized performance.
/// </summary>
public class Projectile : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Range(5, 100)]
    public float destroyAfter;
    public bool destroyOnImpact = false;
    public float minDestroyTime;
    public float maxDestroyTime;

    [Header("Impact Effect Prefabs")]
    public Transform[] bloodImpactPrefabs;
    public Transform[] metalImpactPrefabs;
    public Transform[] dirtImpactPrefabs;
    public Transform[] concreteImpactPrefabs;

    [Header("Damage")]
    public float damage = 25f;

    #endregion

    #region FIELDS

    private PooledObject pooledObject;

    #endregion

    #region UNITY

    private void Awake() {
        pooledObject = GetComponent<PooledObject>();
    }

    private void Start() {
        var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
        Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());

        if (pooledObject == null) {
            StartCoroutine(DestroyAfter());
        } else {
            StartCoroutine(PooledDestroyAfter());
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Returns the projectile to the object pool, or destroys it if not pooled.
    /// </summary>
    private void Release() {
        if (pooledObject != null) {
            pooledObject.ReturnToPool();
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Spawns a random impact VFX prefab at the given position and rotation.
    /// </summary>
    private void SpawnImpactVFX(Transform[] prefabs, Vector3 position, Quaternion rotation) {
        if (prefabs == null || prefabs.Length == 0) return;

        Transform prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return;

        UnityEngine.Object.Instantiate(prefab, position, rotation);
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.GetComponent<Projectile>() != null)
            return;

        if (!destroyOnImpact) {
            StartCoroutine(DestroyTimer());
        } else {
            Release();
        }

        if (collision.transform.CompareTag("Blood")) {
            SpawnImpactVFX(bloodImpactPrefabs, transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Release();
        }

        if (collision.transform.CompareTag("Metal")) {
            SpawnImpactVFX(metalImpactPrefabs, transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Release();
        }

        if (collision.transform.CompareTag("Dirt")) {
            SpawnImpactVFX(dirtImpactPrefabs, transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Release();
        }

        if (collision.transform.CompareTag("Concrete")) {
            SpawnImpactVFX(concreteImpactPrefabs, transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Release();
        }

        if (collision.transform.CompareTag("Target")) {
            collision.transform.gameObject.GetComponent
                <TargetScript>().isHit = true;
            Release();
        }

        if (collision.transform.CompareTag("ExplosiveBarrel")) {
            IDamageable damageable = collision.transform.gameObject.GetComponent<IDamageable>();
            if (damageable != null) {
                damageable.TakeDamage(damage);
            }
            Release();
        }

        if (collision.transform.CompareTag("GasTank")) {
            collision.transform.gameObject.GetComponent
                <GasTankScript>().isHit = true;
            Release();
        }

        if (collision.transform.CompareTag("Enemy")) {
            EnemyBase enemy = collision.transform.GetComponentInParent<EnemyBase>();
            if (enemy != null) {
                enemy.TakeDamage(damage);
                HitmarkerManager.TriggerHitmarker();
            }

            Release();
        }
    }

    /// <summary>
    /// Waits a random time interval then releases the projectile.
    /// </summary>
    private IEnumerator DestroyTimer() {
        yield return new WaitForSeconds
            (Random.Range(minDestroyTime, maxDestroyTime));
        Release();
    }

    /// <summary>
    /// Waits and destroys the projectile after a set time (non-pooled fallback).
    /// </summary>
    private IEnumerator DestroyAfter() {
        yield return new WaitForSeconds(destroyAfter);
        Destroy(gameObject);
    }

    /// <summary>
    /// Waits and returns the pooled projectile after a set time.
    /// </summary>
    private IEnumerator PooledDestroyAfter() {
        yield return new WaitForSeconds(destroyAfter);
        Release();
    }

    #endregion
}
