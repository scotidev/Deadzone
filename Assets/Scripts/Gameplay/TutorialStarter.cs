using UnityEngine;

/// <summary>
/// Utilitário para garantir o estado inicial do tutorial.
/// Desativa os braços e o HUD de munição no início do jogo.
/// </summary>
public class TutorialStarter : MonoBehaviour {

    [Header("Initial State")]
    [Tooltip("O Mesh dos braços do jogador para começar desativado.")]
    [SerializeField] private GameObject playerArmsMesh;

    [Tooltip("O objeto do HUD de munição para começar desativado.")]
    [SerializeField] private GameObject ammoHUDObject;

    private void Start() {
        // Garantimos que o player comece sem ver os braços (até dar melee ou pegar arma)
        if (playerArmsMesh != null) {
            playerArmsMesh.SetActive(false);
        }

        // Garantimos que o HUD de munição só apareça após pegar a primeira arma
        if (ammoHUDObject != null) {
            ammoHUDObject.SetActive(false);
        }
    }
}
