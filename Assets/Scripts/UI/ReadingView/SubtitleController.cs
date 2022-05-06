using Recognissimo.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static VideoController;

public class SubtitleController : MonoBehaviour
{
    public Text subtitleText;

    void Start()
    {
        subtitleText.text = null;
    }

    public void OnRecognitionStarted(RecognitionTarget target)
    {
        subtitleText.text = target.text;
    }

    public void OnRecognitionStopped()
    {
        subtitleText.text = null;
    }
}
