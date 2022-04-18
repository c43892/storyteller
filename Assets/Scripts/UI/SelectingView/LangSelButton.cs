using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LangSelButton : Button
{
    public GameObject SelFrame = null;
    public SystemLanguage Language = SystemLanguage.Unknown;


    public bool Selected
    {
        get => SelFrame.activeSelf;
        set => SelFrame.SetActive(value);
    }
}
