using System;
using UnityEngine;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour {

	[Range(5, 100)]
	[Tooltip("After how long time should the bullet prefab be destroyed?")]
	public float destroyAfter;
	[Tooltip("If enabled the bullet destroys on impact")]
	public bool destroyOnImpact = false;
	[Tooltip("Minimum time after impact that the bullet is destroyed")]
	public float minDestroyTime;
	[Tooltip("Maximum time after impact that the bullet is destroyed")]
	public float maxDestroyTime;

	[Header("Impact Effect Prefabs")]
	public Transform [] bloodImpactPrefabs;
	public Transform [] metalImpactPrefabs;
	public Transform [] dirtImpactPrefabs;
	public Transform []	concreteImpactPrefabs;

	[Header("Damage")]
	// ==============================================================
	//  [ADICIONADO] Campo de dano ao inimigo
	// ==============================================================
	//  "public float damage" expõe o valor no Inspector do prefab
	//  do projétil. Cada arma pode ter um prefab de projétil com
	//  damage diferente (pistola = 25, espingarda = 40, etc.).
	//
	//  Este valor é usado abaixo em enemy.TakeDamage(damage) quando
	//  o projétil colide com um GameObject de tag "Enemy".
	[Tooltip("Dano causado em inimigos com tag 'Enemy' no acerto direto.")]
	public float damage = 25f;

	private void Start ()
	{
		//Grab the game mode service, we need it to access the player character!
		var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
		//Ignore the main player character's collision. A little hacky, but it should work.
		Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());
		
		//Start destroy timer
		StartCoroutine (DestroyAfter ());
	}

	//If the bullet collides with anything
	private void OnCollisionEnter (Collision collision)
	{
		//Ignore collisions with other projectiles.
		if (collision.gameObject.GetComponent<Projectile>() != null)
			return;
		
		// //Ignore collision if bullet collides with "Player" tag
		// if (collision.gameObject.CompareTag("Player")) 
		// {
		// 	//Physics.IgnoreCollision (collision.collider);
		// 	Debug.LogWarning("Collides with player");
		// 	//Physics.IgnoreCollision(GetComponent<Collider>(), GetComponent<Collider>());
		//
		// 	//Ignore player character collision, otherwise this moves it, which is quite odd, and other weird stuff happens!
		// 	Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
		//
		// 	//Return, otherwise we will destroy with this hit, which we don't want!
		// 	return;
		// }
		//
		//If destroy on impact is false, start 
		//coroutine with random destroy timer
		if (!destroyOnImpact) 
		{
			StartCoroutine (DestroyTimer ());
		}
		//Otherwise, destroy bullet on impact
		else 
		{
			Destroy (gameObject);
		}

		//If bullet collides with "Blood" tag
		if (collision.transform.tag == "Blood") 
		{
			//Instantiate random impact prefab from array
			Instantiate (bloodImpactPrefabs [Random.Range 
				(0, bloodImpactPrefabs.Length)], transform.position, 
				Quaternion.LookRotation (collision.contacts [0].normal));
			//Destroy bullet object
			Destroy(gameObject);
		}

		//If bullet collides with "Metal" tag
		if (collision.transform.tag == "Metal") 
		{
			//Instantiate random impact prefab from array
			Instantiate (metalImpactPrefabs [Random.Range 
				(0, bloodImpactPrefabs.Length)], transform.position, 
				Quaternion.LookRotation (collision.contacts [0].normal));
			//Destroy bullet object
			Destroy(gameObject);
		}

		//If bullet collides with "Dirt" tag
		if (collision.transform.tag == "Dirt") 
		{
			//Instantiate random impact prefab from array
			Instantiate (dirtImpactPrefabs [Random.Range 
				(0, bloodImpactPrefabs.Length)], transform.position, 
				Quaternion.LookRotation (collision.contacts [0].normal));
			//Destroy bullet object
			Destroy(gameObject);
		}

		//If bullet collides with "Concrete" tag
		if (collision.transform.tag == "Concrete") 
		{
			//Instantiate random impact prefab from array
			Instantiate (concreteImpactPrefabs [Random.Range 
				(0, bloodImpactPrefabs.Length)], transform.position, 
				Quaternion.LookRotation (collision.contacts [0].normal));
			//Destroy bullet object
			Destroy(gameObject);
		}

		//If bullet collides with "Target" tag
		if (collision.transform.tag == "Target") 
		{
			//Toggle "isHit" on target object
			collision.transform.gameObject.GetComponent
				<TargetScript>().isHit = true;
			//Destroy bullet object
			Destroy(gameObject);
		}
			
		//If bullet collides with "ExplosiveBarrel" tag
		if (collision.transform.tag == "ExplosiveBarrel") 
		{
			//Toggle "explode" on explosive barrel object
			collision.transform.gameObject.GetComponent
				<ExplosiveBarrelScript>().explode = true;
			//Destroy bullet object
			Destroy(gameObject);
		}

		//If bullet collides with "GasTank" tag
		if (collision.transform.tag == "GasTank") 
		{
			//Toggle "isHit" on gas tank object
			collision.transform.gameObject.GetComponent
				<GasTankScript> ().isHit = true;
			//Destroy bullet object
			Destroy(gameObject);
		}

		//If bullet collides with an enemy, deal damage and destroy the bullet.
		// GetComponentInParent handles the case where the bullet hits a child
		// collider (hitbox) rather than the root Enemy GameObject.
		if (collision.transform.CompareTag("Enemy"))
		{
			// ==============================================================
			//  [ADICIONADO] Lógica de dano ao inimigo
			// ==============================================================
			//  CompareTag("Enemy") é mais eficiente que .tag == "Enemy"
			//  porque evita alocação de string na comparação.
			//  O GameObject do inimigo (ou de qualquer filho dele) deve
			//  ter a tag "Enemy" configurada no Inspector.
			//
			//  GetComponentInParent<Enemy>()
			//  Busca o componente Enemy no GameObject ATINGIDO ou em
			//  qualquer ANCESTRAL na hierarquia.
			//  Por que não GetComponent<Enemy>()?
			//  A bala pode acertar um Collider filho do inimigo (ex: uma
			//  "hitbox" na cabeça ou no tronco), não o objeto raiz.
			//  GetComponentInParent sobe na hierarquia até encontrar Enemy.
			//
			//  enemy.TakeDamage(damage) → chama Enemy.TakeDamage() que
			//  reduz a vida e dispara Die() se necessário.
			Enemy enemy = collision.transform.GetComponentInParent<Enemy>();
			if (enemy != null)
				enemy.TakeDamage(damage);

			// Destroí o projétil após acertar o inimigo.
			Destroy(gameObject);
		}
	}

	private IEnumerator DestroyTimer () 
	{
		//Wait random time based on min and max values
		yield return new WaitForSeconds
			(Random.Range(minDestroyTime, maxDestroyTime));
		//Destroy bullet object
		Destroy(gameObject);
	}

	private IEnumerator DestroyAfter () 
	{
		//Wait for set amount of time
		yield return new WaitForSeconds (destroyAfter);
		//Destroy bullet object
		Destroy (gameObject);
	}
}