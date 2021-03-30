using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class PunchBtn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Button punch;
    public Sprite unpressed;
    public Sprite pressed;

    public UnityEvent OnClick;

    private void Start()
    {
        punch = GameObject.Find("Punch button").GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        punch.image.overrideSprite = pressed;
        throw new System.NotImplementedException();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        punch.image.overrideSprite = unpressed;
        throw new System.NotImplementedException();
    }
}
