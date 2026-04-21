//using System.Collections;
//using UnityEngine;

///// <summary>
///// BearTrap is an environment trap that damages and immobilizes zombies when they step on it.
///// Only one zombie can be caught at a time. After the zombie is released, the trap disappears.
///// </summary>
//[RequireComponent(typeof(Rigidbody))]
//public class BearTrap : MonoBehaviour {
//    [Header("Bear Trap Settings")]
//    [SerializeField] private float damage = 25f;
//    [SerializeField] private float trapDuration = 5f;
//    [SerializeField] private float destroyDelay = 2f;

//    private bool isActive = true;
//    private EnemyBase caughtEnemy;
//    private Coroutine releaseCoroutine;
//    private EnemyFollow cachedEnemyFollow;
//    private UnityEngine.AI.NavMeshAgent cachedAgent;

//    private void Awake() {
//        var rb = GetComponent<Rigidbody>();
//        rb.isKinematic = true;
//        rb.useGravity = false;
//    }

//    private void OnTriggerEnter(Collider other) {
//        Debug.Log("BearTrap: Algo entrou no trigger - " + other.name + " | Layer: " + LayerMask.LayerToName(other.gameObject.layer));
//        if (!isActive) return;

//        EnemyBase enemy = other.GetComponent<EnemyBase>();
//        if (enemy != null && caughtEnemy == null) {
//            Debug.Log("BearTrap: Zumbi detectado! - " + other.name);
//            CatchEnemy(enemy);
//        }
//    }

//    private void CatchEnemy(EnemyBase enemy) {
//        isActive = false;
//        caughtEnemy = enemy;

//        enemy.TakeDamage(damage);

//        enemy.SetImmobilized(true);
//        Debug.Log("BearTrap: SetImmobilized(true) chamado!");

//        enemy.transform.SetParent(transform);

//        Debug.Log("BearTrap: Zumbi preso com sucesso!");
//        GetComponent<Collider>().enabled = false;

//        releaseCoroutine = StartCoroutine(ReleaseEnemyAfterDelay());
//    }

//    //private IEnumerator ReleaseEnemyAfterDelay()
//    //{
//    //    yield return new WaitForSeconds(trapDuration);

//    //    if (caughtEnemy != null)
//    //    {
//    //        caughtEnemy.SetImmobilized(false);
//    //        caughtEnemy.transform.SetParent(null);
//    //        Debug.Log("BearTrap: SetImmobilized(false) chamado - Zumbi liberado!");
//    //    }

//    //    yield return new WaitForSeconds(destroyDelay);

//    //    Destroy(gameObject);
//    //}
//}