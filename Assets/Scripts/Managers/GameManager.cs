using UnityEngine;

public enum GameState {
    MainMenu,
    Playing,
    Paused,
    Shopping
}

/// <summary>
/// Persistent central manager that tracks the global game state.
/// Lives in the Loader scene and survives all scene changes via DontDestroyOnLoad.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.MainMenu;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Transitions the game to a new state.
    /// </summary>
    public void SetState(GameState newState) {
        State = newState;
    }
}
