using UnityEngine;

// ==============================================================
//  O QUE É UM ENUM?
// ==============================================================
//  "enum" (enumeração) é um tipo que lista valores nomeados.
//  Em vez de usar números mágicos (0, 1, 2...) para representar
//  estados, usamos nomes descritivos. O compilador garante que
//  só os valores definidos aqui podem ser usados.
//
//  Exemplo de uso:
//    GameManager.Instance.SetState(GameState.InWave);
//    if (GameManager.Instance.State == GameState.Playing) { ... }
//
//  ESTADOS DO JOGO:
//    MainMenu  → tela inicial, sem gameplay ativo
//    Playing   → gameplay normal, loja acessível, sem onda ativa
//    Paused    → jogo pausado (PauseManager)
//    Shopping  → loja aberta (ShopManager) — cursor visível
//    InWave    → onda ativa — loja bloqueada enquanto inimigos existem

public enum GameState {
    MainMenu,   // No menu principal
    Playing,    // Gameplay normal (entre ondas)
    Paused,     // Jogo pausado
    Shopping,   // Loja aberta
    InWave      // [ADICIONADO] Onda de inimigos em andamento
}

/// <summary>
/// Manager persistente que rastreia o estado global do jogo.
/// Sobrevive a todas as mudanças de cena via DontDestroyOnLoad.
/// Outros sistemas consultam State para decidir o que permitir.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.MainMenu;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            // ==============================================================
            //  DontDestroyOnLoad(gameObject)
            // ==============================================================
            //  Impede que este GameObject seja destruído quando uma nova
            //  cena é carregada. O GameManager persiste durante toda a
            //  sessão de jogo — por isso fica no Loader scene.
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Transiciona o jogo para um novo estado.
    /// Chamado por WaveManager (InWave / Playing), ShopManager (Shopping),
    /// PauseManager (Paused), etc.
    /// </summary>
    public void SetState(GameState newState) {
        State = newState;
    }
}
