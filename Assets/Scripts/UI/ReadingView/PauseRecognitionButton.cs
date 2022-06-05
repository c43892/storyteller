using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseRecognitionButton : Button
{
    public bool Pressed { get; private set; }

    public override void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        Pressed = false;
        base.OnPointerUp(eventData);
    }
}
