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

    #endregion

    #region FIELDS

    // Instância da loading screen pré-criada no Awake.
    // Fica desativada até ser necessária, evitando a travada de Instantiate na hora.
    private GameObject _loadingInstance;
    private LoadingScreenUI _loadingUI;

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
        // Estamos vindo da cena de Intro (logos)?
        // Se sim, carrega sem loading screen (transição rápida).
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Intro) {
            SceneManager.LoadScene(sceneName);
            return;
        }

        // Para todas as outras transições, mostra a loading screen.
        StartCoroutine(LoadSceneAsync(sceneName));
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

        // (3) Espera UM FRAME para dar tempo da loading screen renderizar
        //     na tela antes do LoadSceneAsync começar o trabalho pesado.
        yield return null;

        // (4) Inicia o carregamento assíncrono da cena.
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);

        // Impede que a cena seja ativada automaticamente,
        // assim controlamos quando fazer a transição.
        asyncOp.allowSceneActivation = false;

        // (5) Enquanto a cena carrega (progress vai de 0 até 0.9),
        //     atualizamos a barra de progresso em tempo real.
        while (asyncOp.progress < 0.9f) {
            // Normaliza o progress: 0 → 0.9 vira 0 → 1 na barra.
            float normalizedProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            _loadingUI?.SetProgress(normalizedProgress);
            // Espera um frame antes de checar denovo.
            yield return null;
        }

        // (6) Cena totalmente carregada em memória.
        //     Mostra 100% na barra e dá uma pequena pausa pra dar feedback visual.
        _loadingUI?.SetProgress(1f);
        yield return new WaitForSecondsRealtime(0.25f);

        // (7) Esconde a loading screen ANTES de ativar a nova cena.
        _loadingUI?.Hide();

        // (8) Agora ativa a nova cena.
        //     Os Awake() e Start() da nova cena vão rodar aqui.
        asyncOp.allowSceneActivation = true;

        // Aguarda a ativação completar antes de continuar.
        while (!asyncOp.isDone)
            yield return null;

        // (9) Desativa a loading (fica guardada pra próxima transição).
        if (_loadingInstance != null)
            _loadingInstance.SetActive(false);
    }

    #endregion
}
