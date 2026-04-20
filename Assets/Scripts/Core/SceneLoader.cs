using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent manager responsible for all scene transitions in the game.
/// </summary>
public class SceneLoader : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="SceneLoader"/> instance.</summary>
    public static SceneLoader Instance { get; private set; }

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Loads the main menu scene.
    /// </summary>
    public void LoadMenu() => SceneManager.LoadScene("Menu");

    /// <summary>
    /// Loads the main game scene.
    /// </summary>
    public void LoadGame() => SceneManager.LoadScene("Game");

    #endregion
}
