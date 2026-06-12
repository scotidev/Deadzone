using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Refatoração: esse script deveria ser um Service do Service Locator? Analise mais profunda necessaria.

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

    // Instância da loading screen pré-criada no Awake.
    // Fica desativada até ser necessária, evitando a travada de Instantiate na hora.
    private GameObject _loadingInstance;
    private LoadingScreenUI _loadingUI;

    // Flag que impede que um segundo LoadScene seja chamado enquanto já estamos carregando.
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
        // Pré-instancia a loading screen e já deixa ela desativada.
        // Assim na hora de mostrar é só um SetActive — instantâneo.
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
        // Se já estiver carregando uma cena, ignora chamadas duplicadas.
        if (_isLoading) {
            return;
        }

        // Estamos vindo da cena de Intro (logos)?
        // Se sim, carrega sem loading screen (transição rápida).
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Intro) {
            SceneManager.LoadScene(sceneName);
            return;
        }

        // Marca como carregando e inicia a coroutine.
        _isLoading = true;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// Carrega uma cena imediatamente, sem loading screen.
    /// Usado para transições rápidas como Menu → SelectMap.
    /// </summary>
    /// <param name="sceneName">Nome da cena a ser carregada.</param>
    public void LoadSceneImmediate(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Coroutine que carrega a cena de forma assíncrona enquanto exibe
    /// uma tela de loading com barra de progresso.
    /// </summary>
    /// <param name="sceneName">Nome da cena a ser carregada.</param>
    private IEnumerator LoadSceneAsync(string sceneName) {
        // (1) Ativa a loading screen que já foi instanciada no Start().
        //     Como o GameObject já existe, não trava.
        if (_loadingInstance != null) {
            _loadingInstance.SetActive(true);
            _loadingUI?.Show();
        }

        // (2) Avisa o GameManager que estamos carregando.
        GameManager.Instance?.SetState(GameState.Loading);

        // (3) Marca o tempo real de início para calcular a barra mínima.
        float startTime = Time.realtimeSinceStartup;

        // (4) Espera UM FRAME para dar tempo da loading screen renderizar
        //     na tela antes do LoadSceneAsync começar o trabalho pesado.
        yield return null;

        // (5) Inicia o carregamento assíncrono da cena.
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);

        // Segurança: se o nome da cena não existir nas Build Settings, aborta.
        if (asyncOp == null) {
            _isLoading = false;
            yield break;
        }

        // Impede que a cena seja ativada automaticamente,
        // assim controlamos quando fazer a transição.
        asyncOp.allowSceneActivation = false;

        // (6) Loop único: a barra mostra o MENOR valor entre o progresso real
        //     do carregamento (0→1) e o progresso do tempo decorrido (0→1 em 2s).
        //     Resultado: a barra demora no mínimo 2s pra encher, mesmo em cenas
        //     leves, e se a cena for pesada ela segue o progresso real.
        while (true) {
            // Progresso real do carregamento (0 → 0.9 normalizado pra 0 → 1).
            float loadingProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            // Progresso do tempo decorrido (de 0 a 1 em minLoadingDuration segundos).
            float timeProgress = (Time.realtimeSinceStartup - startTime) / minLoadingDuration;
            // Usa o menor dos dois — a barra nunca pula na frente.
            float displayProgress = Mathf.Min(loadingProgress, timeProgress, 1f);

            _loadingUI?.SetProgress(displayProgress);

            // Sai do loop quando BOTH loading e tempo mínimo estão completos.
            if (asyncOp.progress >= 0.9f && timeProgress >= 1f)
                break;

            yield return null;
        }

        // (7) Cena carregada + tempo mínimo cumprido.
        //     Garante 100% na barra.
        _loadingUI?.SetProgress(1f);
        yield return null;

        // (8) Ativa a nova cena ENQUANTO a loading screen ainda está visível,
        //     evitando que o jogador veja um flash da cena antiga.
        //     Os Awake() e Start() da nova cena vão rodar aqui.
        asyncOp.allowSceneActivation = true;

        // Aguarda a ativação completar antes de continuar.
        while (!asyncOp.isDone)
            yield return null;

        // (9) Só esconde a loading DEPOIS que a nova cena já está ativa.
        _loadingUI?.Hide();

        // (10) Desativa a loading (fica guardada pra próxima transição).
        if (_loadingInstance != null)
            _loadingInstance.SetActive(false);

        _isLoading = false;
    }

    #endregion
}
