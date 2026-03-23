using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Plays UI audio feedback (hover and click sounds) for menu buttons.
/// Designed to be used alongside <see cref="MenuImageScale"/> on the same GameObject.
/// Routes all audio through <see cref="AudioManager"/> to respect the global SFX volume.
/// </summary>
public class MenuButtonAudio : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler {

    [Header("Sounds")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private bool isHovered;

    /// <summary>Called when the pointer enters the element. Triggers hover sound once.</summary>
    public void OnPointerEnter(PointerEventData e) => TryPlayHover();

    /// <summary>Called when the element is selected via keyboard or gamepad. Triggers hover sound once.
    /// </summary>
    public void OnSelect(BaseEventData e) => TryPlayHover();

    /// <summary>Called when the pointer exits the element. Resets the hover guard.</summary>
    public void OnPointerExit(PointerEventData e) => isHovered = false;

    /// <summary>Called when the element is deselected. Resets the hover guard.</summary>
    public void OnDeselect(BaseEventData e) => isHovered = false;

    /// <summary>Called when the pointer is pressed down. Plays the click sound.</summary>
    public void OnPointerDown(PointerEventData e) {
        AudioManager.Instance?.PlaySFX(clickSound);
    }

    /// <summary>Plays the hover sound only once per hover session, guarded by the isHovered flag.</summary>
    private void TryPlayHover() {
        if (isHovered) return;

        AudioManager.Instance?.PlaySFX(hoverSound);

        isHovered = true;
    }
}
