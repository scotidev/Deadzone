using UnityEngine;
using UnityEngine.EventSystems;

public class MenuImageScaleSound : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1f);
    public Vector3 pressedScale = new Vector3(0.95f, 0.95f, 1f);
    public float speed = 12f;

    public AudioSource hoverSound;
    public AudioSource clickSound;

    private Vector3 targetScale;
    private bool isSelected;

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
        Select();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Select();
    }

    void Select()
    {
        targetScale = selectedScale;

        if (!isSelected)
        {
            hoverSound.Play();
            isSelected = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Deselect();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Deselect();
    }

    void Deselect()
    {
        targetScale = normalScale;
        isSelected = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressedScale;
        clickSound.Play();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = selectedScale;
    }
}