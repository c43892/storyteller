using Recognissimo.Components;
using Recognissimo.Core;
using spkl.Diffs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Video;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public SpeechRecognizer recognizer;
    public LanguageModelProvider languageModelProvider;
    public VideoScript script;
    public PauseRecognitionButton pauseRecognition;
    public Button repeatButton;
    public Button skipButton;
    public readonly float PassThreshold = 1;
    public Text videoTimeText = null;

    [Serializable]
    public struct RecognitionTarget
    {
        public bool asianLanguage;
        public string text;

        public static RecognitionTarget Default = new RecognitionTarget() { text = null, asianLanguage = false };
        public static bool operator == (RecognitionTarget a, RecognitionTarget b) => a.text == b.text && a.asianLanguage == b.asianLanguage;
        public static bool operator != (RecognitionTarget a, RecognitionTarget b) => !(a == b);
        public override bool Equals(object obj) => this == (RecognitionTarget)obj;
        public override int GetHashCode() => text != null ? (text + asianLanguage.ToString()).GetHashCode() : 0;
    }

    [Serializable]
    public class RecognitionEvent : UnityEvent<RecognitionTarget>
    {
    }

    public UnityEvent<string> onSubtitlePlayingStarted = null;
    public RecognitionEvent onRecognitionStarted = null;
    public UnityEvent<int> onPartialResult = null;
    public UnityEvent onRecognitionStopped = null;

    double[] timeStampsEnd = null;
    double[] timeStampStart = null;
    int nextTimePoint = 0;

    private void Start()
    {
        pauseRecognition.gameObject.SetActive(false);
        repeatButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
    }

    public void LoadAndStartPlaying(string storyName, SystemLanguage lang)
    {
        TargetLang = lang;
        ClearVideoTexture();

        script.LoadSubtitle(storyName, (lang == SystemLanguage.ChineseSimplified || lang == SystemLanguage.ChineseTraditional), () =>
        {
            Addressables.LoadAssetAsync<VideoClip>("stories/" + storyName + "/video.mp4").Completed += (AsyncOperationHandle<VideoClip> obj) =>
            {
                videoPlayer.clip = obj.Result;
                RestartVideo();
            };
        });
    }

    public void StopPlaying()
    {
        videoPlayer.Stop();
        recognizer.vocabulary.wordList.Clear();
        recognizer.StopRecognition();

        ClearVideoTexture();
    }

    public void ClearVideoTexture()
    {
        // clear target texture content
        RenderTexture rt = RenderTexture.active;
        UnityEngine.RenderTexture.active = videoPlayer.targetTexture;
        GL.Clear(true, true, Color.black);
        UnityEngine.RenderTexture.active = rt;
    }


    // Update is called once per frame
    bool inSeekingBack = false;
    void Update()
    {
        if (!inSeekingBack && videoPlayer.isPlaying && nextTimePoint < timeStampsEnd.Length && videoPlayer.time >= timeStampsEnd[nextTimePoint])
        {
            videoPlayer.Pause();
            var targetText = script.SubTitleEndtime[timeStampsEnd[nextTimePoint]];
            recognizer.vocabulary.wordList = new List<string>(targetText.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries));
            recognizer.StartRecognition();
            onRecognitionStarted?.Invoke(new RecognitionTarget() { text = targetText, asianLanguage = script.AsianLanguage });
            pauseRecognition.gameObject.SetActive(true);
            repeatButton.gameObject.SetActive(true);
            skipButton.gameObject.SetActive(true);
            // Debug.Log("recognition started for: " + targetText);
        }
        else if (!inSeekingBack && videoPlayer.isPlaying && nextTimePoint < timeStampsEnd.Length && videoPlayer.time >= timeStampStart[nextTimePoint])
        {
            var targetText = script.SubTitleStarttime[timeStampStart[nextTimePoint]];
            onSubtitlePlayingStarted?.Invoke(targetText);
        }

        var secs = videoPlayer.time;
        var min = (int)(secs / 60);
        var sec = (int)(secs % 60);
        var ms = (int)((secs - min * 60 - sec) * 100);
        videoTimeText.text = min.ToString().PadLeft(2, '0')
            + ":" + sec.ToString().PadLeft(2, '0')
            + "." + ms.ToString().PadLeft(2, '0');
    }

    void RestartVideo()
    {
        inSeekingBack = false;
        nextTimePoint = 0;
        timeStampsEnd = script.SubTitleEndtime.Keys.ToArray().OrderBy((k) => k).ToArray();
        timeStampStart = script.SubTitleStarttime.Keys.ToArray().OrderBy((k) => k).ToArray();
        languageModelProvider.LoadLanguageModel(TargetLang);
        recognizer.StopRecognition();
        recognizer.vocabulary.wordList.Clear();
        videoPlayer.Stop();
        onRecognitionStopped?.Invoke();
        videoPlayer.Prepare();
        videoPlayer.Play();
    }

    string[] recognaizedPart = null;
    public void OnPartialResult(PartialResult result)
    {
        if (pauseRecognition.Pressed)
            return;

        var str = result.partial;
        // Debug.Log($"<color=yellow>{str}</color>");

        if (videoPlayer.isPaused)
        {
            if (recognaizedPart == null)
                recognaizedPart = str.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToArray();
            else
                recognaizedPart = recognaizedPart.Concat(str.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)).ToArray();

            var targetSentence = script.SubTitleEndtime[timeStampsEnd[nextTimePoint]].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var matchCount = SentenceMatchCount(recognaizedPart, targetSentence);
            recognaizedPart = targetSentence.Take(matchCount).ToArray();

            // recognizer.vocabulary.wordList = targetSentence.TakeLast(targetSentence.Length - matchCount).ToList();

            // Debug.Log("partially matched: " + matchCount + ":" + recognaizedPart);
            onPartialResult?.Invoke(matchCount);

            if (matchCount >= targetSentence.Length * PassThreshold)
                StartCoroutine(Move2Next());
        }
    }

    public IEnumerator Move2Next()
    {
        // Debug.Log("recognition stopped");
        recognizer.StopRecognition();
        onRecognitionStopped?.Invoke();

        yield return new WaitForSeconds(0.75f);
        recognaizedPart = null;
        pauseRecognition.gameObject.SetActive(false);
        repeatButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
        nextTimePoint++;
        videoPlayer.Play();
    }

    public void Jump2Previous()
    {
        if (videoPlayer.isPlaying) // only work on pause time
            return;

        recognizer.StopRecognition();
        onRecognitionStopped?.Invoke();

        recognaizedPart = null;
        pauseRecognition.gameObject.SetActive(false);
        repeatButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);

        // nextTimePoint = nextTimePoint < 1 ? 0 : nextTimePoint - 1;
        var targetTimeStamp = timeStampStart[nextTimePoint];
        StartCoroutine(Wait4VideoSeekingBack(targetTimeStamp));
    }

    public void SkipCurrentSentence()
    {
        StartCoroutine(Move2Next());
    }

    IEnumerator Wait4VideoSeekingBack(double targetTimeStamp)
    {
        inSeekingBack = true;

        videoPlayer.time = targetTimeStamp;
        videoPlayer.Play();

        if (videoPlayer.time > targetTimeStamp)
            yield return new WaitForSeconds(0.5f);

        inSeekingBack = false;
    }

    public int SentenceMatchCount(string[] recognitionResult, string[] targetSentence)
    {
        MyersDiff<string> diff = new MyersDiff<string>(recognitionResult, targetSentence, StringComparer.OrdinalIgnoreCase);
        return diff.GetResult().Count(rt => rt.ResultType == ResultType.Both);
    }

    public SystemLanguage TargetLang
    {
        get => script.TargetLang;
        set
        {
            script.TargetLang = value;
            RestartVideo();
        }
    }
}
