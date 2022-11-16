using Recognissimo.Components;
using Recognissimo.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static VideoController;

public class SubtitleController : MonoBehaviour
{
    public Text singleWordTextModel;
    public Transform subtitleArea;
    public AudioSource AS;

    RecognitionTarget target;

    readonly List<Text> wordTexts = new List<Text>();

    void ClearAllWordTexts()
    {
        foreach (var txt in wordTexts)
        {
            txt.transform.SetParent(null);
            txt.GetComponent<Button>().onClick.RemoveAllListeners();
            Destroy(txt.gameObject);
        }

        wordTexts.Clear();
    }

    void Start()
    {
        ClearAllWordTexts();
    }

    public void onSubtitlePlayingStarted(string text)
    {
        ClearAllWordTexts();

        var words = text.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
        foreach(var w in words)
            AddWordText(w);
    }

    public void onRecognitionStarted(RecognitionTarget target)
    {
        this.target = target;
    }

    public void OnRecognitionStopped()
    {
        target = RecognitionTarget.Default;
        StartCoroutine(ClearSubtext());
    }

    public IEnumerator ClearSubtext()
    {
        yield return new WaitForSeconds(0.5f);
        ClearAllWordTexts();
    }

    public void OnPartialResult(int matchCount)
    {
        var str = target.text;
        if (str == null)
            return;


        for (var i = 0; i < wordTexts.Count; i++)
            wordTexts[i].color = i < matchCount ? Color.yellow : Color.white;
    }

    void AddWordText(string word)
    {
        var txt = Instantiate(singleWordTextModel);
        txt.text = word;
        txt.transform.SetParent(subtitleArea);
        txt.gameObject.SetActive(true);
        txt.color = Color.white;
        wordTexts.Add(txt);
        txt.transform.localScale = Vector3.one;

        txt.GetComponent<Button>().onClick.AddListener(() => OnClickWord(word));
    }

    public SpeechRecognizer recognizer;

    IEnumerator WaitForAudioSourceStopPlaying()
    {
        yield return new WaitUntil(() => !AS.isPlaying);
        recognizer.StartRecognition();
    }

    IEnumerator MakePronounciation(string word)
    {
        recognizer.StopRecognition();
        yield return ResourceManager.DownloadWordPronounciation(word);
        yield return ResourceManager.LoadWordPronounciation(word, (clip) =>
        {
            AS.clip = clip;
            AS.Play();

            if (AS.clip == null)
                recognizer.StartRecognition();
            else
                StartCoroutine(WaitForAudioSourceStopPlaying());
        });
    }

    void OnClickWord(string word)
    {
        StartCoroutine(MakePronounciation(word.ToLower()));
    }
}
