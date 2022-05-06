using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadingView : MonoBehaviour
{
    public VideoController VCtrl = null;

    public void Play(string storyName, SystemLanguage lang)
    {
        VCtrl.LoadAndStart(storyName, lang);
    }
}
