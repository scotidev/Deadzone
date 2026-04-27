using UnityEngine;
using UnityEngine.EventSystems;

// REFATORAÇAÕ: esse script é necessario? Nao seria possivel unir ele ao MenuButtonAudio? assim seria possivel remover todos esses PointerEvents? Talvez criar um novo script chamado MenuButtonController? que unifique a logica visual e que toque os sons usando o IAudioManagerService, para garantir que o sistema de audio seja respeitado e funcione em todas as cenas do jogo, sem depender de singletons ou implementações específicas.

/// <summary>
/// Animates the local scale of a UI element in response to pointer
/// and selection events. Works for both mouse and gamepad navigation.
/// Attach alongside <see cref="MenuButtonAudio"/> when sound is needed.
/// </summary>
public class MenuImageScale : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler {
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1f);
    public Vector3 pressedScale = new Vector3(0.95f, 0.95f, 1f);

    public float speed = 12f;

    private Vector3 targetScale;

    /// <summary>Initializes the target scale to the normal (resting) scale.</summary>
    private void Start() => targetScale = normalScale;

    /// <summary>Smoothly interpolates the object's local scale toward the target scale each frame.</summary>
    private void Update() {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    /// <summary>Called when the pointer enters the element. Scales up to selected size.</summary>
    public void OnPointerEnter(PointerEventData e) => targetScale = selectedScale;

    /// <summary>Called when the pointer exits the element. Returns to normal size.</summary>
    public void OnPointerExit(PointerEventData e) => targetScale = normalScale;

    /// <summary>Called when the element is selected via keyboard or gamepad. Scales up to selected size.</summary>
    public void OnSelect(BaseEventData e) => targetScale = selectedScale;

    /// <summary>Called when the element is deselected. Returns to normal size.</summary>
    public void OnDeselect(BaseEventData e) => targetScale = normalScale;

    /// <summary>Called when the pointer is pressed down. Scales down to give a "pressed" feel.</summary>
    public void OnPointerDown(PointerEventData e) => targetScale = pressedScale;

    /// <summary>Called when the pointer is released. Returns to selected size since pointer is still over the element.</summary>
    public void OnPointerUp(PointerEventData e) => targetScale = selectedScale;
}