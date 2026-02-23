using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent manager responsible for all scene transitions in the game.
/// Lives in the Loader scene and survives all scene changes via DontDestroyOnLoad.
/// </summary>
public class SceneLoader : MonoBehaviour {
    public static SceneLoader Instance { get; private set; }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Loads the main menu scene.
    /// </summary>
    public void LoadMenu() => SceneManager.LoadScene("Menu");

    /// <summary>
    /// Loads the main game scene.
    /// </summary>
    public void LoadGame() => SceneManager.LoadScene("Game");
}
