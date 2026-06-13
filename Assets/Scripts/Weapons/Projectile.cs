using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

// temos de alguma forma como fazer os projeteis acertarem inimigos e sair sangue? pq pelo que vi tem blood ali pra baixo, mas ano sei se está configurado? eu preciso assimilar uma textura ou algo assim? nao entendi como o sistema funciona sou iniciante

public class Projectile : MonoBehaviour {

    #region FIELDS

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

    // Referência em cache pro PooledObject pra evitar GetComponent toda vez
    // CONCEITO: Guardamos a referência uma vez no Awake pra não precisar
    // chamar GetComponent<PooledObject>() cada vez que o projétil acerta algo.
    private PooledObject pooledObject;

    #endregion

    #region UNITY

    private void Awake() {
        // CONCEITO: Cache do PooledObject. Se o prefab tiver esse componente
        // (adicionado automaticamente pelo pool), podemos devolver ao pool
        // em vez de destruir. Isso reduz drasticamente o garbage collection.
        pooledObject = GetComponent<PooledObject>();
    }

    private void Start() {
        var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
        Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());

        // CONCEITO: Se não veio do pool (pooledObject == null), o Destroy normal acontece.
        // Se veio do pool, chamamos a corrotina que devolve ao pool.
        if (pooledObject == null) {
            StartCoroutine(DestroyAfter());
        } else {
            StartCoroutine(PooledDestroyAfter());
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Devolve o projétil ao pool em vez de destruir, se ele veio do pool.
    /// Se não veio do pool, usa Destroy() normal (fallback).
    /// CONCEITO: Este é o coração da otimização. Em vez de Destroy(),
    /// que libera memória e causa GC, devolvemos ao pool pra reuso.
    /// </summary>
    private void Release() {
        if (pooledObject != null) {
            // CONCEITO: Devolver ao pool = desativar e guardar pra reuso.
            // O objeto continua existindo, só está "dormindo".
            pooledObject.ReturnToPool();
        } else {
            // CONCEITO: Fallback: se não tem PooledObject (não veio do pool),
            // usa Destroy normal como antes.
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Spawns an impact VFX transform at the given position and rotation.
    /// CONCEITO: Para efeitos de impacto (sangue, metal, terra, concreto),
    /// continuamos usando Instantiate normal. São objetos pequenos e efêmeros.
    /// O ganho maior de performance está em poolar os PROJÉTEIS, não os VFX.
    /// </summary>
    private void SpawnImpactVFX(Transform[] prefabs, Vector3 position, Quaternion rotation) {
        if (prefabs == null || prefabs.Length == 0) return;

        // CONCEITO: Pega um prefab aleatório da lista de impactos
        Transform prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return;

        // CONCEITO: Instantiate normal para VFX de impacto.
        // Esses objetos são pequenos e não tão frequentes quanto projéteis.
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

        if (collision.transform.tag == "Blood") {
            SpawnImpactVFX(bloodImpactPrefabs, transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Release();
        }

        if (collision.transform.tag == "Metal") {
            SpawnImpactVFX(metalImpactPrefabs, transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Release();
        }

        if (collision.transform.tag == "Dirt") {
            SpawnImpactVFX(dirtImpactPrefabs, transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Release();
        }

        if (collision.transform.tag == "Concrete") {
            SpawnImpactVFX(concreteImpactPrefabs, transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Release();
        }

        if (collision.transform.tag == "Target") {
            collision.transform.gameObject.GetComponent
                <TargetScript>().isHit = true;
            Release();
        }

        if (collision.transform.tag == "ExplosiveBarrel") {
            IDamageable damageable = collision.transform.gameObject.GetComponent<IDamageable>();
            if (damageable != null) {
                damageable.TakeDamage(damage);
            }
            Release();
        }

        if (collision.transform.tag == "GasTank") {
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

    private IEnumerator DestroyTimer() {
        yield return new WaitForSeconds
            (Random.Range(minDestroyTime, maxDestroyTime));
        Release();
    }

    private IEnumerator DestroyAfter() {
        yield return new WaitForSeconds(destroyAfter);
        Destroy(gameObject);
    }

    /// <summary>
    /// Versão do DestroyAfter que usa o pool em vez de Destroy.
    /// CONCEITO: Quando o projétil veio do pool, usamos esta corrotina
    /// que devolve ao pool após o tempo de vida máximo.
    /// </summary>
    private IEnumerator PooledDestroyAfter() {
        yield return new WaitForSeconds(destroyAfter);
        Release();
    }

    #endregion
}