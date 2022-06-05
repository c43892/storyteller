using Recognissimo.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static VideoController;

public class SubtitleController : MonoBehaviour
{
    public Text subtitleText;
    RecognitionTarget target;

    void Start()
    {
        subtitleText.text = null;
    }

    public void OnRecognitionStarted(RecognitionTarget rt)
    {
        target = rt;
        subtitleText.text = target.text;
    }

    public void OnRecognitionStopped()
    {
        target = RecognitionTarget.Default;
        StartCoroutine(ClearSubtext());
    }

    public IEnumerator ClearSubtext()
    {
        yield return new WaitForSeconds(1);
        subtitleText.text = target != RecognitionTarget.Default ? target.text : null;
    }

    public void OnPartialResult(int matchCount)
    {
        var str = target.text;
        if (str == null)
            return;

        var words = str.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
        subtitleText.text = "<color=\"yellow\">";

        var i = 0;
        while (i < matchCount && i < words.Length)
        {
            subtitleText.text += words[i] + " ";
            i++;
        }

        subtitleText.text += "</color>";

        while (i < words.Length)
        {
            subtitleText.text += words[i] + " ";
            i++;
        }

    }
}
