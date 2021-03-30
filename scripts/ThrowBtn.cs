using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class ThrowBtn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Button throwbtn;
    public Sprite unpressed;
    public Sprite pressed;

    public UnityEvent OnClick;

    private void Start()
    {
        throwbtn = GameObject.Find("Throw").GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        throwbtn.image.overrideSprite = pressed;
        throw new System.NotImplementedException();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        throwbtn.image.overrideSprite = unpressed;
        throw new System.NotImplementedException();
    }
}
