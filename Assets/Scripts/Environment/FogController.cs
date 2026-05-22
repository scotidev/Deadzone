using UnityEngine;

//REFATORAÇÃO: a fog deveria estar lá fora, nao no player. O que podemos manter aqui que é inteligente é: a logica de que quando o player sai do safezone, ele começa a tomar dano. Temos no SafeZone um trigger que detecta quando o player entra ou sai  e a partir disso ativa/desativa o dano pelo health. Agora o que nao podemos deixar é fazer com que a fog apenas apareça quando o player estiver fora do safezone, porque a fog é um elemento visual que deve estar presente no ambiente, nao apenas no player. O que podemos fazer é ter a fog sempre ativa, mas com uma emissao de particulas muito baixa (ou zero) quando o player estiver dentro do safezone, e aumentar a emissao quando ele sair. Assim, a fog continua existindo no ambiente, mas se torna mais densa e visível quando o player está em perigo.

/// <summary>
/// Responsible for controlling the emission of the fog ParticleSystem based on the player's poison state from PlayerHealth (inside or outside the SafeZone).
/// Attatch this script to the fog GameObject, which should be a child of the Player.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class FogController : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Fog Emission")]
    [SerializeField] private float emissionRateOutside = 40f;

    #endregion

    #region FIELDS

    private PlayerHealth playerHealth;
    private ParticleSystem fogParticles;

    private ParticleSystem.EmissionModule emission;

    #endregion

    #region UNITY 

    private void Awake() {
        fogParticles = GetComponent<ParticleSystem>();
        emission = fogParticles.emission;

        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void OnEnable() {
        if (playerHealth != null)
            playerHealth.OnPoisonStateChanged += HandlePoisonStateChanged;
    }

    private void OnDisable() {
        if (playerHealth != null)
            playerHealth.OnPoisonStateChanged -= HandlePoisonStateChanged;
    }

    private void Start() {
        // Start with zero emission to respect the tutorial phase.
        // It will be updated via HandlePoisonStateChanged when the tutorial ends.
        SetEmissionRate(0f);
        fogParticles.Play();
    }
    #endregion

    #region METHODS

    /// <summary>
    /// Called by PlayerHealth.OnPoisonStateChanged.
    /// </summary>
    private void HandlePoisonStateChanged(bool poisoned) {
        SetEmissionRate(poisoned ? emissionRateOutside : 0f);
    }

    /// <summary>
    /// rateOverTime accepts a MinMaxCurve. Assigning a float directly creates a constant value curve equal to that float.
    /// </summary>
    /// <param name="rate"></param>
    private void SetEmissionRate(float rate) {
        emission.rateOverTime = rate;
    }

    #endregion
}
