using InfimaGames.LowPolyShooterPack;
using UnityEngine;

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

    public void SetInterfaceMode(bool isPaused) {
        if (playerCharacter == null) return;

        playerCharacter.SetInterfaceMode(isPaused);
    }

    /// <summary>
    /// Hides or reveals the player's weapon by toggling the holster animation.
    /// holstered = true  → hides the weapon (player cannot shoot).
    /// holstered = false → reveals the weapon (player can shoot).
    /// Called by BuildingController when entering/exiting build mode to prevent shooting while building.
    /// </summary>
    /// <param name="holstered"></param>
    public void SetHolstered(bool holstered) {
        if (playerCharacter == null) return;
        playerCharacter.SetHolstered(holstered);
    }
}
