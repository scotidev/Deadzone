using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// Generic relay that forwards pointer events (hover enter, hover exit, click) as UnityEvents.
/// Used alongside UIButtonFeedback to separate visual/audio feedback from domain logic.
/// Attach to any UI element that needs to trigger separate logic on hover/click.
/// </summary>
public class MapCardRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

    [Header("Events")]
    [Tooltip("Invoked when the pointer enters this element")]
    [SerializeField] private UnityEvent onHoverEnter;

    [Tooltip("Invoked when the pointer exits this element")]
    [SerializeField] private UnityEvent onHoverExit;

    [Tooltip("Invoked when the pointer clicks this element")]
    [SerializeField] private UnityEvent onClick;

    /// <summary>
    /// Fires the onHoverEnter UnityEvent when the pointer enters this element.
    /// </summary>
    /// <param name="eventData">Pointer event data from Unity's event system.</param>
    public void OnPointerEnter(PointerEventData eventData) {
        onHoverEnter?.Invoke();
    }

    /// <summary>
    /// Fires the onHoverExit UnityEvent when the pointer exits this element.
    /// </summary>
    /// <param name="eventData">Pointer event data from Unity's event system.</param>
    public void OnPointerExit(PointerEventData eventData) {
        onHoverExit?.Invoke();
    }

    /// <summary>
    /// Fires the onClick UnityEvent when the pointer clicks this element.
    /// </summary>
    /// <param name="eventData">Pointer event data from Unity's event system.</param>
    public void OnPointerClick(PointerEventData eventData) {
        onClick?.Invoke();
    }
}
