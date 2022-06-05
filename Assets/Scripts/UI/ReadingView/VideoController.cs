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
    public readonly float PassThreshold = 0.8f;

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
    public class RecognitionStartEvent : UnityEvent<RecognitionTarget>
    {
    }

    public RecognitionStartEvent onRecognitionStarted = null;
    public UnityEvent<int> onPartialResult = null;
    public UnityEvent onRecognitionStopped = null;

    double[] timeStamps = null;
    int nextTimePoint = 0;

    private void Start()
    {
        pauseRecognition.gameObject.SetActive(false);
    }

    public void LoadAndStartPlaying(string storyName, SystemLanguage lang)
    {
        TargetLang = lang;
        ClearVideoTexture();

        script.LoadScript(storyName, (lang == SystemLanguage.ChineseSimplified || lang == SystemLanguage.ChineseTraditional), () =>
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
    void Update()
    {
        if (videoPlayer.isPlaying && nextTimePoint < timeStamps.Length && videoPlayer.clockTime >= timeStamps[nextTimePoint])
        {
            videoPlayer.Pause();
            var targetText = script.TargetSubTitle[timeStamps[nextTimePoint]];
            recognizer.vocabulary.wordList = new List<string>(targetText.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries));
            recognizer.StartRecognition();
            onRecognitionStarted?.Invoke(new RecognitionTarget() { text = targetText, asianLanguage = script.AsianLanguage });
            pauseRecognition.gameObject.SetActive(true);
            Debug.Log("recognition started for: " + targetText);
        }
    }

    void RestartVideo()
    {
        nextTimePoint = 0;
        timeStamps = script.TargetSubTitle.Keys.ToArray().OrderBy((k) => k).ToArray();
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
        Debug.Log($"<color=yellow>{str}</color>");

        if (videoPlayer.isPaused)
        {
            if (recognaizedPart == null)
                recognaizedPart = str.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToArray();
            else
                recognaizedPart = recognaizedPart.Concat(str.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)).ToArray();

            var targetSentence = script.TargetSubTitle[timeStamps[nextTimePoint]].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var matchCount = SentenceMatchCount(recognaizedPart, targetSentence);
            recognaizedPart = targetSentence.Take(matchCount).ToArray();

            Debug.Log("partially matched: " + matchCount + ":" + recognaizedPart);
            onPartialResult?.Invoke(matchCount);

            if (matchCount >= targetSentence.Length * PassThreshold)
                Move2Next();
        }
    }

    public void Move2Next()
    {
        Debug.Log("recognition stopped");
        recognaizedPart = null;
        recognizer.StopRecognition();
        onRecognitionStopped?.Invoke();
        pauseRecognition.gameObject.SetActive(false);

        nextTimePoint++;
        videoPlayer.Play();
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
