using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A special zombie for the tutorial that stays still and only takes damage.
/// Deactivates specified barriers when it dies to allow player progression.
/// Plays idle sounds at random intervals and a death sound on destruction.
/// </summary>
public class TutorialDummyZombie : EnemyBase {

    #region SERIALIZED FIELDS

    [Header("Tutorial Settings")]
    [Tooltip("Health value for this dummy, set in the inspector.")]
    [SerializeField] private float tutorialHealth = 50f;

    [Tooltip("The Pistol pickup GameObject to activate when this zombie dies.")]
    [SerializeField] private GameObject pistolPickupObject;

    [Header("Idle Sounds")]
    [Tooltip("Array of idle sounds played at random intervals.")]
    [SerializeField] private AudioClip[] idleSounds;
    [Range(0f, 1f)]
    [SerializeField] private float idleSoundVolume = 1f;
    [Tooltip("Random interval range between idle sound plays (min, max).")]
    [SerializeField] private Vector2 idleIntervalRange = new Vector2(5f, 7f);

    [Header("Death Sound")]
    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)]
    [SerializeField] private float deathSoundVolume = 1f;

    #endregion

    #region FIELDS

    /// <summary>
    /// Acumulador de tempo desde a última execução do som de idle.
    /// </summary>
    private float idleSoundTimer = 0f;

    /// <summary>
    /// Próximo tempo (em segundos) em que um som de idle deve tocar.
    /// Sorteado aleatoriamente entre idleIntervalRange.x e idleIntervalRange.y.
    /// </summary>
    private float idleSoundNextTime = 0f;

    #endregion

    #region UNITY

    protected override void Awake() {
        // Força que este seja um inimigo tutorial para não contar nas waves do WaveManager
        isTutorialEnemy = true;

        base.Awake();

        // Desabilita movimento e ataque para que ele fique parado
        if (enemyFollow != null) enemyFollow.enabled = false;
        if (enemyAttack != null) enemyAttack.enabled = false;

        // Inicia o timer do som de idle com um intervalo aleatório
        ResetIdleSoundTimer();
    }

    private void Update() {
        // Update só chama funções — a lógica fica dentro de cada método
        HandleIdleSound();
    }

    #endregion

    #region PROTECTED METHODS

    /// <summary>
    /// Initializes stats using the value defined in the inspector.
    /// Overrides the abstract method from EnemyBase.
    /// </summary>
    protected override void InitializeStats() {
        maxHealth = tutorialHealth;
    }

    /// <summary>
    /// Plays a random idle sound from the idleSounds array at random intervals.
    /// Sorteia um dos áudio clips do array e toca via Play3DSound (que usa IAudioManagerService).
    /// </summary>
    private void HandleIdleSound() {
        if (idleSounds == null || idleSounds.Length == 0)
            return;

        // Acumula o tempo passado desde o último som de idle
        idleSoundTimer += Time.deltaTime;

        // Quando o timer atinge o intervalo sorteado, toca um som e reinicia o timer
        if (idleSoundTimer >= idleSoundNextTime) {
            // Sorteia um dos 2 sons de idle aleatoriamente
            AudioClip clip = idleSounds[Random.Range(0, idleSounds.Length)];
            Play3DSound(clip, idleSoundVolume);
            ResetIdleSoundTimer();
        }
    }

    /// <summary>
    /// Resets the idle sound timer to a new random interval within the configured range.
    /// Zera o timer e sorteia um novo intervalo (ex: entre 5 e 7 segundos).
    /// </summary>
    private void ResetIdleSoundTimer() {
        idleSoundTimer = 0f;
        idleSoundNextTime = Random.Range(idleIntervalRange.x, idleIntervalRange.y);
    }

    /// <summary>
    /// Plays the death sound when this dummy zombie dies.
    /// Sobrescreve o método virtual de EnemyBase, que é chamado por Die().
    /// Usa Play3DSound, que internamente chama IAudioManagerService.PlaySFX3DAttached().
    /// </summary>
    public override void PlayDeathSound() {
        Play3DSound(deathSound, deathSoundVolume);
    }

    /// <summary>
    /// Handles dummy death: activates the pistol pickup for the player.
    /// </summary>
    protected override void Die() {
        // Ativa o pickup da pistola para o jogador coletar após a morte do zumbi
        if (pistolPickupObject != null) {
            pistolPickupObject.SetActive(true);
            Debug.Log("[TutorialDummyZombie] Pistol pickup activated!");
        }

        // Call base Die to handle currency, events, and destruction
        base.Die();
    }

    #endregion
}
