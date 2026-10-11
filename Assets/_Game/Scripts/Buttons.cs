using UnityEngine;
using UnityEngine.EventSystems;

public class Buttons : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool IsPressed { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsPressed = false;
    }

    private void OnDisable()
    {
        IsPressed = false;
    }
}