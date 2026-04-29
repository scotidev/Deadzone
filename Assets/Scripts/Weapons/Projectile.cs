using InfimaGames.LowPolyShooterPack;
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

    #endregion

    #region UNITY
    private void Start() {
        var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
        Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());

        StartCoroutine(DestroyAfter());
    }

    #endregion

    #region METHODS

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.GetComponent<Projectile>() != null)
            return;

        if (!destroyOnImpact) {
            StartCoroutine(DestroyTimer());
        } else {
            Destroy(gameObject);
        }

        if (collision.transform.tag == "Blood") {
            Instantiate(bloodImpactPrefabs[Random.Range
                (0, bloodImpactPrefabs.Length)], transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Destroy(gameObject);
        }

        if (collision.transform.tag == "Metal") {
            Instantiate(metalImpactPrefabs[Random.Range
                (0, bloodImpactPrefabs.Length)], transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Destroy(gameObject);
        }

        if (collision.transform.tag == "Dirt") {
            Instantiate(dirtImpactPrefabs[Random.Range
                (0, bloodImpactPrefabs.Length)], transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Destroy(gameObject);
        }

        if (collision.transform.tag == "Concrete") {
            Instantiate(concreteImpactPrefabs[Random.Range
                (0, bloodImpactPrefabs.Length)], transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            Destroy(gameObject);
        }

        if (collision.transform.tag == "Target") {
            collision.transform.gameObject.GetComponent
                <TargetScript>().isHit = true;
            Destroy(gameObject);
        }

        if (collision.transform.tag == "ExplosiveBarrel") {
            collision.transform.gameObject.GetComponent
                <ExplosiveBarrelScript>().explode = true;
            Destroy(gameObject);
        }

        if (collision.transform.tag == "GasTank") {
            collision.transform.gameObject.GetComponent
                <GasTankScript>().isHit = true;
            Destroy(gameObject);
        }

        if (collision.transform.CompareTag("Enemy")) {
            EnemyBase enemy = collision.transform.GetComponentInParent<EnemyBase>();
            if (enemy != null) {
                enemy.TakeDamage(damage);
                HitmarkerManager.TriggerHitmarker();
            }

            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyTimer() {
        yield return new WaitForSeconds
            (Random.Range(minDestroyTime, maxDestroyTime));
        Destroy(gameObject);
    }

    private IEnumerator DestroyAfter() {
        yield return new WaitForSeconds(destroyAfter);
        Destroy(gameObject);
    }

    #endregion
}