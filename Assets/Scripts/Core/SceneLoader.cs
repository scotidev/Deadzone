using UnityEngine;
using UnityEngine.SceneManagement;

// Refatoração: esse script deveria ser um Service do Service Locator? Analise mais profunda necessaria.

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
    /// Loads any scene by name.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);

    #endregion
}
