using UnityEngine;

/// <summary>
/// Abstract base class for all interactable objects in the game.
/// Derived classes must implement the Interact() method.
/// Stores a customizable interaction prompt message that is displayed to the player.
/// </summary>
public abstract class Interactable : MonoBehaviour {

    [Header("Interaction Settings")]
    [SerializeField] private string interactionPrompt = "[E] Interact";

    /// <returns>The interaction prompt string to be displayed in the UI.</returns>
    public string GetInteractionPrompt() => interactionPrompt;

    /// <summary>
    /// Updates the interaction prompt string dynamically.
    /// </summary>
    /// <param name="newPrompt">The new message to display.</param>
    public void SetInteractionPrompt(string newPrompt) => interactionPrompt = newPrompt;

    /// <summary>
    /// Called when the player presses the interaction key while looking at this object.
    /// </summary>
    public abstract void Interact();
}
