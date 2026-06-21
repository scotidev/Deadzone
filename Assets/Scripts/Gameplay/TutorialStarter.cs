using UnityEngine;

/// <summary>
/// Ensures the initial state for the tutorial: the player arms and ammo HUD start disabled.
/// </summary>
public class TutorialStarter : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Initial State")]
    [Tooltip("O Mesh dos braços do jogador para começar desativado.")]
    [SerializeField] private GameObject playerArmsMesh;

    [Tooltip("O objeto do HUD de munição para começar desativado.")]
    [SerializeField] private GameObject ammoHUDObject;

    #endregion

    #region UNITY

    private void Start() {
        if (playerArmsMesh != null) {
            playerArmsMesh.SetActive(false);
        }

        if (ammoHUDObject != null) {
            ammoHUDObject.SetActive(false);
        }
    }

    #endregion
}
