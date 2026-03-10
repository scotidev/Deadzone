using UnityEngine;

public enum GameState {
    Loader,
    MainMenu,
    Playing,
    Paused,
    Shopping,
    InWave
}

/// <summary>
/// Persistent manager that tracks the global game state.
/// Other systems consult State to decide what is allowed.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Loader;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
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
