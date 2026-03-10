using UnityEngine;

// ==============================================================
//  O QUE É UM ENUM?
// ==============================================================
//  "enum" (enumeração) é um tipo que lista valores nomeados.
//  Em vez de usar números mágicos (0, 1, 2...) para representar
//  estados, usamos nomes descritivos. O compilador garante que
//  só os valores definidos aqui podem ser usados.
//
//  IMPORTANTE: o PRIMEIRO item da lista vale 0 internamente.
//  Se não inicializarmos a propriedade State explicitamente,
//  o C# coloca 0 como padrão — ou seja, o primeiro item do enum.
//  Por isso "Loader" foi colocado PRIMEIRO: é o estado inicial
//  correto quando o jogo acaba de abrir.
//
//  ESTADOS DO JOGO (em ordem cronológica de ocorrência):
//    Loader    → cena de bootstrap, intro da logo sendo exibida
//    MainMenu  → tela inicial, sem gameplay ativo
//    Playing   → gameplay normal, loja acessível, sem onda ativa
//    Paused    → jogo pausado (PauseManager)
//    Shopping  → loja aberta (ShopManager) — cursor visível
//    InWave    → onda ativa — loja bloqueada enquanto inimigos existem
public enum GameState {
    Loader,     // Cena de bootstrap: managers inicializando, logo sendo exibida
    MainMenu,   // No menu principal
    Playing,    // Gameplay normal (entre ondas)
    Paused,     // Jogo pausado
    Shopping,   // Loja aberta
    InWave      // Onda de inimigos em andamento
}

/// <summary>
/// Persistent manager that tracks the global game state.
/// Survives all scene changes via DontDestroyOnLoad.
/// Other systems consult State to decide what is allowed.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    // ==============================================================
    //  PROPRIEDADE State
    // ==============================================================
    //  Guarda o estado atual do jogo.
    //  "= GameState.Loader" define o valor inicial: quando o objeto
    //  nasce na cena Loader, o estado já começa correto.
    //  "private set" impede que outros scripts escrevam diretamente
    //  — eles devem usar SetState() para mudar o estado.
    public GameState State { get; private set; } = GameState.Loader;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            // DontDestroyOnLoad: este GameObject sobrevive a todas as
            // trocas de cena. O GameManager persiste durante toda a sessão.
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Transitions the game to a new state.
    /// Called by WaveManager (InWave / Playing), ShopManager (Shopping),
    /// PauseManager (Paused), LogoIntro (Loader) and others.
    /// </summary>
    public void SetState(GameState newState) {
        State = newState;
    }
}
