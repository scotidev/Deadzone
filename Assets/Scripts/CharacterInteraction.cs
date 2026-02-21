using InfimaGames.LowPolyShooterPack;
using UnityEngine;

/// <summary>
/// Acts as a bridge between the shop system and the Infima Games character system.
/// Translates shop commands to player character actions, managing the transition between
/// gameplay mode and menu/interface mode (pausing movement, camera rotation, shooting, and cursor).
/// </summary>
public class CharacterInteraction : MonoBehaviour {
    public static CharacterInteraction Instance { get; private set; }

    private Character playerCharacter;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        playerCharacter = GetComponent<Character>();
    }

    /// <summary>
    /// Switches the player between "Gameplay Mode" and "Menu Mode".
    /// Controls camera rotation, leg movement, weapon shooting, and mouse cursor visibility.
    /// </summary>
    /// <param name="isPaused">True if in menu mode, False if in gameplay mode.</param>
    public void SetInterfaceMode(bool isPaused) {
        if (playerCharacter == null) return;

        playerCharacter.SetInterfaceMode(isPaused);
    }
}
