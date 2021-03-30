using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KickBtn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Button kick;
    public Sprite unpressed;
    public Sprite pressed;

    public UnityEvent OnClick;

    private void Start()
    {
        kick = GameObject.Find("Kick button").GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        kick.image.overrideSprite = pressed;
        throw new System.NotImplementedException();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        kick.image.overrideSprite = unpressed;
        throw new System.NotImplementedException();
    }
}
