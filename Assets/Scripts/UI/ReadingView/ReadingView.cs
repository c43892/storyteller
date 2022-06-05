using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadingView : MonoBehaviour
{
    public VideoController VCtrl = null;
    public SelectingView SelView = null;

    public void Play(string storyName, SystemLanguage lang)
    {
        VCtrl.LoadAndStartPlaying(storyName, lang);
    }

    public void ReturnBack()
    {
        VCtrl.StopPlaying();
        SelView.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }
}
