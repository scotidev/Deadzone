using TMPro;
using UnityEngine;

/// <summary>
/// Manages user interface elements throughout the game.
/// </summary>
public class UIManager : MonoBehaviour {
    public static UIManager Instance { get; private set; }

    [Header("Interaction Elements")]
    [SerializeField] private GameObject interactionGroup;
    [SerializeField] private TextMeshProUGUI interactionText;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (interactionGroup != null)
            interactionGroup.SetActive(false);
    }

    /// <summary>
    /// Toggles the visibility of the interaction prompt and updates its message.
    /// Automatically hides the prompt if the shop is open to prevent UI overlap.
    /// </summary>
    /// <param name="show">True to show the prompt, false to hide it.</param>
    /// <param name="message">The interaction message to display (e.g., "[E] Open Shop").</param>
    public void ToggleInteractionPrompt(bool show, string message = "") {
        if (interactionGroup == null) return;

        if (ShopInterface.Instance != null && ShopInterface.Instance.IsShopOpen()) {
            interactionGroup.SetActive(false);
            return;
        }

        interactionGroup.SetActive(show);

        if (show && interactionText != null) {
            interactionText.text = message;
        }
    }
}
