using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class SpBtn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Button spbtn;
    public Sprite unpressed;
    public Sprite pressed;

    public UnityEvent OnClick;

    private void Start()
    {
        spbtn = GameObject.Find("Special button").GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        spbtn.image.overrideSprite = pressed;
        //throw new System.NotImplementedException();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        spbtn.image.overrideSprite = unpressed;
        //throw new System.NotImplementedException();
    }
}
