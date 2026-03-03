using UnityEngine;
using UnityEngine.EventSystems;

public class MenuImageScale : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1f);
    public Vector3 pressedScale = new Vector3(0.95f, 0.95f, 1f);
    public float speed = 12f;

    private Vector3 targetScale;

    void Start()
    {
        targetScale = normalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = selectedScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        targetScale = selectedScale;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        targetScale = normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = selectedScale;
    }
}