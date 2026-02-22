using TMPro;
using UnityEngine;

/// <summary>
/// Manages the interaction prompt UI element that displays context-sensitive messages to the player.
/// </summary>
public class InteractionPromptUI : BaseUI
{
    [SerializeField] private TextMeshProUGUI interactionText;

    /// <summary>
    /// Shows the interaction prompt with a specific message.
    /// </summary>
    /// <param name="message">The message to display (e.g., "[E] Open Shop").</param>
    public void Show(string message)
    {
        UpdateMessage(message);
        Show();
    }

    /// <summary>
    /// Updates the interaction message text.
    /// </summary>
    /// <param name="message">The new message to display.</param>
    public void UpdateMessage(string message)
    {
        if (interactionText != null)
            interactionText.text = message;
    }
}
