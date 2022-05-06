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
    public Text TargetLangLabel;
    public VideoScript script;
    public float PassThreshold = 0.6f;

    [Serializable]
    public struct RecognitionTarget
    {
        public string text;
    }

    [Serializable]
    public class RecognitionStartEvent : UnityEvent<RecognitionTarget>
    {
    }

    public RecognitionStartEvent onRecognitionStarted = null;
    public UnityEvent onRecognitionStopped = null;

    double[] timeStamps = null;
    int nextTimePoint = 0;

    public void LoadAndStart(string storyName, SystemLanguage lang)
    {
        TargetLang = lang;
        script.LoadScript(storyName, (lang == SystemLanguage.ChineseSimplified || lang == SystemLanguage.ChineseTraditional), () =>
        {
            Addressables.LoadAssetAsync<VideoClip>("stories/" + storyName + "/video.mp4").Completed += (AsyncOperationHandle<VideoClip> obj) =>
            {
                videoPlayer.clip = obj.Result;
                RestartVideo();
            };
        });
    }


    // Update is called once per frame
    void Update()
    {
        if (videoPlayer.isPlaying && nextTimePoint < timeStamps.Length && videoPlayer.clockTime >= timeStamps[nextTimePoint])
        {
            videoPlayer.Pause();
            var targetText = script.TargetSubTitle[timeStamps[nextTimePoint]];
            recognizer.vocabulary.wordList = 
                new List<string>(targetText.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries));
            recognizer.StartRecognition();
            onRecognitionStarted?.Invoke(new RecognitionTarget() { text = targetText });
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

    private void VideoPlayer_prepareCompleted()
    {
        throw new NotImplementedException();
    }

    public void OnResult(Result result)
    {
        Debug.Log($"<color=green>{result.text}</color>");
        if (videoPlayer.isPaused && CompareSentences(
                result.text.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries),
                script.TargetSubTitle[timeStamps[nextTimePoint]].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) >= PassThreshold)
        {
            Debug.Log("recognition stopped");
            recognizer.StopRecognition();

            nextTimePoint++;
            onRecognitionStopped?.Invoke();
            videoPlayer.Play();
        }
    }

    public float CompareSentences(string[] recognitionResult, string[] targetSentence)
    {
        MyersDiff<string> diff = new MyersDiff<string>(recognitionResult, targetSentence, StringComparer.OrdinalIgnoreCase);
        var matchCount = diff.GetResult().Count(rt => rt.ResultType == ResultType.Both);
        var matchRate = (float)matchCount / targetSentence.Length;
        Debug.Log($"<color=" + (matchRate >= PassThreshold ? "green" : "red") + $">{matchRate}</color>");
        return matchRate;
    }

    public void SwitchLang()
    {
        if (TargetLang == SystemLanguage.English)
        {
            TargetLang = SystemLanguage.ChineseSimplified;
            TargetLangLabel.text = "cn";
        }
        else
        {
            TargetLang = SystemLanguage.English;
            TargetLangLabel.text = "en";
        }
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
