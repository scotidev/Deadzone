using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  FogController controla APENAS a névoa densa ao redor do player
//  (o Particle System). Ele NÃO para o sistema — só reduz a emissão
//  para zero quando o player está na safezone.
//
//  Por que não parar o sistema completamente?
//  Porque com Simulation Space: World, partículas já emitidas ficam
//  no mundo. Ao entrar na casa e APENAS zerar a emissão:
//    → partículas do lado de fora persistem por mais alguns segundos
//    → olhando pela janela de dentro, a névoa AINDA É VISÍVEL
//    → ao sair novamente, a emissão volta instantaneamente
//
//  LAYER 1 – Scene Fog (Unity Lighting): sempre visível em toda a cena,
//            inclusive de dentro da casa olhando para fora.
//  LAYER 2 – Particle System (este script): névoa densa e imersiva
//            apenas ao redor do player quando está fora.
//
//  ONDE COLOCAR:
//  No mesmo GameObject do ParticleSystem de névoa, que deve ser
//  FILHO do Player com Simulation Space: World.

/// <summary>
/// Controla a emissão do ParticleSystem de névoa baseado no estado
/// de veneno do PlayerHealth (dentro ou fora da SafeZone).
///
/// Não para o sistema — apenas zera a emissão dentro da casa,
/// preservando partículas já emitidas visíveis pela janela.
///
/// Coloque este script no GameObject da névoa, filho do Player.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class FogController : MonoBehaviour {

    // ==============================================================
    //  CONFIGURAÇÃO
    // ==============================================================

    [Header("Emissão da Névoa")]
    [Tooltip("Partículas por segundo emitidas quando o player está fora da casa.")]
    [SerializeField] private float emissionRateOutside = 40f;

    // ==============================================================
    //  REFERÊNCIAS
    // ==============================================================

    private PlayerHealth playerHealth;
    private ParticleSystem fogParticles;

    // ==============================================================
    //  EmissionModule
    // ==============================================================
    //  EmissionModule é uma struct do ParticleSystem que controla
    //  quantas partículas são emitidas por segundo.
    //  Ela DEVE ser obtida uma vez e reutilizada — não é uma classe,
    //  então não guarda referência: cada ".emission" cria uma cópia nova.
    //  Por isso guardamos em "emission" e usamos sempre ela.
    private ParticleSystem.EmissionModule emission;

    // ==============================================================
    //  AWAKE / ONENABLE / ONDISABLE / START
    // ==============================================================

    private void Awake() {
        fogParticles = GetComponent<ParticleSystem>();
        emission     = fogParticles.emission;

        playerHealth = GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            Debug.LogError("[FogController] PlayerHealth não encontrado no pai! " +
                           "Certifique-se de que este GameObject é filho do Player.");
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
        // Começa com emissão zero — player está dentro da casa.
        // SafeZone vai disparar OnPoisonStateChanged(true) quando o
        // player sair, ativando a emissão automaticamente.
        SetEmissionRate(0f);
        fogParticles.Play();
    }

    // ==============================================================
    //  CALLBACK DO EVENTO
    // ==============================================================

    /// <summary>
    /// Chamado por PlayerHealth.OnPoisonStateChanged.
    ///   poisoned = true  → player saiu da casa  → névoa densa ativa.
    ///   poisoned = false → player entrou na casa → emissão zerada,
    ///                      partículas existentes ainda visíveis lá fora.
    /// </summary>
    private void HandlePoisonStateChanged(bool poisoned) {
        SetEmissionRate(poisoned ? emissionRateOutside : 0f);
    }

    // ==============================================================
    //  CONTROLE DE EMISSÃO
    // ==============================================================

    private void SetEmissionRate(float rate) {
        // ==============================================================
        //  MinMaxCurve com constante
        // ==============================================================
        //  rateOverTime aceita um MinMaxCurve. Atribuir um float diretamente
        //  cria automaticamente uma curva de valor constante igual ao float.
        //  É o jeito mais simples de definir uma taxa fixa de emissão.
        emission.rateOverTime = rate;
    }
}
