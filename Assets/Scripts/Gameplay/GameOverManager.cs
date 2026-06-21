using InfimaGames.LowPolyShooterPack;
using UnityEngine;

/// <summary>
/// Singleton that orchestrates the Game Over flow when the player dies.
/// Listens to PlayerHealth.OnPlayerDied, plays the death sound,
/// unlocks the cursor, disables input, and shows the Game Over panel.
/// </summary>
public class GameOverManager : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="GameOverManager"/> instance.</summary>
    public static GameOverManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Audio")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField, Range(0f, 1f)] private float deathClipVolume = 1f;

    [Header("Game Over Settings")]
    [SerializeField] private bool canGameOver = true;

    [Header("Player Reference")]
    [SerializeField] private Character playerCharacter;

    #endregion

    #region FIELDS

    private IAudioManagerService audioService;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        ResolvePlayerCharacter();
    }

    private void OnEnable() {
        var playerHealth = GetPlayerHealth();
        if (playerHealth != null)
            playerHealth.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable() {
        var playerHealth = GetPlayerHealth();
        if (playerHealth != null)
            playerHealth.OnPlayerDied -= HandlePlayerDied;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Ensures a valid reference to the player's character component.
    /// </summary>
    private void ResolvePlayerCharacter() {
        if (playerCharacter != null)
            return;

        playerCharacter = FindFirstObjectByType<Character>();
    }

    /// <summary>
    /// Finds the PlayerHealth component on the player character.
    /// </summary>
    private PlayerHealth GetPlayerHealth() {
        ResolvePlayerCharacter();

        if (playerCharacter != null)
            return playerCharacter.GetComponent<PlayerHealth>();

        return FindFirstObjectByType<PlayerHealth>();
    }

    /// <summary>
    /// Called when PlayerHealth.OnPlayerDied is invoked. Orchestrates the full Game Over flow.
    /// </summary>
    private void HandlePlayerDied() {
        if (!canGameOver)
            return;

        var playerHealth = GetPlayerHealth();
        if (playerHealth != null)
            playerHealth.OnPlayerDied -= HandlePlayerDied;

        audioService?.StopBGM(0.5f);

        if (playerCharacter != null) {
            playerCharacter.SetInterfaceMode(true);
            playerCharacter.SetHolstered(true);
            playerCharacter.ClearInputStates();
        }

        SetCursorState(true);

        audioService?.PlaySFX2D(deathClip, deathClipVolume);

        int currentWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
        UIManager.Instance?.ShowGameOver(currentWave);

        GameManager.Instance?.SetState(GameState.GameOver);
    }

    /// <summary>
    /// Sets cursor visibility and lock state together.
    /// </summary>
    private static void SetCursorState(bool visible) {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    #endregion
}
