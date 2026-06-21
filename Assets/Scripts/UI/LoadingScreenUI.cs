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

    private int _lastBackgroundIndex = -1;

    #endregion

    #region METHODS

    /// <summary>
    /// Makes the loading screen visible, resets progress to 0, and picks a random background.
    /// </summary>
    public void Show() {
        gameObject.SetActive(true);

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
        }

        SetProgress(0f);
        PickRandomBackground();
    }

    /// <summary>
    /// Picks a random sprite from the backgrounds array different from the last one used.
    /// </summary>
    private void PickRandomBackground() {
        if (backgroundImage == null || backgroundSprites == null || backgroundSprites.Length == 0)
            return;

        if (backgroundSprites.Length == 1) {
            backgroundImage.sprite = backgroundSprites[0];
            _lastBackgroundIndex = 0;
            return;
        }

        int randomIndex;
        do {
            randomIndex = Random.Range(0, backgroundSprites.Length);
        } while (randomIndex == _lastBackgroundIndex);

        backgroundImage.sprite = backgroundSprites[randomIndex];
        _lastBackgroundIndex = randomIndex;
    }

    /// <summary>
    /// Hides the loading screen.
    /// </summary>
    public void Hide() {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates the progress bar fill amount and optional percentage text.
    /// </summary>
    /// <param name="progress">Value from 0 (empty) to 1 (complete).</param>
    public void SetProgress(float progress) {
        if (progressBar != null)
            progressBar.fillAmount = progress;

        if (percentageText != null)
            percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    #endregion
}
