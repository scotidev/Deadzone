using System.Collections;
using UnityEngine;

/// <summary>
/// Class responsible for the logo intro scene. It waits for a specified duration
/// before transitioning to the main menu. It also allows skipping the intro
/// with any key press or mouse click if enabled.
/// </summary>
public class LogoIntro : MonoBehaviour {

    #region SERIALIZED FIELDS

    [SerializeField] private float duration = 4f;

    [Tooltip("If true, pressing any key or mouse click skips the intro and goes to menu.")]
    [SerializeField] private bool allowSkip = true;

    #endregion

    #region FIELDS

    private bool skipped = false;

    #endregion

    #region UNITY
    private void Start() {
        GameManager.Instance?.SetState(GameState.Loader);

        StartCoroutine(WaitAndLoad());
    }

    private void Update() {
        if (allowSkip && !skipped && Input.anyKeyDown)
            SkipIntro();
    }

    #endregion

    #region METHODS

    private IEnumerator WaitAndLoad() {
        yield return new WaitForSeconds(duration);

        GoToMenu();
    }

    private void SkipIntro() {
        skipped = true;
        StopAllCoroutines();
        GoToMenu();
    }

    private void GoToMenu() {
        SceneLoader.Instance?.LoadMenu();
    }

    #endregion
}
