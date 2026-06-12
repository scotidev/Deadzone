using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the loading screen UI: shows/hides the screen and updates the
/// progress bar fill amount. Attach this script to the root of your loading
/// screen prefab and assign the progress bar Image in the Inspector.
/// </summary>
public class LoadingScreenUI : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("References")]

    [Tooltip("Image component usado como barra de progresso. " +
             "No Inspector, configure Image.Type = Filled e FillMethod = Horizontal.")]
    [SerializeField] private Image progressBar;

    [Tooltip("(Opcional) Texto que mostra a porcentagem, ex: '75%'.")]
    [SerializeField] private Text percentageText;

    [Header("Background")]
    [Tooltip("Image que vai exibir o fundo do loading screen.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Array de sprites de fundo. Um aleatório é escolhido a cada loading, " +
             "nunca repetindo o mesmo duas vezes seguidas.")]
    [SerializeField] private Sprite[] backgroundSprites;

    #endregion

    #region FIELDS

    // Guarda o índice do último sprite que tocou pra não repetir.
    private int _lastBackgroundIndex = -1;

    #endregion

    #region METHODS

    /// <summary>
    /// Torna a tela de loading visível, reseta a barra para 0%
    /// e sorteia um fundo aleatório.
    /// </summary>
    public void Show() {
        // Ativa o GameObject (que contém o Canvas e tudo da loading screen).
        gameObject.SetActive(true);

        // Garante que o Canvas da loading screen renderize SEMPRE por cima
        // de todos os outros Canvases durante a transição de cenas.
        // O overrideSorting=true + sortingOrder alto impede que o Canvas da
        // cena antiga ou da nova cena apareça por cima da loading screen.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
        }

        // Começa com a barra vazia.
        SetProgress(0f);
        // Sorteia um sprite de fundo diferente do último.
        PickRandomBackground();
    }

    /// <summary>
    /// Sorteia um sprite do array <see cref="backgroundSprites"/> que seja
    /// diferente do último que tocou. Se o array tiver menos de 2 elementos,
    /// sempre pega o índice 0 (ou não faz nada se estiver vazio).
    /// </summary>
    private void PickRandomBackground() {
        // Se não tem Image de fundo ou não tem sprites, não faz nada.
        if (backgroundImage == null || backgroundSprites == null || backgroundSprites.Length == 0)
            return;

        // Se só tem um sprite, usa ele sempre.
        if (backgroundSprites.Length == 1) {
            backgroundImage.sprite = backgroundSprites[0];
            _lastBackgroundIndex = 0;
            return;
        }

        // Sorteia um índice diferente do último.
        int randomIndex;
        do {
            randomIndex = Random.Range(0, backgroundSprites.Length);
        } while (randomIndex == _lastBackgroundIndex);

        // Aplica o sprite sorteado e guarda o índice.
        backgroundImage.sprite = backgroundSprites[randomIndex];
        _lastBackgroundIndex = randomIndex;
    }

    /// <summary>
    /// Esconde a tela de loading.
    /// </summary>
    public void Hide() {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Atualiza o preenchimento da barra de progresso e, se houver,
    /// o texto de porcentagem.
    /// </summary>
    /// <param name="progress">
    /// Valor de 0 (vazio) a 1 (completo).
    /// Exemplo: 0.75f = 75% preenchido.
    /// </param>
    public void SetProgress(float progress) {
        // Se temos uma Image de barra, atualiza o fillAmount.
        // O fillAmount já trabalha com valores de 0 a 1 — exatamente o que precisamos.
        if (progressBar != null)
            progressBar.fillAmount = progress;

        // Se temos um texto de porcentagem, atualiza ele também.
        if (percentageText != null)
            percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    #endregion
}
