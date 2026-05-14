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

    #endregion

    #region METHODS

    /// <summary>
    /// Torna a tela de loading visível e reseta a barra para 0%.
    /// </summary>
    public void Show() {
        // Ativa o GameObject (que contém o Canvas e tudo da loading screen).
        gameObject.SetActive(true);
        // Começa com a barra vazia.
        SetProgress(0f);
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
