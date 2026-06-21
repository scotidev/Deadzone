using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent manager responsible for all scene transitions in the game.
/// When the current GameState is not Intro, it shows a loading screen
/// with a progress bar while the new scene loads asynchronously.
/// </summary>
public class SceneLoader : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="SceneLoader"/> instance.</summary>
    public static SceneLoader Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Loading Screen")]
    [Tooltip("Prefab of the loading screen (Canvas + progress bar). " +
             "Must have a LoadingScreenUI component on its root.")]
    [SerializeField] private GameObject loadingScreenPrefab;

    [Tooltip("Tempo mínimo em segundos que a loading screen fica visível. " +
             "A barra de progresso leva esse tempo pra encher, mesmo em cenas leves.")]
    [SerializeField] private float minLoadingDuration = 2f;

    #endregion

    #region FIELDS

    private GameObject _loadingInstance;
    private LoadingScreenUI _loadingUI;

    private bool _isLoading = false;

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

    private void Start() {
        if (loadingScreenPrefab != null) {
            _loadingInstance = Instantiate(loadingScreenPrefab, transform);
            _loadingUI = _loadingInstance.GetComponent<LoadingScreenUI>();
            _loadingInstance.SetActive(false);
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Loads any scene by name. If the current GameState is Intro (the logo
    /// intro scene), the load happens synchronously without a loading screen.
    /// For every other transition it loads asynchronously and displays a
    /// loading screen with a progress bar.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    public void LoadScene(string sceneName) {
        if (_isLoading) {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Intro) {
            SceneManager.LoadScene(sceneName);
            return;
        }

        _isLoading = true;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// Loads a scene immediately, without a loading screen.
    /// Used for quick transitions like Menu to SelectMap.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    public void LoadSceneImmediate(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Coroutine that loads a scene asynchronously while displaying
    /// a loading screen with a progress bar.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    private IEnumerator LoadSceneAsync(string sceneName) {
        if (_loadingInstance != null) {
            _loadingInstance.SetActive(true);
            _loadingUI?.Show();
        }

        GameManager.Instance?.SetState(GameState.Loading);

        float startTime = Time.realtimeSinceStartup;

        yield return null;

        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);

        if (asyncOp == null) {
            _isLoading = false;
            yield break;
        }

        asyncOp.allowSceneActivation = false;

        while (true) {
            float loadingProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            float timeProgress = (Time.realtimeSinceStartup - startTime) / minLoadingDuration;
            float displayProgress = Mathf.Min(loadingProgress, timeProgress, 1f);

            _loadingUI?.SetProgress(displayProgress);

            if (asyncOp.progress >= 0.9f && timeProgress >= 1f)
                break;

            yield return null;
        }

        _loadingUI?.SetProgress(1f);
        yield return null;

        asyncOp.allowSceneActivation = true;

        while (!asyncOp.isDone)
            yield return null;

        _loadingUI?.Hide();

        if (_loadingInstance != null)
            _loadingInstance.SetActive(false);

        _isLoading = false;
    }

    #endregion
}
